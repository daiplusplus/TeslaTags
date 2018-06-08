using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TeslaTags
{
	public static class Program
	{
		public static void Main(String[] args)
		{
			HashSet<String> done = new HashSet<String>( StringComparer.OrdinalIgnoreCase );
			foreach( String line in File.ReadAllLines( "Log.txt" ) )
			{
				done.Add( line );
			}

			List<String> directories = Directory
				.EnumerateDirectories( @"D:\Music", "*", SearchOption.AllDirectories )
				.ToList();

			directories.Sort();

			foreach( String directoryPath in directories )
			{
				Console.Write( directoryPath + "..." );

				if( done.Contains( directoryPath ) )
				{
					Console.WriteLine( " Skipped." );
				}
				else
				{
					FolderType folderType = Folder.Process( directoryPath );
					Console.WriteLine( " Done: {0}", folderType );
					done.Add( directoryPath );
					File.AppendAllText( "Log.txt", directoryPath + "\r\n", Encoding.UTF8 );
				}
			}
		}


	}
}
