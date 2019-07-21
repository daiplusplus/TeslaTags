using System;
using System.Collections.Generic;
using System.IO;

using TagLib;
using flac = TagLib.Flac;

namespace TeslaTags
{
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

			RecoveryTag recoveryTag = LoadRecoveryTagFromJsonFile( fileInfo, messages );

			return new FlacLoadedFile( fileInfo, flacFile, flacTag, recoveryTag );
		}

		private FlacLoadedFile( FileInfo fileInfo, flac.File flacFile, Tag tag, RecoveryTag recoveryTag )
			: base( fileInfo, tag, recoveryTag )
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

		public override void Save(List<Message> messages)
		{
			this.FlacAudioFile.Save();

			SaveRecoveryTagToJsonFile( this.FileInfo, this.RecoveryTag, messages );
		}

		public flac.File FlacAudioFile { get; }
	}
}
