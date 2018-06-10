using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using TagLib;

namespace TeslaTags
{
	public static class Folder
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
					case FolderType.ArtistAlbumNoTrackNumbers:
						Retagger.RetagForArtistAlbum( files, messages, trackNumbersExpected: ( folderType != FolderType.ArtistAlbumNoTrackNumbers ) );
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
							file.Save();
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
					file.Dispose();
				}
			}
		}

		public static List<LoadedFile> LoadFiles( String directoryPath, List<Message> messages )
		{
			DirectoryInfo di = new DirectoryInfo( directoryPath );

			List<FileInfo> audioFiles = new List<FileInfo>();
			audioFiles.AddRange( di.GetFiles("*.mp3") );
			audioFiles.AddRange( di.GetFiles("*.flac") );

			List<LoadedFile> loadedFiles = new List<LoadedFile>();
			foreach( FileInfo fi in audioFiles )
			{
				LoadedFile loadedFile;
				if( fi.Extension.ToUpperInvariant() == ".MP3" ) loadedFile = MpegLoadedFile.Create( fi, messages );
				else if( fi.Extension.ToUpperInvariant() == ".FLAC" ) loadedFile = FlacLoadedFile.Create( fi, messages );
				else continue;

				loadedFiles.Add( loadedFile );
			}

			return loadedFiles;
		}

		private static String GetList( List<LoadedFile> files, Func<Tag,String> selector )
		{
			List<String> values = files
				.Select( ft => selector( ft.Tag ) ) 
				.Distinct()
				.OrderBy( str => str )
				.Select( str => String.IsNullOrWhiteSpace( str ) ? "null" : ( '"' + str + '"' ) )
				.ToList();

			return String.Join( ",", values );
		}

		private static FolderType DetermineFolderType( String directoryPath, List<LoadedFile> files, List<Message> messages )
		{
			if( files.Count == 0 ) return FolderType.Empty;

			Boolean allAlbumArtistsAreVariousArtists = files.All( f => f.Tag.FirstAlbumArtist.EqualsCI( Values.VariousArtistsConst ) ); //files.All( f => String.Equals( "Various Artists", f.Id3v2Tag.AlbumArtists.SingleOrDefault(), StringComparison.Ordinal ) );

			String  firstAlbumArtist   = files.First().Tag.FirstAlbumArtist; //files.First().Id3v2Tag.AlbumArtists.FirstOrDefault();
			Boolean allSameAlbumArtist = files.All( f => f.Tag.FirstAlbumArtist.EqualsCI( firstAlbumArtist ) ); //files.All( f => String.Equals( firstAlbumArtist, f.Id3v2Tag.AlbumArtists.SingleOrDefault(), StringComparison.Ordinal ) );

			String  firstArtist        = files.First().Tag.FirstPerformer; //files.First().Id3v2Tag.Performers.FirstOrDefault();
			Boolean allSameArtist      = files.All( ft => ft.Tag.FirstPerformer.EqualsCI( firstArtist ) ); //files.All( f => String.Equals( firstArtist, f.Id3v2Tag.Performers.SingleOrDefault(), StringComparison.Ordinal ) );

			String  firstAlbum         = files.First().Tag.Album; //files.First().Id3v2Tag.Album;
			Boolean sameAlbum          = files.All( ft => ft.Tag.Album.EqualsCI( firstAlbum ) ); //files.All( f => String.Equals( firstAlbum, f.Id3v2Tag.Album, StringComparison.Ordinal ) );
			Boolean noAlbum            = files.All( ft => String.IsNullOrWhiteSpace( ft.Tag.Album ) ); //files.All( f => String.IsNullOrWhiteSpace( f.Id3v2Tag.Album ) );

			if( allAlbumArtistsAreVariousArtists )
			{
				if( noAlbum ) return FolderType.AssortedFiles;

				if( sameAlbum ) return FolderType.CompilationAlbum;

				String differentAlbums = GetList( files, ft => ft.Album );
				String messageText = "Unexpected folder type: All tracks have AlbumArtist = \"Various Artists\", but they have different Album values: " + differentAlbums;
				messages.Add( new Message( MessageSeverity.Error, directoryPath, directoryPath, messageText ) );
				return FolderType.UnableToDetermine;
			}
			else
			{
				if( allSameArtist )
				{
					if     ( noAlbum   ) return FolderType.ArtistAssorted;
					else if( sameAlbum )
					{
						// If none of the files have track-numbers in their filenames and they're all lacking track number tags, then it's something like a video-game soundtrack dump where track-numbers don't apply:
						Boolean noneHaveTrackNumberTags  = files.All( ft => ft.Tag.Track == 0 );

						var folderDiscTrackInfo = Values.GetDiscTrackNumberForAllFiles( directoryPath );
						Boolean noneHaveTrackNumberNames = folderDiscTrackInfo.hasBest && folderDiscTrackInfo.files.Values.Any( t => t.track != null );

						if( noneHaveTrackNumberTags && noneHaveTrackNumberNames ) return FolderType.ArtistAlbumNoTrackNumbers;

						return FolderType.ArtistAlbum;
					}
					else
					{
						String differentAlbums = GetList( files, ft => ft.Album );
						messages.Add( new Message( MessageSeverity.Error, directoryPath, directoryPath, "Folder has same artist, but has multiple albums (" + differentAlbums + "). " ) );
						return FolderType.UnableToDetermine;
					}
				}
				else if( allSameAlbumArtist )
				{
					if( noAlbum )
					{
						messages.Add( new Message( MessageSeverity.Error, directoryPath, directoryPath, "Folder has no albums" ) );
						return FolderType.UnableToDetermine;
					}
					else if( sameAlbum )
					{
						return FolderType.ArtistAlbumWithGuestArtists;
					}
					else
					{
						String differentArtists = GetList( files, ft => ft.FirstPerformer );
						String differentAlbums  = GetList( files, ft => ft.Album );

						messages.Add( new Message( MessageSeverity.Error, directoryPath, directoryPath, "Folder has same album-artist, but multiple artists (" + differentArtists + ") or multiple albums (" + differentAlbums + "). " ) );
						return FolderType.UnableToDetermine;
					}
				}
				else
				{
					// Different Artists and/or Album Artists and/or Albums, i.e. a mess. Inform the user to tidy it up.

					
					String differentArtists      = GetList( files, ft => ft.FirstPerformer );
					String differentAlbums       = GetList( files, ft => ft.Album );
					String differentAlbumArtists = GetList( files, ft => ft.FirstAlbumArtist );

					String messageText = "Folder has multiple artists (" + differentArtists + "), albums (" + differentAlbums + ") or album-artists (" + differentAlbumArtists + ").";
					messages.Add( new Message( MessageSeverity.Error, directoryPath, directoryPath, messageText ) );
					return FolderType.UnableToDetermine;
				}
			}
		}
	}

	public static class Extensions
	{
		public static Boolean EqualsCI( this String x, String y )
		{
			return String.Equals( x, y, StringComparison.OrdinalIgnoreCase );
		}

		public static void AddInfo( this List<Message> messages, String filePath, String text )
		{
			messages.Add( new Message( MessageSeverity.Info, Path.GetDirectoryName( filePath ), filePath, text ) );
		}

		/// <summary>Returns false if there were no reasons.</summary>
		public static Boolean AddFileCorruptionErrors( this List<Message> messages, String filePath, IEnumerable<String> corruptionReasons )
		{
			if( corruptionReasons == null ) return false;
			
			Boolean any = false;

			foreach( String reason in corruptionReasons )
			{
				messages.AddFileError( filePath, "File corrupted: " + reason );
				any = true;
			}

			return any;
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

		public static void AddFileChange( this List<Message> messages, String filePath, String messageText )
		{
			messages.Add( new Message( MessageSeverity.FileModification, Path.GetDirectoryName( filePath ), filePath, messageText ) );
		}

		public static void AddFileChange( this List<Message> messages, String filePath, String field, String oldValue, String newValue )
		{
			oldValue = ( oldValue == null ) ? "null" : ("\"" + oldValue + "\"");
			newValue = ( newValue == null ) ? "null" : ("\"" + newValue + "\"");

			String messageText = String.Concat( field, ": ", oldValue, " -> ", newValue );

			messages.Add( new Message( MessageSeverity.FileModification, Path.GetDirectoryName( filePath ), filePath, messageText ) );
		}
	}
}
