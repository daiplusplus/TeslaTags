using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using TagLib;
using TagLib.Mpeg;

namespace TeslaTags
{
	public static class Folder
	{
		public static FolderType Process(String path)
		{
			List<LoadedFile> files = LoadFiles( path );
			try
			{
				FolderType folderType = DetermineFolderType( files );
				switch( folderType )
				{
					case FolderType.ArtistAlbum:
						Retagger.RetagForArtistAlbum( files );
						break;
					case FolderType.ArtistAlbumWithGuestArtists:
						Retagger.RetagForArtistAlbumWithGuestArtists( files );
						break;
					case FolderType.AssortedFiles:
						Retagger.RetagForAssortedFiles( files );
						break;
					case FolderType.CompilationAlbum:
						Retagger.RetagForCompilationAlbum( files );
						break;
					case FolderType.Container:
						break;
				}
				return folderType;
			}
			finally
			{
				foreach( LoadedFile file in files )
				{
					if( file.IsModified )
					{
						file.AudioFile.Save();
					}
					file.AudioFile.Dispose();
					file.Dispose();
				}
			}
		}

		private static List<LoadedFile> LoadFiles( String path )
		{
			DirectoryInfo di = new DirectoryInfo( path );
			FileInfo[] mp3s = di.GetFiles("*.mp3");

			Dictionary<String,Exception> errorsByFile = new Dictionary<String,Exception>();

			List<LoadedFile> files = new List<LoadedFile>();
			foreach( FileInfo fi in mp3s )
			{
				TagLib.File file = null;
				try
				{
					file = TagLib.File.Create( fi.FullName );
					if( file is AudioFile audioFile )
					{
						Tag id3v2 = file.GetTag(TagTypes.Id3v2);
						files.Add( new LoadedFile( fi, audioFile, id3v2 ) );
					}
				}
				catch(Exception ex)
				{
					errorsByFile.Add( fi.FullName, ex );
					if( file != null ) file.Dispose();
				}
			}

			if( errorsByFile.Count > 0 )
			{
				System.Diagnostics.Debugger.Break();
			}

			return files;
		}

		private static FolderType DetermineFolderType(List<LoadedFile> files)
		{
			if( files.Count == 0 ) return FolderType.Container;

			//List<TagSummary> filesTags = files.Select( f => TagSummary.Create( f ) ).ToList();

			Boolean allVariousArtists = files.All( f => String.Equals( "Various Artists", f.Id3v2Tag.AlbumArtists.SingleOrDefault(), StringComparison.Ordinal ) );

			String firstAlbumArtist = files.First().Id3v2Tag.AlbumArtists.FirstOrDefault();
			Boolean allSameAlbumArtist = files.All( f => String.Equals( firstAlbumArtist, f.Id3v2Tag.AlbumArtists.SingleOrDefault(), StringComparison.Ordinal ) );

			String firstArtist = files.First().Id3v2Tag.Performers.FirstOrDefault();
			Boolean allSameArtist      = files.All( f => String.Equals( firstArtist, f.Id3v2Tag.Performers.SingleOrDefault(), StringComparison.Ordinal ) );

			String firstAlbum = files.First().Id3v2Tag.Album;
			Boolean sameAlbum = files.All( f => String.Equals( firstAlbum, f.Id3v2Tag.Album, StringComparison.Ordinal ) );
			Boolean noAlbum   = files.All( f => String.IsNullOrWhiteSpace( f.Id3v2Tag.Album ) );

			if( allVariousArtists )
			{
				if( noAlbum ) return FolderType.AssortedFiles;

				if( sameAlbum ) return FolderType.CompilationAlbum;

				throw new Exception( "Unexpected folder type. All tracks are AlbumArtist=Various Artists, but with different Album names." );
			}
			else
			{
				if( allSameArtist ) return FolderType.ArtistAlbum;

				if( allSameAlbumArtist ) return FolderType.ArtistAlbumWithGuestArtists;

				throw new Exception( "Unexpected folder type. All tracks have different Artist and AlbumArtist values." );
			}
		}

		class TagSummary
		{
			public static TagSummary Create(TagLib.Id3v2.Tag id3v2Tag)
			{
				return new TagSummary(
					id3v2Tag.FirstPerformer,
					id3v2Tag.FirstAlbumArtist,
					id3v2Tag.Album,
					id3v2Tag.Track,
					id3v2Tag.Disc,
					id3v2Tag.Year
				);
			}

			public TagSummary(String artist, String albumArtist, String album, UInt32 trackNumber, UInt32 discNumber, UInt32 albumYear)
			{
				this.Artist = artist;
				this.AlbumArtist = albumArtist;
				this.Album = album;
				this.TrackNumber = trackNumber;
				this.DiscNumber = discNumber;
				this.AlbumYear = albumYear;
			}

			public String Artist      { get; }
			public String AlbumArtist { get; }
			public String Album       { get; }
			public UInt32 TrackNumber { get; }
			public UInt32 DiscNumber  { get; }
			public UInt32 AlbumYear   { get; }
		}
	}
}
