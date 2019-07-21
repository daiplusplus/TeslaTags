using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TagLib;

using mpeg  = TagLib.Mpeg;
using aac   = TagLib.Aac;
using mpeg4 = TagLib.Mpeg4;
using id3v2 = TagLib.Id3v2;

namespace TeslaTags
{
	public sealed class Mp4LoadedFile : TagLibLoadedFile<mpeg4.File>
	{
		// MP4 files can have either Apple tags or "ISO" tags. We don't support ISO tags.

		public static Boolean TryCreate( FileInfo fileInfo, mpeg4.File mp4File, List<Message> messages, out LoadedFile loadedFile )
		{
			mpeg4.AppleTag appleTag = (mpeg4.AppleTag)mp4File.GetTag( TagTypes.Apple );
			if( appleTag == null )
			{
				messages.AddFileError( fileInfo.FullName, "Does not contain an Apple tag." );
				loadedFile = default;
				return false;
			}
			else
			{
				RecoveryTag recoveryTag = LoadRecoveryTagFromJsonFile( fileInfo, messages );

				loadedFile = new Mp4LoadedFile( fileInfo, mp4File, appleTag, recoveryTag );
				return true;
			}
		}

		private Mp4LoadedFile( FileInfo fileInfo, mpeg4.File mp4File, Tag tag, RecoveryTag recoveryTag )
			: base( fileInfo, mp4File, tag, null )
		{
		}
	}
}
