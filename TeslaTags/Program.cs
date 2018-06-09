using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace TeslaTags
{
	public static class Program
	{
		public static Int32 Main(String[] args)
		{
			if( args.Length < 1 )
			{
				Console.WriteLine( "Usage: TeslaTags.exe <musicDirectory> [<readOnly> = false]" );
				return 1;
			}

			String root = args[0];
			Boolean readOnly = Boolean.Parse( args.ElementAtOrDefault(1) ?? "false" ); // `Boolean.Parse` is case-insensitive
			
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

					DirectoryResult result = tp.ProcessDirectory( directoryPath, readOnly );


					Console.Write( result.FolderType );
					Console.Write( "{0} files, {1} modified files, ", result.TotalFiles, result.ModifiedFiles );

					if( result.Messages.Any( m => m.Severity == MessageSeverity.Error ) ) Console.ForegroundColor = ConsoleColor.Red;
					Console.Write( "{0} errors", result.Messages.Count( m => m.Severity == MessageSeverity.Error ) );
					Console.ResetColor();

					Console.Write( ", " );

					if( result.Messages.Any( m => m.Severity == MessageSeverity.Warning ) ) Console.ForegroundColor = ConsoleColor.Yellow;
					Console.Write( "{0} warnings", result.Messages.Count( m => m.Severity == MessageSeverity.Warning ) );
					Console.ResetColor();

					Console.WriteLine();
				}
			}

			return 0;
		}
	}

	public enum MessageSeverity
	{
		Info,
		Warning,
		Error
	}

	public class Message
	{
		public Message(MessageSeverity severity, String directory, String path, String text)
		{
			this.Severity = severity;
			this.FullPath = path ?? throw new ArgumentNullException(nameof(path));
			this.IsDirectory = String.Equals( directory, path, StringComparison.OrdinalIgnoreCase );
			this.RelativePath = path.StartsWith( directory, StringComparison.OrdinalIgnoreCase ) ? path.Substring( directory.Length ) : path;
			this.Text = text ?? throw new ArgumentNullException(nameof(text));
		}

		public MessageSeverity Severity { get; }
		public String FullPath { get; }
		public String RelativePath { get; }
		public Boolean IsDirectory { get; }
		public String Text { get; }

		private String toString;

		public override String ToString()
		{
			return this.toString ?? ( this.toString = String.Concat( this.FullPath, "\t", this.Severity.ToString(), "\t", this.Text ) );
		}
	}

	public class DirectoryResult
	{
		private static readonly List<Message> _empty = new List<Message>();

		public DirectoryResult(FolderType folderType, Int32 totalFiles, Int32 modifiedFiles, List<Message> messages)
		{
			this.FolderType    = folderType;
			this.TotalFiles    = totalFiles;
			this.ModifiedFiles = modifiedFiles;
			this.Messages      = messages ?? _empty;
		}

		public FolderType FolderType { get; }
		public Int32 TotalFiles { get; }
		public Int32 ModifiedFiles { get; }
		public List<Message> Messages { get; }
	}

	public sealed class TeslaTagsProcesser : IDisposable
	{
		const String DirectoryLog = "TeslaTags_DoneDirectories.txt";
		const String GeneralLog   = "TeslaTags_Log.txt";

		private readonly String root;
		private readonly String directoryLog;
		private readonly String generalLog;

		private readonly HashSet<String> done = new HashSet<String>( StringComparer.OrdinalIgnoreCase );

		public TeslaTagsProcesser(String root)
		{
			if( String.IsNullOrWhiteSpace( root ) ) throw new ArgumentNullException( nameof(root) );

			if( !Path.IsPathRooted( root ) )
			{
				root = root.TrimEnd( '\\' ); // Remove any trailing backslashes.
			}

			this.root = root;

			this.directoryLog = Path.Combine( root, DirectoryLog );
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
			// Directory.EnumerateDirectories returns strings without a trailing slash.

			List<String> directories = Directory
				.EnumerateDirectories( this.root, "*", SearchOption.AllDirectories )
				.ToList();

			directories.Add( this.root );

			directories.Sort();

			return directories;
		}

		/// <param name="readOnly">If true, then warnings and errors will be generated, but files will not be modified.</param>
		public DirectoryResult ProcessDirectory(String directory, Boolean readOnly)
		{
			List<Message> messages = new List<Message>();

			if( this.done.Contains( directory ) )
			{
				messages.Add( new Message( MessageSeverity.Info, directory, directory, "Directory \"" + directory + "\" is listed in already-done log: \"" + this.directoryLog + "\" so it was skipped. Delete the log file (or remove the directory's entry) to process this directory again." ) );

				return new DirectoryResult( FolderType.Skipped, 0, 0, messages );
			}

			(FolderType folderType, Int32 modifiedCount, Int32 totalCount) = Folder.Process( directory, readOnly, messages );

			File.AppendAllLines( this.generalLog, messages.Select( msg => msg.ToString() ) ); // AppendAllLines writes a terminal `\r\n`

			File.AppendAllText( this.directoryLog, directory + "\r\n" );

			return new DirectoryResult( folderType, totalCount, modifiedCount, messages );
		}
	}


}
