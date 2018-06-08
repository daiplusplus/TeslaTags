using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TagLib;
using TagLib.Id3v1;
using TagLib.Id3v2;
using TagLib.Ape;
using TagLib.Mpeg;
using System.IO;

namespace TeslaTags
{
	public static class Program
	{
		public static void Main(String[] args)
		{
			// TODO: Strip APE tags?
			// TODO: Warn about inconsistent values
			// TODO: Warn about similar Artists names, e.g. "P.O.D" and "P.O.D." or "The Hives" and "Hives".

			String directory = @"Zero 7 - 2002-02-18 - Another Late Night";
			//String directory = @"UNKLESounds - 2015 - Global Underground 41 Naples";

			directory = Path.Combine( @"C:\Users\David\Desktop\TeslaTagsTest\", directory );

			FileInfo[] fileNames = new DirectoryInfo( directory ).GetFiles();
			Array.Sort( fileNames, (x,y) => x.Name.CompareTo( y.Name ) );

			for( Int32 i = 0; i < fileNames.Length; i++ )
			{
				String fileName = fileNames[i].FullName;
				TagLib.File file;
				try
				{
					file = TagLib.File.Create( fileName );
					if( file == null ) continue;
				}
				catch( Exception ex )
				{
					Console.WriteLine( ex.Message );
					continue;
				}

				using( file )
				{
					if( file is TagLib.Mpeg.AudioFile audioFile )
					{
						Retagger.PrependInvisibleCharactersUsingBase1( audioFile, i, fileNames.Length );

						audioFile.Save();
					}

				}
			}

		}


	}
}
