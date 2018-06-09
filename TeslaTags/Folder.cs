using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using TagLib;
using TagLib.Mpeg;

namespace TeslaTags
{
	static class Folder
	{
		public static (FolderType folderType, Int32 modifiedCount, Int32 totalCount) Process(String directoryPath, Boolean readOnly, List<Message> messages)
		{
			List<LoadedFile> files = LoadFiles( directoryPath, messages );
			try
			{
				FolderType folderType = DetermineFolderType( directoryPath, files, messages );
				switch( folderType )
				{
					case FolderType.ArtistAlbum:
						Retagger.RetagForArtistAlbum( files, messages );
						break;
					case FolderType.ArtistAlbumWithGuestArtists:
						Retagger.RetagForArtistAlbumWithGuestArtists( files, messages );
						break;
					case FolderType.ArtistAssorted:
						Retagger.RetagForArtistAssortedFiles( files, messages );
						break;
					case FolderType.AssortedFiles:
						Retagger.RetagForAssortedFiles( files, messages );
						break;
					case FolderType.CompilationAlbum:
						Retagger.RetagForCompilationAlbum( files, messages );
						break;
					case FolderType.Empty:
					case FolderType.UnableToDetermine:
					case FolderType.Skipped:
					default:
						break;
				}

				Int32 modifiedCount = 0;
				foreach( LoadedFile file in files )
				{
					if( !readOnly && file.IsModified )
					{
						try
						{
							file.AudioFile.Save();
						}
						catch(Exception ex)
						{
							messages.Add( new Message( MessageSeverity.Error, directoryPath, file.FileInfo.FullName, "Could not save file: " + ex.Message ) );
						}
						
						modifiedCount++;
					}
				}

				return (folderType, modifiedCount, files.Count);
			}
			finally
			{
				foreach( LoadedFile file in files )
				{
					file.AudioFile.Dispose();
					file.Dispose();
				}
			}
		}

		private static List<LoadedFile> LoadFiles( String directoryPath, List<Message> messages )
		{
			DirectoryInfo di = new DirectoryInfo( directoryPath );
			FileInfo[] mp3s = di.GetFiles("*.mp3");

			List<LoadedFile> files = new List<LoadedFile>();
			foreach( FileInfo fi in mp3s )
			{
				TagLib.File file = null;
				try
				{
					file = TagLib.File.Create( fi.FullName );
					if( file.CorruptionReasons?.Any() ?? false )
					{
						foreach( String reason in file.CorruptionReasons )
						{
							messages.Add( new Message( MessageSeverity.Error, directoryPath, fi.FullName, "File corrupted: " + reason ) );
						}
					}

					if( file is AudioFile audioFile )
					{
						TagLib.Id3v2.Tag id3v2 = (TagLib.Id3v2.Tag)file.GetTag(TagTypes.Id3v2);
						files.Add( new LoadedFile( fi, audioFile, id3v2 ) );
					}
					else
					{
						file.Dispose();
					}
				}
				catch(Exception ex)
				{
					messages.Add( new Message( MessageSeverity.Error, directoryPath, fi.FullName, "Could not load file: " + ex.Message ) );
					if( file != null ) file.Dispose();
				}
			}

			return files;
		}

