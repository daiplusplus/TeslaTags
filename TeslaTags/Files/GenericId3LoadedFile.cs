using System;
using System.Collections.Generic;

using id3v1 = TagLib.Id3v1;
using id3v2 = TagLib.Id3v2;
using ape   = TagLib.Ape;
using System.IO;

namespace TeslaTags
{
	public sealed class GenericId3LoadedFile : LoadedFile
	{
		public static Boolean TryCreate( FileInfo fileInfo, TagLib.File file, List<Message> messages, out LoadedFile loadedFile )
		{
			// Many files use ID3v1 and ID3v2 and APE besides MP3. Try them all here.

			// Note that `file.GetTag( TagTypes.Id3v1 )` may work while `file.GetTag( TagTypes.AllTags )` returns null - the TagLib library is weird.

			{
				TagLib.Tag id3v2TagMaybe = file.GetTag( TagLib.TagTypes.Id3v2, create: false );
				if( id3v2TagMaybe != null )
				{
					if( id3v2TagMaybe is id3v2.Tag id3v2Tag )
					{
						RecoveryTag recoveryTag = LoadRecoveryTagFromJsonFile( fileInfo, messages );

						loadedFile = new GenericId3LoadedFile( fileInfo, file, id3v2Tag, recoveryTag );
						return true;
					}
					else
					{
						messages.AddFileError( fileInfo.FullName, "Expected " + typeof(id3v2.Tag).FullName + " but TagLib.File.GetTag( Id3v2 ) returned " + id3v2TagMaybe.GetType().FullName );

						loadedFile = default;
						return false;
					}
				}
			}

			{
				TagLib.Tag id3v1TagMaybe = file.GetTag( TagLib.TagTypes.Id3v1, create: false );
				if( id3v1TagMaybe != null )
				{
					if( id3v1TagMaybe is id3v1.Tag id3v1Tag )
					{
						RecoveryTag recoveryTag = LoadRecoveryTagFromJsonFile( fileInfo, messages );

						loadedFile = new GenericId3LoadedFile( fileInfo, file, id3v1Tag, recoveryTag );
						return true;
					}
					else
					{
						messages.AddFileError( fileInfo.FullName, "Expected " + typeof(id3v1.Tag).FullName + " but TagLib.File.GetTag( Id3v1 ) returned " + id3v1TagMaybe.GetType().FullName );

						loadedFile = default;
						return false;
					}
				}
			}

			loadedFile = default;
			return false;
		}

		private GenericId3LoadedFile( FileInfo fileInfo, TagLib.File file, TagLib.Tag tag, RecoveryTag recoveryTag )
			: base( fileInfo, tag, recoveryTag )
		{
			this.TagLibFile = file;
		}

		public TagLib.File TagLibFile { get; }

		public override void Save( List<Message> messages )
		{
			this.TagLibFile.Save();
			SaveRecoveryTagToJsonFile( this.FileInfo, this.RecoveryTag, messages );
		}

		protected override void Dispose( Boolean disposing )
		{
			if( disposing )
			{
				this.TagLibFile.Dispose();
			}
		}
	}
}
