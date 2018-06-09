using System;
using System.IO;

using TagLib;
using TagLib.Mpeg;

namespace TeslaTags
{
	public sealed class LoadedFile : IDisposable
	{
		public LoadedFile( FileInfo fileInfo, AudioFile audioFile, TagLib.Id3v2.Tag id3v2Tag )
		{
			this.FileInfo  = fileInfo  ?? throw new ArgumentNullException( nameof( fileInfo ) );
			this.AudioFile = audioFile ?? throw new ArgumentNullException( nameof( audioFile ) );
			this.Id3v2Tag  = id3v2Tag  ?? throw new ArgumentNullException( nameof( id3v2Tag ) );
		}

		public void Dispose()
		{
			this.AudioFile.Dispose();
		}

		public FileInfo  FileInfo  { get; }
		public AudioFile AudioFile { get; }
		public TagLib.Id3v2.Tag Id3v2Tag  { get; }

		public Boolean IsModified { get; set; }
	}
}
