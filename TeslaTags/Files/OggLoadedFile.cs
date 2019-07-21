using System;
using System.Collections.Generic;
using System.IO;

using TagLib;
using ogg = TagLib.Ogg;

namespace TeslaTags
{
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

			RecoveryTag recoveryTag = LoadRecoveryTagFromJsonFile( fileInfo, messages );

			return new OggLoadedFile( fileInfo, oggFile, oggTag, recoveryTag );
		}

		private OggLoadedFile( FileInfo fileInfo, ogg.File oggFile, Tag tag, RecoveryTag recoveryTag )
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

		public override void Save(List<Message> messages)
		{
			this.OggAudioFile.Save();
			SaveRecoveryTagToJsonFile( this.FileInfo, this.RecoveryTag, messages );
		}

		public ogg.File OggAudioFile { get; }
	}
}
