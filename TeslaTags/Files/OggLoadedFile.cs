using System;
using System.Collections.Generic;
using System.IO;

using TagLib;
using ogg = TagLib.Ogg;

namespace TeslaTags
{
	public sealed class OggLoadedFile : TagLibLoadedFile<ogg.File>
	{
		public static Boolean TryCreate( FileInfo fileInfo, ogg.File oggFile, List<Message> messages, out LoadedFile loadedFile )
		{
			ogg.XiphComment oggTag = (ogg.XiphComment)oggFile.GetTag(TagTypes.Xiph);
			if( oggTag == null )
			{
				messages.AddFileError( fileInfo.FullName, "Does not contain XIPH comment data." );
				loadedFile = default;
				return false;
			}
			else
			{
				RecoveryTag recoveryTag = LoadRecoveryTagFromJsonFile( fileInfo, messages );

				loadedFile = new OggLoadedFile( fileInfo, oggFile, oggTag, recoveryTag );
				return true;
			}
		}

		private OggLoadedFile( FileInfo fileInfo, ogg.File oggFile, Tag tag, RecoveryTag recoveryTag )
			: base( fileInfo, oggFile, tag, null )
		{
		}
	}
}