		private static FolderType DetermineFolderType( String directoryPath, List<LoadedFile> files, List<Message> messages )
		{
			if( files.Count == 0 ) return FolderType.Empty;

			List<TagSummary> filesTags = files.Select( f => TagSummary.Create( f.Id3v2Tag ) ).ToList();

			Boolean allAlbumArtistsAreVariousArtists = filesTags.All( ft => ft.AlbumArtist.EqualsCI( "Various Artists" ) ); //files.All( f => String.Equals( "Various Artists", f.Id3v2Tag.AlbumArtists.SingleOrDefault(), StringComparison.Ordinal ) );

			String  firstAlbumArtist   = filesTags.First().AlbumArtist; //files.First().Id3v2Tag.AlbumArtists.FirstOrDefault();
			Boolean allSameAlbumArtist = filesTags.All( ft => ft.AlbumArtist.EqualsCI( firstAlbumArtist ) ); //files.All( f => String.Equals( firstAlbumArtist, f.Id3v2Tag.AlbumArtists.SingleOrDefault(), StringComparison.Ordinal ) );

			String  firstArtist        = filesTags.First().Artist; //files.First().Id3v2Tag.Performers.FirstOrDefault();
			Boolean allSameArtist      = filesTags.All( ft => ft.Artist.EqualsCI( firstArtist ) ); //files.All( f => String.Equals( firstArtist, f.Id3v2Tag.Performers.SingleOrDefault(), StringComparison.Ordinal ) );

			String  firstAlbum         = filesTags.First().Album; //files.First().Id3v2Tag.Album;
			Boolean sameAlbum          = filesTags.All( ft => ft.Album.EqualsCI( firstAlbum ) ); //files.All( f => String.Equals( firstAlbum, f.Id3v2Tag.Album, StringComparison.Ordinal ) );
			Boolean noAlbum            = filesTags.All( ft => String.IsNullOrWhiteSpace( ft.Album ) ); //files.All( f => String.IsNullOrWhiteSpace( f.Id3v2Tag.Album ) );

			if( allAlbumArtistsAreVariousArtists )
			{
				if( noAlbum ) return FolderType.AssortedFiles;

				if( sameAlbum ) return FolderType.CompilationAlbum;

				List<String> differentAlbums = filesTags
					.Select( ft => "\"" + ft.Album + "\"" )
					.Distinct()
					.OrderBy( s => s )
					.ToList();

				String messageText = "Unexpected folder type: All tracks have AlbumArtist = \"Various Artists\", but they have different Album values: " + String.Join( ", ", differentAlbums );
				messages.Add( new Message( MessageSeverity.Error, directoryPath, directoryPath, messageText ) );
				return FolderType.UnableToDetermine;
			}
			else
			{
				if( allSameArtist && sameAlbum ) return FolderType.ArtistAlbum;

				if( allSameAlbumArtist && sameAlbum ) return FolderType.ArtistAlbumWithGuestArtists;

				if( noAlbum ) return FolderType.ArtistAssorted;

				List<String> differentArtists = filesTags
					.Select( ft => "\"" + ft.Artist + "\"" )
					.Distinct()
					.OrderBy( s => s)
					.ToList();

				List<String> differentAlbumArtists = filesTags
					.Select( ft => "\"" + ft.AlbumArtist + "\"" )
					.Distinct()
					.OrderBy( s => s)
					.ToList();

				List<String> differentAlbums = filesTags
					.Select( ft => "\"" + ft.Album + "\"" )
					.Distinct()
					.OrderBy( s => s )
					.ToList();

				StringBuilder sb = new StringBuilder("Unexpected folder type: ");

				if( differentArtists     .Count > 1 ) sb.Append( "Folder has multiple artists ("       + String.Join( ",", differentArtists      ) + "). " );
				if( differentAlbumArtists.Count > 1 ) sb.Append( "Folder has multiple album-artists (" + String.Join( ",", differentAlbumArtists ) + "). " );
				if( differentAlbums      .Count > 1 ) sb.Append( "Folder has multiple albums ("        + String.Join( ",", differentAlbums       ) + "). " );

				messages.Add( new Message( MessageSeverity.Error, directoryPath, directoryPath, sb.ToString() ) );
				return FolderType.UnableToDetermine;
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

		public static void AddFileWarning( this List<Message> messages, String filePath, String text )
		{
			messages.Add( new Message( MessageSeverity.Warning, Path.GetDirectoryName( filePath ), filePath, text ) );
		}

		public static void AddFileWarning( this List<Message> messages, String filePath, String format, params Object[] args )
		{
			Extensions.AddFileWarning( messages, filePath, text: String.Format( CultureInfo.InvariantCulture, format, args ) );
		}

		public static void AddFileError( this List<Message> messages, String filePath, String text )
		{
			messages.Add( new Message( MessageSeverity.Error, Path.GetDirectoryName( filePath ), filePath, text ) );
		}

		public static void AddFileError( this List<Message> messages, String filePath, String format, params Object[] args )
		{
			Extensions.AddFileError( messages, filePath, text: String.Format( CultureInfo.InvariantCulture, format, args ) );
		}
	}
}
