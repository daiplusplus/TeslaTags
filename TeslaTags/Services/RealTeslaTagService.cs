using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TeslaTags
{
	public partial class RealTeslaTagService : ITeslaTagsService
	{
		private sealed class LogFiles : IDisposable
		{
			const String DirectoryLog = "TeslaTags_DoneDirectories.txt";
			const String GeneralLog   = "TeslaTags_Log.txt";

			public static LogFiles Create(String rootDirectory)
			{
				String directoryLogFileName = Path.Combine( rootDirectory, DirectoryLog );
				String generalLogFileName   = Path.Combine( rootDirectory, GeneralLog   );

				HashSet<String> doneDirectories = new HashSet<String>( StringComparer.OrdinalIgnoreCase );

				if( File.Exists( directoryLogFileName ) && new FileInfo( directoryLogFileName ).Length > 3 ) // 3 for the BOM
				{
					foreach( String line in File.ReadAllLines( directoryLogFileName ) )
					{
						doneDirectories.Add( line );
					}
				}

				StreamWriter directoryLogWriter = null;
				StreamWriter generalLogWriter   = null;
				try
				{
					directoryLogWriter = new StreamWriter( directoryLogFileName, append: true );
					generalLogWriter   = new StreamWriter( generalLogFileName  , append: true );

					return new LogFiles( directoryLogWriter, generalLogWriter, directoryLogFileName, generalLogFileName, doneDirectories );
				}
				catch
				{
					directoryLogWriter?.Dispose();
					generalLogWriter  ?.Dispose();

					throw;
				}
			}

			private LogFiles(StreamWriter directoryLogWriter, StreamWriter generalLogWriter, String directoryLogFileName, String generalLogFileName, HashSet<String> doneDirectories)
			{
				this.directoryLogWriter   = directoryLogWriter   ?? throw new ArgumentNullException( nameof( directoryLogWriter   ) );
				this.generalLogWriter     = generalLogWriter     ?? throw new ArgumentNullException( nameof( generalLogWriter     ) );
				this.directoryLogFileName = directoryLogFileName ?? throw new ArgumentNullException( nameof( directoryLogFileName ) );
				this.generalLogFileName   = generalLogFileName   ?? throw new ArgumentNullException( nameof( generalLogFileName   ) );
				this.doneDirectories      = doneDirectories      ?? throw new ArgumentNullException( nameof( doneDirectories      ) );
			}

			public void Dispose()
			{
				this.directoryLogWriter.Dispose();
				this.generalLogWriter  .Dispose();
			}

			public void DeleteDirectoryLog()
			{
				// Close the streams and delete the log files.
				this.directoryLogWriter.Dispose();
				if( File.Exists( this.directoryLogFileName ) ) File.Delete( this.directoryLogFileName );
			}

			private readonly StreamWriter directoryLogWriter;
			private readonly StreamWriter generalLogWriter;

			public readonly String directoryLogFileName;
			public readonly String generalLogFileName;

			public readonly HashSet<String> doneDirectories;

			public void DirectoryLogWriteLine( String line )
			{
				this.directoryLogWriter.WriteLine( line );
				this.directoryLogWriter.Flush();
			}

			public void GeneralLogWriteLine( String line )
			{
				this.generalLogWriter.WriteLine( line );
				this.generalLogWriter.Flush();
			}

			public void GeneralLogWriteLines( IEnumerable<String> lines )
			{
				foreach( String line in lines )
				{
					this.generalLogWriter.WriteLine( line );
				}

				this.generalLogWriter.Flush();
			}
		}

		public Task StartRetaggingAsync( RetaggingOptions options, IProgress<IReadOnlyList<String>> directoriesProgress, IProgress<DirectoryResult> directoryProgress, CancellationToken cancellationToken )
		{
			TaskCompletionSource<Object> tcs = new TaskCompletionSource<Object>();

			ThreadPool.QueueUserWorkItem( new WaitCallback( ( state ) => {

				try
				{
					RunRetagging( options, directoriesProgress, directoryProgress, cancellationToken );

					if( cancellationToken.IsCancellationRequested )
					{
						tcs.SetCanceled();
					}
					else
					{
						tcs.SetResult( null ); // Mark the `tcs.Task` as completed successfully.
					}
				}
				catch( Exception ex )
				{
					tcs.SetException( ex );
				}
			} ) );

			return tcs.Task;

			//return Task.Run( () => RunRetagging( musicRootDirectory, readOnly, undo, genreRules, listener ) );
		}

		public static void RunRetagging( RetaggingOptions options, IProgress<IReadOnlyList<String>> directoriesProgress, IProgress<DirectoryResult> directoryProgress, CancellationToken cancellationToken )
		{
			using( LogFiles logs = LogFiles.Create( options.MusicRootDirectory ) )
			{
				try
				{
					RunRetaggingInner( logs, options, directoriesProgress, directoryProgress, cancellationToken );
				}
				catch( Exception ex )
				{
					logs.GeneralLogWriteLine( "Unhandled {0}: {1} at {2:yyyy-MM-dd HH:mm:ss} UTC\r\n".FormatInvariant( ex.GetType().Name, ex.Message, DateTime.UtcNow ) );
					throw;
				}
			}
		}

		private static void RunRetaggingInner( LogFiles logs, RetaggingOptions options, IProgress<IReadOnlyList<String>> directoriesProgress, IProgress<DirectoryResult> directoryProgress, CancellationToken cancellationToken )
		{
			Stopwatch sw = Stopwatch.StartNew();

			logs.GeneralLogWriteLine( "Started at {0:yyyy-MM-dd HH:mm:ss} UTC\r\n".FormatInvariant( DateTime.UtcNow ) );

			List<String> directories = GetDirectories( options.DirectoryFilterPredicate, options.MusicRootDirectory );

			logs.GeneralLogWriteLine( "Enumerated directories after {0:N2}ms\r\n".FormatInvariant( sw.ElapsedMilliseconds ) );

			directoriesProgress.Report( directories );

			Int32 i = 0;
			foreach( String directoryPath in directories )
			{
				if( cancellationToken.IsCancellationRequested ) break;

				DirectoryResult result = ProcessDirectory( directoryPath, options.ReadOnly, options.Undo, options.GenreRules, logs );

				directoryProgress.Report( result );
				i++;
			}

			if( cancellationToken.IsCancellationRequested )
			{
				logs.GeneralLogWriteLine( "Cancelled after processing {0} directories at {1:yyyy-MM-dd HH:mm:ss} UTC\r\n".FormatInvariant( i, DateTime.UtcNow ) );
			}
			else
			{
				logs.DeleteDirectoryLog();
				logs.GeneralLogWriteLine( "Completed processing {0} directories successfully at {1:yyyy-MM-dd HH:mm:ss} UTC\r\n".FormatInvariant( i, DateTime.UtcNow ) );
			}
		}

		public static List<String> GetDirectories( IDirectoryPredicate excludePredicate, String root )
		{
			// Directory.EnumerateDirectories returns strings without a trailing slash.

			DirectoryInfo rootDir = new DirectoryInfo( root );

			List<String> list = new List<String>();
			list.Add( root );
			list.AddRange(
				Directory
					.EnumerateDirectories( root, "*", SearchOption.AllDirectories /* AllDirectories == include all descendant directories and reparse points */ )
					.Where( s => s != null ) // I don't know why I was seeing nulls... was I?
					.Where( directoryPath => !excludePredicate.Matches( rootDir, new DirectoryInfo( directoryPath ) ) )
			);
			list.Sort();

			return list;
		}

		/// <param name="readOnly">If true, then warnings and errors will be generated, but files will not be modified.</param>
		private static DirectoryResult ProcessDirectory(String directoryPath, Boolean readOnly, Boolean undo, GenreRules genreRules, LogFiles logs)
		{
			List<Message> messages = new List<Message>();

			if( logs.doneDirectories.Contains( directoryPath ) )
			{
				String text = "Directory \"" + directoryPath + "\" is listed in already-done log: \"" + logs.directoryLogFileName + "\" so it was skipped. Delete the log file (or remove the directory's entry) to process this directory again.";
				messages.AddInfoDirectory( directoryPath, text );

				return new DirectoryResult( directoryPath, FolderType.Skipped, 0, 0, 0, messages );
			}

			(FolderType folderType, Int32 modifiedCountProposed, Int32 modifiedCountActual, Int32 totalCount) = TeslaTagFolderProcessor.Process( directoryPath, readOnly, undo, genreRules, messages );

			try
			{
				logs.GeneralLogWriteLines( messages.Select( msg => msg.ToString() ) );
			}
			catch( Exception ex )
			{
				String text = String.Format( CultureInfo.InvariantCulture, @"Couldn't write to log file ""{0}"". Exception: {1}, Message: ""{2}"".", logs.generalLogFileName, ex.GetType().Name, ex.Message );
				messages.Add( new Message( MessageSeverity.Error, directoryPath, directoryPath, text ) );
			}
			
			try
			{
				logs.DirectoryLogWriteLine( directoryPath );
			}
			catch( Exception ex )
			{
				String text = String.Format( CultureInfo.InvariantCulture, @"Couldn't write to log file ""{0}"". Exception: {1}, Message: ""{2}"".", logs.directoryLogFileName, ex.GetType().Name, ex.Message );
				messages.Add( new Message( MessageSeverity.Error, directoryPath, directoryPath, text ) );
			}

			return new DirectoryResult( directoryPath, folderType, totalCount, modifiedCountProposed, modifiedCountActual, messages );
		}
	}
}
