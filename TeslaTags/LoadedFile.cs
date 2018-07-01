using System;
using System.IO;

using TagLib;
using mpeg  = TagLib.Mpeg;
using flac  = TagLib.Flac;
using id3v2 = TagLib.Id3v2;
using aac   = TagLib.Aac;
using ogg   = TagLib.Ogg;
using riff  = TagLib.Riff;

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Text;

namespace TeslaTags
{
	public abstract class LoadedFile : IDisposable
	{
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

		public abstract void Save();

		public FileInfo FileInfo { get; }
		public Tag      Tag      { get; }

		public Boolean  IsModified { get; set; }

		public RecoveryTag RecoveryTag { get; }
	}

	public sealed class MpegLoadedFile : LoadedFile
	{
		// Private frames' "Owner" tag should be a URI or email address:
		// http://id3.org/id3v2.3.0#Private_frame
		private const String teslaTagsPrivateTagOwner = "https://github.com/Jehoel/TeslaTags";

		public static MpegLoadedFile Create( FileInfo fileInfo, List<Message> messages )
		{
			mpeg.AudioFile mpegFile = Load<mpeg.AudioFile>( fileInfo, messages );
			if( mpegFile == null ) return null;

			id3v2.Tag id3v2Tag = (id3v2.Tag)mpegFile.GetTag(TagTypes.Id3v2);
			if( id3v2Tag == null )
			{
				messages.AddFileError( fileInfo.FullName, "Does not contain ID3v2 tag." );
				mpegFile.Dispose();
				return null;
			}

			// Load recovery tag:
			RecoveryTag recoveryTag = new RecoveryTag();
			{
				id3v2.PrivateFrame recoveryTagFrame = id3v2Tag
					.GetFrames<id3v2.PrivateFrame>()
					.Where( pf => pf.Owner == teslaTagsPrivateTagOwner )
					.FirstOrDefault();

				if( recoveryTagFrame != null )
				{
					String recoveryTagJsonStr = recoveryTagFrame.PrivateData.ToString( StringType.UTF8 );
					recoveryTag = JsonConvert.DeserializeObject<RecoveryTag>( recoveryTagJsonStr );
				}
			}

			return new MpegLoadedFile( fileInfo, mpegFile, id3v2Tag, recoveryTag );
		}

		private MpegLoadedFile( FileInfo fileInfo, mpeg.AudioFile mpegFile, Tag tag, RecoveryTag recoveryTag )
			: base( fileInfo, tag, recoveryTag )
		{
			this.MpegAudioFile = mpegFile;
		}

		protected sealed override void Dispose(Boolean disposing)
		{
			if( disposing )
			{
				this.MpegAudioFile.Dispose();
			}
		}

		public override void Save()
		{
			if( this.RecoveryTag.IsSet )
			{
				id3v2.Tag id3v2Tag = (id3v2.Tag)this.MpegAudioFile.GetTag(TagTypes.Id3v2);

				id3v2.PrivateFrame recoveryTagFrame = id3v2Tag
					.GetFrames<id3v2.PrivateFrame>()
					.Where( pf => pf.Owner == teslaTagsPrivateTagOwner )
					.FirstOrDefault();

				if( recoveryTagFrame == null )
				{
					recoveryTagFrame = new id3v2.PrivateFrame( teslaTagsPrivateTagOwner );
					id3v2Tag.AddFrame( recoveryTagFrame );
				}
				
				String newRecoveryTagJsonStr = JsonConvert.SerializeObject( this.RecoveryTag );
				Byte[] newRecoveryTagJsonStrBytes = Encoding.UTF8.GetBytes( newRecoveryTagJsonStr );

				recoveryTagFrame.PrivateData = new ByteVector( newRecoveryTagJsonStrBytes );
			}

			this.MpegAudioFile.Save();
		}

		public mpeg.AudioFile MpegAudioFile { get; }
	}

	public sealed class FlacLoadedFile : LoadedFile
	{
		public static FlacLoadedFile Create( FileInfo fileInfo, List<Message> messages )
		{
			flac.File flacFile = Load<flac.File>( fileInfo, messages );
			if( flacFile == null ) return null;

			flac.Metadata flacTag = (flac.Metadata)flacFile.GetTag(TagTypes.FlacMetadata);
			if( flacTag == null )
			{
				messages.AddFileError( fileInfo.FullName, "Does not contain FLAC metadata." );
				flacFile.Dispose();
				return null;
			}

			return new FlacLoadedFile( fileInfo, flacFile, flacTag );
		}

		private FlacLoadedFile( FileInfo fileInfo, flac.File flacFile, Tag tag )
			: base( fileInfo, tag, null )
		{
			this.FlacAudioFile = flacFile;
		}

		protected sealed override void Dispose(Boolean disposing)
		{
			if( disposing )
			{
				this.FlacAudioFile.Dispose();
			}
		}

		public override void Save()
		{
			this.FlacAudioFile.Save();
		}

		public flac.File FlacAudioFile { get; }
	}

	public sealed class OggLoadedFile : LoadedFile
	{
		public static OggLoadedFile Create( FileInfo fileInfo, List<Message> messages )
		{
			ogg.File oggFile = Load<ogg.File>( fileInfo, messages );
			if( oggFile == null ) return null;

			ogg.XiphComment oggTag = (ogg.XiphComment)oggFile.GetTag(TagTypes.Xiph);
			if( oggTag == null )
			{
				messages.AddFileError( fileInfo.FullName, "Does not contain XIPH comment data." );
				oggFile.Dispose();
				return null;
			}

			return new OggLoadedFile( fileInfo, oggFile, oggTag );
		}

		private OggLoadedFile( FileInfo fileInfo, ogg.File oggFile, Tag tag )
			: base( fileInfo, tag, null )
		{
			this.OggAudioFile = oggFile;
		}

		protected sealed override void Dispose(Boolean disposing)
		{
			if( disposing )
			{
				this.OggAudioFile.Dispose();
			}
		}

		public override void Save()
		{
			this.OggAudioFile.Save();
		}

		public ogg.File OggAudioFile { get; }
	}

	public sealed class RiffLoadedFile : LoadedFile
	{
		public static RiffLoadedFile Create( FileInfo fileInfo, List<Message> messages )
		{
			riff.File riffFile = Load<riff.File>( fileInfo, messages );
			if( riffFile == null ) return null;

			riff.InfoTag riffTag = (riff.InfoTag)riffFile.GetTag(TagTypes.RiffInfo);
			if( riffTag == null )
			{
				messages.AddFileError( fileInfo.FullName, "Does not contain RIFF info data." );
				riffFile.Dispose();
				return null;
			}

			return new RiffLoadedFile( fileInfo, riffFile, riffTag );
		}

		private RiffLoadedFile( FileInfo fileInfo, riff.File riffFile, Tag tag )
			: base( fileInfo, tag, null )
		{
			this.RiffAudioFile = riffFile;
		}

		protected sealed override void Dispose(Boolean disposing)
		{
			if( disposing )
			{
				this.RiffAudioFile.Dispose();
			}
		}

		public override void Save()
		{
			this.RiffAudioFile.Save();
		}

		public riff.File RiffAudioFile { get; }
	}
}
