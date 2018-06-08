using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using TagLib;
using TagLib.Mpeg;

namespace TeslaTags
{
	static class Folder
	{
		public static (FolderType folderType, Int32 modifiedCount, Int32 totalCount) Process(String path, List<String> errors)
		{
			List<LoadedFile> files = LoadFiles( path );
			try
			{
				FolderType folderType = DetermineFolderType( files );
				Int32 modifiedCount = 0;
				switch( folderType )
				{
					case FolderType.ArtistAlbum:
						Retagger.RetagForArtistAlbum( files, errors );
						break;
					case FolderType.ArtistAlbumWithGuestArtists:
						modifiedCount = Retagger.RetagForArtistAlbumWithGuestArtists( files, errors );
						break;
					case FolderType.ArtistAssorted:
						modifiedCount = Retagger.RetagForArtistAssortedFiles( files, errors );
						break;
					case FolderType.AssortedFiles:
						modifiedCount = Retagger.RetagForAssortedFiles( files );
						break;
					case FolderType.CompilationAlbum:
						modifiedCount = Retagger.RetagForCompilationAlbum( files, errors );
						break;
					case FolderType.Container:
						break;
				}
				return (folderType, modifiedCount, files.Count);
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
						TagLib.Id3v2.Tag id3v2 = (TagLib.Id3v2.Tag)file.GetTag(TagTypes.Id3v2);
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

			List<TagSummary> filesTags = files.Select( f => TagSummary.Create( f.Id3v2Tag ) ).ToList();

			Boolean allVariousArtists  = filesTags.All( ft => ft.AlbumArtist.EqualsCI( "Various Artists" ) ); //files.All( f => String.Equals( "Various Artists", f.Id3v2Tag.AlbumArtists.SingleOrDefault(), StringComparison.Ordinal ) );

			String  firstAlbumArtist   = filesTags.First().AlbumArtist; //files.First().Id3v2Tag.AlbumArtists.FirstOrDefault();
			Boolean allSameAlbumArtist = filesTags.All( ft => ft.AlbumArtist.EqualsCI( firstAlbumArtist ) ); //files.All( f => String.Equals( firstAlbumArtist, f.Id3v2Tag.AlbumArtists.SingleOrDefault(), StringComparison.Ordinal ) );

			String  firstArtist        = filesTags.First().Artist; //files.First().Id3v2Tag.Performers.FirstOrDefault();
			Boolean allSameArtist      = filesTags.All( ft => ft.Artist.EqualsCI( firstArtist ) ); //files.All( f => String.Equals( firstArtist, f.Id3v2Tag.Performers.SingleOrDefault(), StringComparison.Ordinal ) );

			String  firstAlbum         = filesTags.First().Album; //files.First().Id3v2Tag.Album;
			Boolean sameAlbum          = filesTags.All( ft => ft.Album.EqualsCI( firstAlbum ) ); //files.All( f => String.Equals( firstAlbum, f.Id3v2Tag.Album, StringComparison.Ordinal ) );
			Boolean noAlbum            = filesTags.All( ft => String.IsNullOrWhiteSpace( ft.Album ) ); //files.All( f => String.IsNullOrWhiteSpace( f.Id3v2Tag.Album ) );

			if( allVariousArtists )
			{
				if( noAlbum ) return FolderType.AssortedFiles;

				if( sameAlbum ) return FolderType.CompilationAlbum;

				throw new Exception( "Unexpected folder type. All tracks are AlbumArtist=Various Artists, but with different Album names." );
			}
			else
			{
				if( allSameArtist && sameAlbum ) return FolderType.ArtistAlbum;

				if( allSameAlbumArtist && sameAlbum ) return FolderType.ArtistAlbumWithGuestArtists;

				if( noAlbum ) return FolderType.ArtistAssorted;

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
				this.Artist      = artist;
				this.AlbumArtist = albumArtist;
				this.Album       = album;
				this.TrackNumber = trackNumber;
				this.DiscNumber  = discNumber;
				this.AlbumYear   = albumYear;
			}

			public String  Artist      { get; }
			public String  AlbumArtist { get; }
			public String  Album       { get; }
			public UInt32  TrackNumber { get; }
			public UInt32  DiscNumber  { get; }
			public UInt32  AlbumYear   { get; }
			public Boolean HasAlbumArt { get; }
		}
	}

	public static class Extensions
	{
		public static Boolean EqualsCI( this String x, String y )
		{
			return String.Equals( x, y, StringComparison.OrdinalIgnoreCase );
		}
	}
}
