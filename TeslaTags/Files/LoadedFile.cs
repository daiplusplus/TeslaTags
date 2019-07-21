using System;
using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;

using TagLib;

using mpeg = TagLib.Mpeg;
using aac = TagLib.Aac;

namespace TeslaTags
{
	using RecoveryDict = Dictionary<String, RecoveryTag>;

	public abstract class LoadedFile : IDisposable
	{
		#region Static factory

		public static LoadedFile LoadFromFile(FileInfo fi, List<Message> messages)
		{
			String extension = fi.Extension.ToUpperInvariant();
			switch( extension )
			{
			case ".MP3":
				{
					LoadedFile loadedMpegFile = MpegLoadedFile.Create( fi, messages );
					return loadedMpegFile;
				}
			case ".FLAC":
				{
					LoadedFile loadedFlacFile = FlacLoadedFile.Create( fi, messages );
					return loadedFlacFile;
				}
			case ".WAV":
				{
					LoadedFile loadedRiffFile = RiffLoadedFile.Create( fi, messages );
					return loadedRiffFile;
				}
			case ".OGG":
				{
					LoadedFile loadedOggFile = OggLoadedFile.Create( fi, messages );
					return loadedOggFile;
				}
			default:
				return null;
			}
		}

		protected static T Load<T>( FileInfo fileInfo, List<Message> messages )
			where T : TagLib.File
		{
			TagLib.File file;
			try
			{
				file = TagLib.File.Create( fileInfo.FullName );
			}
			catch(Exception ex)
			{
				messages.AddFileError( fileInfo.FullName, "Could not load file: " + ex.Message );
				return null;
			}
			
			Boolean anyErrors = messages.AddFileCorruptionErrors( fileInfo.FullName, file.CorruptionReasons );
			if( !anyErrors && file is T typedFile )
			{
				return typedFile;
			}

			file.Dispose();
			return null;
		}

		private static (String jsonFileName, RecoveryDict dict) GetRecoveryDict( FileInfo fileInfo, List<Message> messages )
		{
			// Does the file exist?
			String directoryPath = fileInfo.DirectoryName;
			String jsonFileName = Path.Combine( directoryPath, "TeslaTagsRecovery.json" );
			if( !System.IO.File.Exists( jsonFileName ) ) return ( jsonFileName, null );

			String jsonFile = System.IO.File.ReadAllText( jsonFileName );

			RecoveryDict dict = new RecoveryDict( StringComparer.OrdinalIgnoreCase );
			try
			{
				JsonConvert.PopulateObject( jsonFile, dict );

				return ( jsonFileName, dict );
			}
			catch( JsonException je )
			{
				messages.AddFileWarning( fileInfo.FullName, "Couldn't load recovery information from \"" + jsonFileName + "\". Message: " + je.Message );
				return ( jsonFileName, null );
			}
		}
		
		protected static RecoveryTag LoadRecoveryTagFromJsonFile( FileInfo fileInfo, List<Message> messages )
		{
			var (jsonFileName, dict) = GetRecoveryDict( fileInfo, messages );

			if( dict == null ) return null;

			if( dict.TryGetValue( fileInfo.Name, out RecoveryTag tag ) ) return tag;

			return null;
		}

		protected static void SaveRecoveryTagToJsonFile( FileInfo fileInfo, RecoveryTag tag, List<Message> messages )
		{
			if( tag == null || tag.IsEmpty ) return;

			var (jsonFileName, dict) = GetRecoveryDict( fileInfo, messages );

			if( dict == null ) dict = new RecoveryDict();

			dict[ fileInfo.Name ] = tag;

			String jsonFile = JsonConvert.SerializeObject( dict, Formatting.Indented );
			System.IO.File.WriteAllText( jsonFileName, jsonFile );
		}

		#endregion

		protected LoadedFile( FileInfo fileInfo, Tag tag, RecoveryTag recoveryTag )
		{
			this.FileInfo    = fileInfo;
			this.Tag         = tag;
			this.RecoveryTag = recoveryTag ?? new RecoveryTag();
		}

		public void Dispose()
		{
			this.Dispose( disposing: true );
			GC.SuppressFinalize( this );
		}

		protected abstract void Dispose(Boolean disposing);

		public abstract void Save( List<Message> messages );

		public FileInfo FileInfo { get; }
		public Tag      Tag      { get; }

		public Boolean  IsModified { get; set; }

		public RecoveryTag RecoveryTag { get; }
	}
}
