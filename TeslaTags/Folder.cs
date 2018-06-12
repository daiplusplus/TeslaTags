using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using TagLib;

namespace TeslaTags
{
	public static class Folder
	{
		public static (FolderType folderType, Int32 modifiedCount, Int32 totalCount) Process(String directoryPath, Boolean readOnly, Boolean undo, GenreRules genreRules, List<Message> messages)
		{
			List<LoadedFile> files = LoadFiles( directoryPath, messages );
			try
			{
				FolderType folderType;
				if( undo )
				{
					Boolean anyReverted = Retagger.RetagForUndo( files, messages );
					
					if( anyReverted ) folderType = FolderType.Reverted;
					else              folderType = FolderType.Skipped;
				}
				else
				{
					folderType = DetermineFolderType( directoryPath, files, messages );
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

					Retagger.RetagForGenre( folderType, files, genreRules, messages );
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

			Boolean allAlbumArtistsAreVariousArtists = files.All( f => f.Tag?.FirstAlbumArtist?.EqualsCI( Values.VariousArtistsConst ) ?? false );

			String  firstAlbumArtist   = files.First().Tag.FirstAlbumArtist;
			Boolean allSameAlbumArtist = !String.IsNullOrWhiteSpace( firstAlbumArtist ) && files.All( f => f.Tag?.FirstAlbumArtist?.EqualsCI( firstAlbumArtist ) ?? false );

			String  firstArtist        = files.First().Tag.FirstPerformer;
			Boolean allSameArtist      = !String.IsNullOrWhiteSpace( firstArtist ) && files.All( ft => ft.Tag?.FirstPerformer?.EqualsCI( firstArtist ) ?? false );

			String  firstAlbum         = files.First().Tag.Album;
			Boolean sameAlbum          = !String.IsNullOrWhiteSpace( firstAlbum ) && files.All( ft => ft.Tag?.Album?.EqualsCI( firstAlbum ) ?? false );
			Boolean noAlbum            = files.All( ft => String.IsNullOrWhiteSpace( ft.Tag.Album ) );

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

						var folderDiscTrackInfo = DiscAndTrackNumberHelper.GetDiscTrackNumberForAllFiles( directoryPath );
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

	
}
