using System;
using System.IO;

using TagLib;
using mpeg  = TagLib.Mpeg;
using flac  = TagLib.Flac;
using id3v2 = TagLib.Id3v2;

using System.Collections.Generic;

namespace TeslaTags
{
	public abstract class LoadedFile : IDisposable
	{
		protected static T Load<T>( FileInfo fileInfo, List<Message> messages )
			where T : TagLib.File
		{
			TagLib.File file;
			try
			{
				file = TagLib.File.Create( fileInfo.FullName );
			}
			catch(Exception ex)
			{
				messages.AddFileError( fileInfo.FullName, "Could not load file: " + ex.Message );
				return null;
			}
			
			Boolean anyErrors = messages.AddFileCorruptionErrors( fileInfo.FullName, file.CorruptionReasons );
			if( !anyErrors && file is T typedFile )
			{
				return typedFile;
			}

			file.Dispose();
			return null;
		}

		protected LoadedFile( FileInfo fileInfo, Tag tag )
		{
			this.FileInfo = fileInfo;
			this.Tag      = tag;
		}

		public abstract void Dispose();

		public abstract void Save();

		public FileInfo FileInfo { get; }
		public Tag      Tag      { get; }

		public Boolean  IsModified { get; set; }
	}

	public sealed class MpegLoadedFile : LoadedFile
	{
		public static MpegLoadedFile Create( FileInfo fileInfo, List<Message> messages )
		{
			mpeg.AudioFile mpegFile = Load<mpeg.AudioFile>( fileInfo, messages );
			if( mpegFile == null ) return null;

			id3v2.Tag id3v2Tag = (id3v2.Tag)mpegFile.GetTag(TagTypes.Id3v2);
			if( id3v2Tag == null )
			{
				messages.AddFileError( fileInfo.FullName, "Does not contain ID3v2 tag." );
				mpegFile.Dispose();
				return null;
			}

			return new MpegLoadedFile( fileInfo, mpegFile, id3v2Tag );
		}

		private MpegLoadedFile( FileInfo fileInfo, mpeg.AudioFile mpegFile, Tag tag )
			: base( fileInfo, tag )
		{
			this.MpegAudioFile = mpegFile;
		}

		public override void Dispose()
		{
			this.MpegAudioFile.Dispose();
		}

		public override void Save()
		{
			this.MpegAudioFile.Save();
		}

		public mpeg.AudioFile MpegAudioFile { get; }
	}

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

			return new FlacLoadedFile( fileInfo, flacFile, flacTag );
		}

		private FlacLoadedFile( FileInfo fileInfo, flac.File flacFile, Tag tag )
			: base( fileInfo, tag )
		{
			this.FlacAudioFile = flacFile;
		}

		public override void Dispose()
		{
			this.FlacAudioFile.Dispose();
		}

		public override void Save()
		{
			this.FlacAudioFile.Save();
		}

		public flac.File FlacAudioFile { get; }
	}
}
