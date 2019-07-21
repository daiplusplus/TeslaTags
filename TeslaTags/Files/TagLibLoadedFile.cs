using System;
using System.Collections.Generic;
using System.IO;

using TagLib;

namespace TeslaTags
{
	public abstract class TagLibLoadedFile<TFile> : LoadedFile
		where TFile : TagLib.File
	{
		protected TagLibLoadedFile( FileInfo fileInfo, TFile tagLibFile, Tag tag, RecoveryTag recoveryTag )
			: base( fileInfo, tag, recoveryTag )
		{
			this.TagLibFile = tagLibFile ?? throw new ArgumentNullException(nameof(tagLibFile));
		}

		public sealed override void Save( List<Message> messages )
		{
			this.TagLibFile.Save();
			SaveRecoveryTagToJsonFile( this.FileInfo, this.RecoveryTag, messages );
		}

		protected sealed override void Dispose( Boolean disposing ) // Sealed Dispose method... ugly.
		{
			if( disposing )
			{
				this.TagLibFile.Dispose();
			}
		}

		public TFile TagLibFile { get; }
	}
}
