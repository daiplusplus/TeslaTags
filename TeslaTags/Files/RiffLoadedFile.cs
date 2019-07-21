using System;
using System.Collections.Generic;
using System.IO;

using TagLib;
using riff = TagLib.Riff;

namespace TeslaTags
{
	public sealed class RiffLoadedFile : TagLibLoadedFile<riff.File>
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

			RecoveryTag recoveryTag = LoadRecoveryTagFromJsonFile( fileInfo, messages );

			return new RiffLoadedFile( fileInfo, riffFile, riffTag, recoveryTag );
		}

		private RiffLoadedFile( FileInfo fileInfo, riff.File riffFile, Tag tag, RecoveryTag recoveryTag )
			: base( fileInfo, riffFile, tag, recoveryTag )
		{
		}
	}
}
