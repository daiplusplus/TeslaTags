using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Newtonsoft.Json;

using TagLib;

using mpeg = TagLib.Mpeg;
using id3v2 = TagLib.Id3v2;

namespace TeslaTags
{
	public sealed class MpegLoadedFile : LoadedFile
	{
		private const Boolean _usePrivateFrameForMp3RecoveryData = true;

		// Private frames' "Owner" tag should be a URI or email address:
		// http://id3.org/id3v2.3.0#Private_frame
		private const String teslaTagsPrivateTagOwner = "https://github.com/Jehoel/TeslaTags";

		public static Boolean TryCreate( FileInfo fileInfo, mpeg.AudioFile mpegAudioFile, List<Message> messages, out LoadedFile loadedFile )
		{
			id3v2.Tag id3v2Tag = (id3v2.Tag)mpegAudioFile.GetTag(TagTypes.Id3v2);
			if( id3v2Tag == null )
			{
				messages.AddFileError( fileInfo.FullName, "Does not contain ID3v2 tag." );
				loadedFile = default;
				return false;
			}

			// Load recovery tag:
			RecoveryTag recoveryTag = null;
			if( _usePrivateFrameForMp3RecoveryData )
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
				else
				{
					recoveryTag = LoadRecoveryTagFromJsonFile( fileInfo, messages );
				}
			}

			if( recoveryTag == null )
			{
				recoveryTag = new RecoveryTag();
			}

			loadedFile = new MpegLoadedFile( fileInfo, mpegAudioFile, id3v2Tag, recoveryTag );
			return true;
		}

		private MpegLoadedFile( FileInfo fileInfo, mpeg.AudioFile mpegAudioFile, Tag tag, RecoveryTag recoveryTag )
			: base( fileInfo, tag, recoveryTag )
		{
			this.MpegAudioFile = mpegAudioFile;
		}

		protected sealed override void Dispose(Boolean disposing)
		{
			if( disposing )
			{
				this.MpegAudioFile.Dispose();
			}
		}

		public override void Save(List<Message> messages)
		{
			if( this.RecoveryTag.IsSet )
			{
				if( _usePrivateFrameForMp3RecoveryData )
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

				// Also save to JSON file:
				SaveRecoveryTagToJsonFile( this.FileInfo, this.RecoveryTag, messages );
			}

			this.MpegAudioFile.Save();
		}

		public mpeg.AudioFile MpegAudioFile { get; }
	}
}
