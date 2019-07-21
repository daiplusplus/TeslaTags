using System;
using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;

using TagLib;

using aiff   = TagLib.Aiff;
using asf    = TagLib.Asf;
using audi   = TagLib.Audible;
using matr   = TagLib.Matroska;
using mpeg   = TagLib.Mpeg;
using mp4    = TagLib.Mpeg4;
using aac    = TagLib.Aac;
using ape    = TagLib.Ape;
using flac   = TagLib.Flac;
using muse   = TagLib.MusePack;
using wavp   = TagLib.WavPack;
using ogg    = TagLib.Ogg;
using riff   = TagLib.Riff;

namespace TeslaTags
{
	using RecoveryDict = Dictionary<String, RecoveryTag>;

	public abstract class LoadedFile : IDisposable
	{
		#region Static factory

		public static Boolean TryLoadFromFile( FileInfo fileInfo, List<Message> messages, out LoadedFile loadedFile )
		{
			if( fileInfo == null ) throw new ArgumentNullException(nameof(fileInfo));
			if( messages == null ) throw new ArgumentNullException(nameof(messages));
			
			//

			TagLib.File tagLibFile;
			try
			{
				tagLibFile = TagLib.File.Create( fileInfo.FullName ); // Never returns null.
			}
			catch( UnsupportedFormatException ufEx )
			{
				messages.AddFileError( fileInfo.FullName, "Unsupported format: " + ufEx.Message );
				loadedFile = null;
				return false;
			}
			catch( CorruptFileException cfEx )
			{
				messages.AddFileError( fileInfo.FullName, "Corrupted file: " + cfEx.Message );
				loadedFile = null;
				return false;
			}
			catch( Exception ex )
			{
				messages.AddFileError( fileInfo.FullName, "Could not load file: " + ex.Message );
				loadedFile = null;
				return false;
			}

			//

			Boolean isLoaded = false;
			try
			{
				isLoaded = TryCreateFromTagLibFile( fileInfo, tagLibFile, messages, out loadedFile );
				return isLoaded;
			}
			finally
			{
				if( !isLoaded )
				{
					tagLibFile.Dispose();
				}
			}
		}

		private static Boolean TryCreateFromTagLibFile( FileInfo fileInfo, TagLib.File tagLibFile, List<Message> messages, out LoadedFile loadedFile )
		{
			switch( tagLibFile )
			{
			case aiff.File aiffFile:
				return GenericId3LoadedFile.TryCreate( fileInfo, aiffFile, messages, out loadedFile ); // AIFF uses Id3v2.
			
			case mpeg.AudioFile mpegAudioFile:
				return MpegLoadedFile.TryCreate( fileInfo, mpegAudioFile, messages, out loadedFile );

			case mpeg.File mpegFile: 
				return GenericId3LoadedFile.TryCreate( fileInfo, mpegFile, messages, out loadedFile ); // MPEG files can optionally use Id3v2, Id3v1, or APE.

			case mp4.File mp4File:
				return Mp4LoadedFile.TryCreate( fileInfo, mp4File, messages, out loadedFile );

			case aac.File aacFile:
				return GenericId3LoadedFile.TryCreate( fileInfo, aacFile, messages, out loadedFile ); // AAC uses Id3v2, Id3v1, or APE.

			case flac.File flacFile:
				return FlacLoadedFile.TryCreate( fileInfo, flacFile, messages, out loadedFile );

			case ape.File apeFile: // Tesla doesn't support the APE file format, and especially not APE-format tags when present in other files like MP3. While APE files can contain Id3v2 tags, it's moot.
				return GenericId3LoadedFile.TryCreate( fileInfo, apeFile, messages, out loadedFile ); // AIFF optionally uses Id3v2, Id3v1, or APE.

			case muse.File museFile:
				return GenericId3LoadedFile.TryCreate( fileInfo, museFile, messages, out loadedFile ); // MUSE optionally uses Id3v2, Id3v1, or APE.

			case wavp.File wavpFile:
				return GenericId3LoadedFile.TryCreate( fileInfo, wavpFile, messages, out loadedFile ); // WavPack optionally uses Id3v2, Id3v1, or APE.

			case ogg.File oggFile:
				return OggLoadedFile.TryCreate( fileInfo, oggFile, messages, out loadedFile );

			case riff.File riffFile:     // RIFF has its own tag format.
			case asf.File  asfFile:      // ASF has its own tag format.
			case audi.File audibleFile:  // Audible has its own tag format.
			case matr.File matroskaFile: // Matroska has its own tag format.
			default:
				messages.AddFileWarning( fileInfo.FullName, "Unsupported TagLib file type ({0}).", tagLibFile.GetType().FullName );
				loadedFile = default;
				return false;
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

			if( file == null )
			{
				messages.AddFileError( fileInfo.FullName, "Could not load file: TagLib.File.Create() returned null." );
				return null;
			}
			
			Boolean anyErrors = messages.AddFileCorruptionErrors( fileInfo.FullName, file.CorruptionReasons );
			if( !anyErrors )
			{
				if( file is T typedFile )
				{
					return typedFile;
				}
				else
				{
					messages.AddFileError( fileInfo.FullName, "Could not load file. Expected " + typeof(T).FullName + " but TagLib returned " + file.GetType().FullName + "." );
					return null;
				}
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
