using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using TagLib;

using ape = TagLib.Ape;

namespace TeslaTags
{
	public partial class RealTeslaTagService
	{
		private static List<Message> Wrap( String directoryPath, HashSet<String> fileExtensionsToLoad, Action<String,List<LoadedFile>,List<Message>> action )
		{
			List<Message> messages = new List<Message>();
			List<LoadedFile> files = TeslaTagFolderProcessor.LoadFiles( directoryPath, fileExtensionsToLoad, messages );
			try
			{
				action( directoryPath, files, messages );
				return messages;
			}
			finally
			{
				foreach( LoadedFile file in files )
				{
					if( file.IsModified ) file.Save( messages );
					file.Dispose();
				}
			}
		}

		public Task<List<Message>> RemoveApeTagsAsync( String directoryPath, HashSet<String> fileExtensionsToLoad )
		{
			return Task.Run( () => Wrap( directoryPath, fileExtensionsToLoad, RemoveApeTagsInner ) );
		}

		private static void RemoveApeTagsInner( String directoryPath, List<LoadedFile> files, List<Message> messages )
		{
			foreach( MpegLoadedFile mpegFile in files.OfType<MpegLoadedFile>() )
			{
				ape.Tag apeTag = (ape.Tag)mpegFile.MpegAudioFile.GetTag(TagTypes.Ape);
				if( apeTag != null )
				{
					mpegFile.MpegAudioFile.RemoveTags( TagTypes.Ape );
					mpegFile.IsModified = true;
					messages.AddFileChange( mpegFile.FileInfo.FullName, "APE tag removed." );
				}
			}
		}

		public Task<List<Message>> SetAlbumArtAsync( String directoryPath, HashSet<String> fileExtensionsToLoad, String imageFileName, AlbumArtSetMode mode )
		{
			if( String.IsNullOrWhiteSpace( directoryPath ) || !Directory.Exists( directoryPath ) ) throw new ArgumentException( "Value must be a valid path to a directory that exists.", nameof(directoryPath) );
			if( String.IsNullOrWhiteSpace( imageFileName ) ) throw new ArgumentNullException( nameof(imageFileName) );
			imageFileName = Path.IsPathRooted( imageFileName ) ? imageFileName : Path.Combine( directoryPath, imageFileName );
			if( !System.IO.File.Exists( imageFileName ) ) throw new ArgumentException( "Value must be a file that exists.", nameof(imageFileName ) );

			return Task.Run( () => Wrap( directoryPath, fileExtensionsToLoad, (dp, files, messages) => SetAlbumArtInner( files, messages, imageFileName, mode ) ) );
		}

		private static void SetAlbumArtInner(List<LoadedFile> files, List<Message> messages, String imageFileName, AlbumArtSetMode mode)
		{
			// imageFileName will be resolved by now.

			IPicture newPicture = new Picture( imageFileName );

			foreach( LoadedFile file in files )
			{
				IPicture[] pictures = file.Tag.Pictures;
				if( pictures == null || pictures.Length == 0 )
				{
					pictures = new IPicture[1];
					pictures[0] = newPicture;

					file.Tag.Pictures = pictures;
					file.IsModified = true;

					messages.AddFileChange( file.FileInfo.FullName, "Had zero pictures. Added new picture." );
				}
				else
				{
					Int32 oldPictureCount = pictures.Length;
					List<IPicture> existingIdenticalPictures = pictures
						.Where( p => p.Data.CompareTo( newPicture.Data ) == 0 )
						.ToList();

					if( existingIdenticalPictures.Count > 1 )
					{
						messages.AddFileWarning( file.FileInfo.FullName, "Duplicate pictures in file." );
					}
					else if( existingIdenticalPictures.Count == 1 )
					{
						messages.AddInfoFile( file.FileInfo.FullName, "New picture already exists in file." );
					}

					switch( mode )
					{
					case AlbumArtSetMode.AddIfMissing:
						
						if( existingIdenticalPictures.Count == 0 )
						{
							Array.Resize( ref pictures, pictures.Length + 1 );
							pictures[ pictures.GetUpperBound(0) ] = newPicture;
							
							file.Tag.Pictures = pictures;
							file.IsModified = true;

							messages.AddFileChange( file.FileInfo.FullName, "Added new picture to list of existing pictures. File now has " + pictures.Length + " pictures." );
						}

						break;

					case AlbumArtSetMode.Replace:

						pictures = new IPicture[1];
						pictures[0] = newPicture;
						
						file.Tag.Pictures = pictures;
						file.IsModified = true;

						messages.AddFileChange( file.FileInfo.FullName, "Replaced " + oldPictureCount + " pictures with single new picture." );

						break;
					}
				}
			}
		}

		public Task<List<Message>> SetTrackNumbersFromFileNamesAsync( String directoryPath, HashSet<String> fileExtensionsToLoad, Int32 offset, Int32? discNumber )
		{
			if( String.IsNullOrWhiteSpace( directoryPath ) || !Directory.Exists( directoryPath ) ) throw new ArgumentException( "Value must be a valid path to a directory that exists.", nameof(directoryPath) );

			return Task.Run( () => Wrap( directoryPath, fileExtensionsToLoad, (dp, files, messages) => SetTrackNumbersFromFileNamesInner( dp, files, messages, offset, discNumber ) ) );
		}

		private static void SetTrackNumbersFromFileNamesInner(String directoryPath, List<LoadedFile> files, List<Message> messages, Int32 offset, Int32? discNumber)
		{
			var result = DiscAndTrackNumberHelper.GetDiscTrackNumberForAllFiles( directoryPath );
			if( !result.hasBest )
			{
				messages.Add( new Message(MessageSeverity.Error, directoryPath, directoryPath, "Could not find a file-name pattern for at least half the files in the directory. Aborting." ) );
				return;
			}
			
			foreach( LoadedFile file in files )
			{
				var (disc, track, err) = result.files[ file.FileInfo.Name ];
				if( err != null )
				{
					messages.AddFileError( file.FileInfo.FullName, err );
					continue;
				}

				if( discNumber != null ) disc = discNumber.Value;

				if( disc  != null )
				{
					String oldDisc  = file.Tag.Disc.ToString(CultureInfo.InvariantCulture);
					UInt32 newDisc  = (UInt32)disc.Value;

					file.Tag.Disc   = newDisc;
					file.IsModified = true;

					messages.AddFileChange( file.FileInfo.FullName, nameof(TagLib.Tag.Disc), oldDisc, newDisc.ToString(CultureInfo.InvariantCulture) );
				}

				if( track != null )
				{
					Int32 newTrack = Math.Max( 0, track.Value + offset );
					
					String oldTrack  = file.Tag.Track.ToString(CultureInfo.InvariantCulture);

					file.Tag.Track = (UInt32)newTrack;
					file.IsModified = true;

					messages.AddFileChange( file.FileInfo.FullName, nameof(TagLib.Tag.Track), oldTrack, newTrack.ToString(CultureInfo.InvariantCulture) );
				}
			}
		}
	}
}
