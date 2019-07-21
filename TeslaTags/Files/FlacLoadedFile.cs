using System;
using System.Collections.Generic;
using System.IO;

using TagLib;
using flac = TagLib.Flac;

namespace TeslaTags
{
	public sealed class FlacLoadedFile : TagLibLoadedFile<flac.File>
	{
		public static Boolean TryCreate( FileInfo fileInfo, flac.File flacFile, List<Message> messages, out LoadedFile loadedFile )
		{
			flac.Metadata flacTag = (flac.Metadata)flacFile.GetTag(TagTypes.FlacMetadata);
			if( flacTag == null )
			{
				messages.AddFileError( fileInfo.FullName, "Does not contain FLAC metadata." );
				loadedFile = default;
				return false;
			}

			RecoveryTag recoveryTag = LoadRecoveryTagFromJsonFile( fileInfo, messages );

			loadedFile = new FlacLoadedFile( fileInfo, flacFile, flacTag, recoveryTag );
			return true;
		}

		private FlacLoadedFile( FileInfo fileInfo, flac.File flacFile, Tag tag, RecoveryTag recoveryTag )
			: base( fileInfo, flacFile, tag, recoveryTag )
		{
		}
	}
}
