using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace TeslaTags
{
	public static class Program
	{
		public static Int32 Main(String[] args)
		{
			if( args.Length != 1 )
			{
				Console.WriteLine( "Usage: TeslaTags.exe <musicDirectory>" );
				return 1;
			}

			String root = args[0];
			
			using( TeslaTagsProcesser tp = new TeslaTagsProcesser( root ) )
			{
				List<String> directories = tp.GetDirectories();
				if( directories.Count == 0 )
				{
					Console.WriteLine( "Directory {0} is empty.", root );
				}

				foreach( String directoryPath in directories )
				{
					Console.Write( directoryPath );
					Console.Write( "..." );

					DirectoryResult result = tp.ProcessDirectory( directoryPath );


					Console.Write( result.FolderType );
					Console.Write( "{0} files, {1} modified files, ", result.TotalFiles, result.ModifiedFiles );

					if( result.Errors.Count > 0 ) Console.ForegroundColor = ConsoleColor.Red;
					Console.Write( "{0} errors", result.Errors.Count );
					Console.ResetColor();

					Console.Write( ", " );

					if( result.Errors.Count > 0 ) Console.ForegroundColor = ConsoleColor.Yellow;
					Console.Write( "{0} warnings", result.Warnings.Count );
					Console.ResetColor();

					Console.WriteLine();
				}
			}

			return 0;
		}
	}

	public class DirectoryResult
	{
		private static readonly List<String> _empty = new List<String>();

		public DirectoryResult(FolderType folderType, Int32 totalFiles, Int32 modifiedFiles, List<String> errors, List<String> warnings)
		{
			this.FolderType    = folderType;
			this.TotalFiles    = totalFiles;
			this.ModifiedFiles = modifiedFiles;
			this.Errors        = errors   ?? _empty;
			this.Warnings      = warnings ?? _empty;
		}

		public FolderType FolderType { get; }
		public Int32 TotalFiles { get; }
		public Int32 ModifiedFiles { get; }
		public List<String> Errors { get; }
		public List<String> Warnings { get; }
	}

	public sealed class TeslaTagsProcesser : IDisposable
	{
		const String DirectoryLog = "TeslaTags_DoneDirectories.txt";
		const String ErrorLog     = "TeslaTags_Errors.txt";
		const String WarningLog   = "TeslaTags_Warnings.txt";
		const String GeneralLog   = "TeslaTags_Log.txt";

		private readonly String root;
		private readonly String directoryLog;
		private readonly String errorLog;
		private readonly String warningLog;
		private readonly String generalLog;

		private readonly HashSet<String> done = new HashSet<String>( StringComparer.OrdinalIgnoreCase );

		public TeslaTagsProcesser(String root)
		{
			this.root = root;

			this.directoryLog = Path.Combine( root, DirectoryLog );
			this.errorLog     = Path.Combine( root, ErrorLog );
			this.warningLog   = Path.Combine( root, WarningLog );
			this.generalLog   = Path.Combine( root, GeneralLog );

			if( File.Exists( this.directoryLog ) )
			{
				foreach( String line in File.ReadAllLines( this.directoryLog ) )
				{
					this.done.Add( line );
				}
			}

			File.AppendAllText( this.generalLog, String.Format( CultureInfo.InvariantCulture, "Started at {0:yyyy-MM-dd HH:mm:ss} UTC\r\n", DateTime.UtcNow ) );
		}

		public void Dispose()
		{
			File.AppendAllText( Path.Combine( this.root, GeneralLog ), String.Format( CultureInfo.InvariantCulture, "Ended at {0:yyyy-MM-dd HH:mm:ss} UTC\r\n", DateTime.UtcNow ) );
		}
		
		public List<String> GetDirectories()
		{
			List<String> directories = Directory
				.EnumerateDirectories( this.root, "*", SearchOption.AllDirectories )
				.ToList();

			directories.Sort();

			return directories;
		}

		public DirectoryResult ProcessDirectory(String directory)
		{
			if( this.done.Contains( directory ) )
			{
				return new DirectoryResult( FolderType.Skipped, 0, 0, null, null );
			}

			List<String> errors   = new List<String>();
			List<String> warnings = new List<String>();

			(FolderType folderType, Int32 modifiedCount, Int32 totalCount) = Folder.Process( directory, errors, warnings );

			File.AppendAllLines( this.errorLog, errors ); // AppendAllLines writes a terminal `\r\n`
			File.AppendAllLines( this.warningLog, warnings );

			File.AppendAllText( this.directoryLog, directory + "\r\n" );

			return new DirectoryResult( folderType, totalCount, modifiedCount, errors, warnings );
		}
	}


}
