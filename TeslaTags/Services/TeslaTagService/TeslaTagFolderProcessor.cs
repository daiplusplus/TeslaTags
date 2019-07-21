using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using TagLib;

namespace TeslaTags
{
	public static class TeslaTagFolderProcessor
	{
		public static (FolderType folderType, Int32 modifiedCountProposed, Int32 modifiedCountActual, Int32 totalCount) Process(String directoryPath, HashSet<String> fileExtensionsToLoad, Boolean readOnly, Boolean undo, GenreRules genreRules, List<Message> messages)
		{
			List<LoadedFile> files = LoadFiles( directoryPath, fileExtensionsToLoad, messages );
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

				Int32 proposedModifiedCount = 0;
				Int32 actualModifiedCount = 0;
				foreach( LoadedFile file in files )
				{
					if( file.IsModified ) proposedModifiedCount++;

					if( !readOnly && file.IsModified )
					{
						try
						{
							file.Save( messages );
						}
						catch(Exception ex)
						{
							messages.Add( new Message( MessageSeverity.Error, directoryPath, file.FileInfo.FullName, "Could not save file: " + ex.Message ) );
						}
						
						actualModifiedCount++;
					}
				}

				return (folderType, proposedModifiedCount, actualModifiedCount, files.Count);
			}
			finally
			{
				foreach( LoadedFile file in files )
				{
					file.Dispose();
				}
			}
		}

		public static List<LoadedFile> LoadFiles( String directoryPath, HashSet<String> fileExtensionsToLoad, List<Message> messages )
		{
			DirectoryInfo di = new DirectoryInfo( directoryPath );

			List<LoadedFile> loadedFiles = new List<LoadedFile>();
			foreach( FileInfo fi in di.GetFiles().Where( fi => fileExtensionsToLoad.Contains( fi.Extension ) ) )
			{
				if( LoadedFile.TryLoadFromFile( fi, messages, out LoadedFile loadedFile ) )
				{
					loadedFiles.Add( loadedFile );
				}
			}

			return loadedFiles;
		}

		/// <summary>Creates a comma-separated lexicographically-ordered list of distinct values in <paramref name="files"/> using <paramref name="selector"/> to get each value. Nonempty values are wrapped in double-quotes. Empty values are displayed as "null".</summary>
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

			Boolean allAlbumArtistsAreVariousArtists = files.All( f => f.Tag.FirstAlbumArtist.EqualsCI( Values.VariousArtistsConst ) );

			String  firstAlbumArtist   = files.First().Tag.GetAlbumArtist();
			Boolean allSameAlbumArtist = !String.IsNullOrWhiteSpace( firstAlbumArtist ) && files.All( f => f.Tag.GetAlbumArtist().EqualsCI( firstAlbumArtist ) );

			String  firstArtist        = files.First().Tag.GetPerformers();
			Boolean allSameArtist      = !String.IsNullOrWhiteSpace( firstArtist ) && files.All( ft => ft.Tag.GetPerformers().EqualsCI( firstArtist ) );

			String  firstAlbum         = files.First().Tag.Album;
			Boolean allSameAlbum       = !String.IsNullOrWhiteSpace( firstAlbum ) && files.All( ft => ft.Tag.Album.EqualsCI( firstAlbum ) );
			Boolean allNoAlbum         = files.All( ft => String.IsNullOrWhiteSpace( ft.Tag.Album ) );

			if( allAlbumArtistsAreVariousArtists )
			{
				if( allNoAlbum ) return FolderType.AssortedFiles;

				if( allSameAlbum ) return FolderType.CompilationAlbum;

				String differentAlbums = GetList( files, ft => ft.Album );
				String messageText = "Unexpected folder type: All tracks have AlbumArtist = \"Various Artists\", but they have different Album values: " + differentAlbums;
				messages.Add( new Message( MessageSeverity.Error, directoryPath, directoryPath, messageText ) );
				return FolderType.UnableToDetermine;
			}
			else
			{
				if( allSameArtist )
				{
					if( allNoAlbum )
					{
						return FolderType.ArtistAssorted;
					}
					else if( allSameAlbum )
					{
						// If none of the files have track-numbers in their filenames and they're all lacking track number tags, then it's something like a video-game soundtrack dump where track-numbers don't apply:
						Boolean anyHaveTrackNumberTags = files.Any( ft => ft.Tag.Track > 0 );

						var folderDiscTrackInfo = DiscAndTrackNumberHelper.GetDiscTrackNumberForAllFiles( directoryPath );

						Boolean anyHaveTrackNumberNames = false;
						if( folderDiscTrackInfo.hasBest )
						{
							anyHaveTrackNumberNames = folderDiscTrackInfo.files.Values.Any( t => t.track != null );
						}

						if( anyHaveTrackNumberTags || anyHaveTrackNumberNames )
						{
							return FolderType.ArtistAlbum;
						}
						else
						{
							return FolderType.ArtistAlbumNoTrackNumbers; // I note that the "No track numbers" thing is orthogonal to a folder's type, e.g. an `ArtistAlbumWithGuestArtists` could have no track numbers too! TODO: Fix this! Consider making `FolderType` a flags enum and `NoTrackNumbers` as a high-bit flag?
						}
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
					if( allNoAlbum )
					{
						messages.Add( new Message( MessageSeverity.Error, directoryPath, directoryPath, "Folder has no albums" ) );
						return FolderType.UnableToDetermine;
					}
					else if( allSameAlbum )
					{
						return FolderType.ArtistAlbumWithGuestArtists;
					}
					else
					{
						String differentArtists = GetList( files, ft => ft.GetPerformers() );
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
