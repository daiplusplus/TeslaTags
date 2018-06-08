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
			const String DirectoryLog = "DoneDirectories.txt";
			const String ErrorLog     = "Errors.txt";
			const String Log          = "Log.txt";

			if( args.Length != 1 )
			{
				Console.WriteLine( "Usage: TeslaTags.exe <musicDirectory>" );
				return 1;
			}

			HashSet<String> done = new HashSet<String>( StringComparer.OrdinalIgnoreCase );
			foreach( String line in File.ReadAllLines( DirectoryLog ) )
			{
				done.Add( line );
			}

			File.AppendAllText( Log, String.Format( CultureInfo.InvariantCulture, "Started at {0:yyyy-MM-dd HH:mm:ss} UTC\r\n", DateTime.UtcNow ) );

			List<String> directories = Directory
				.EnumerateDirectories( @"D:\Music", "*", SearchOption.AllDirectories )
				.ToList();

			directories.Sort();

			List<String> errors = new List<String>();

			foreach( String directoryPath in directories )
			{
				Console.Write( directoryPath );
				Console.Write( "..." );

				if( done.Contains( directoryPath ) )
				{
					Console.WriteLine( " Skipped." );
				}
				else
				{
					(FolderType folderType, Int32 modifiedCount, Int32 totalCount) = Folder.Process( directoryPath, errors );
					Console.WriteLine( " Done: {0}, Modified: {1} of {2}.", folderType, modifiedCount, totalCount );

					done.Add( directoryPath );
					File.AppendAllText( DirectoryLog, directoryPath + "\r\n", Encoding.UTF8 );
				}
			}

			File.AppendAllLines( ErrorLog, errors );

			File.AppendAllText( Log, String.Format( CultureInfo.InvariantCulture, "Completed at {0:yyyy-MM-dd HH:mm:ss} UTC\r\n", DateTime.UtcNow ) );

			return 0;
		}


	}
}
