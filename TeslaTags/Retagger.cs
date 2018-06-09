using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using TagLib;
using TagLib.Id3v1;
using TagLib.Id3v2;
using TagLib.Mpeg;
//using TagLib.Ape;
//using TagLib.Mpeg;

namespace TeslaTags
{
	internal static class Retagger
	{
		private const Char MongolianVowelSeparator = '\u180E';
		private const Char ZeroWidthSpace          = '\u200B';
		private const Char ZeroWidthNoBreakSpace   = '\uFEFF';

		private static readonly Char[] _base3Digits = new[] { MongolianVowelSeparator, ZeroWidthSpace, ZeroWidthNoBreakSpace };

		private static void PrependInvisibleCharactersUsingBase3(TagLib.Mpeg.AudioFile audioFile, Int32 sortOrder, Int32 maxSortOrder)
		{
			TagLib.Tag id3v1 = audioFile.GetTag( TagTypes.Id3v1 ); // TODO: What happens if ID3v1 is removed?
			TagLib.Tag id3v2 = audioFile.GetTag( TagTypes.Id3v2 );

			// Naive approach: Prepend with MongolianVowelSeparator repeated `sortOrder` times (i.e. base-1)
			// Smarter approach: Treat the 3 zero-width unicode characters as a base-3 radix number system, and encode the sortOrder as that).

			Int32 minStringLength = LengthOfBase3String( maxSortOrder );
			String prefix = ToBase3( sortOrder, minStringLength );

			String trimmedTitle = Trim( id3v2.Title );
			String prefixedTitle = prefix + trimmedTitle;

			id3v2.Title = prefixedTitle;
		}

		private static void PrependInvisibleCharactersUsingBase1(TagLib.Mpeg.AudioFile audioFile, Int32 sortOrder, Int32 maxSortOrder)
		{
			TagLib.Tag id3v1 = audioFile.GetTag( TagTypes.Id3v1 );
			TagLib.Tag id3v2 = audioFile.GetTag( TagTypes.Id3v2 );

			// Naive approach: Prepend with MongolianVowelSeparator repeated `sortOrder` times (i.e. base-1)

			String prefix = String.Empty.PadLeft( sortOrder, MongolianVowelSeparator );

			String trimmedTitle = Trim( id3v2.Title );
			String prefixedTitle = prefix + trimmedTitle;

			id3v2.Title = prefixedTitle;
		}

		private static void PrependTrackNumber(TagLib.Mpeg.AudioFile audioFile, Int32 sortOrder, Int32 maxSortOrder)
		{
			Int32 maxLength = LengthOfBase10String( (UInt32)maxSortOrder );
			String prefix = sortOrder.ToString( CultureInfo.InvariantCulture ).PadLeft( maxLength, '0' );

			TagLib.Tag id3v1 = audioFile.GetTag( TagTypes.Id3v1 );
			TagLib.Tag id3v2 = audioFile.GetTag( TagTypes.Id3v2 );
			
			String trimmedTitle = Trim( id3v2.Title );
			String prefixedTitle = prefix + " - " + trimmedTitle;
			id3v2.Title = prefixedTitle;
		}

		private static Int32 LengthOfBase3String(Int32 value)
		{
			Int32 count = 0;
			Int32 workingValue = value;
			do
			{
				workingValue = workingValue / 3;
				count++;
			}
			while( workingValue > 0 );

			return count;
		}

		private static Int32 LengthOfBase10String(UInt32 value)
		{
			if( value < 10 ) return 1;
			if( value < 100 ) return 2;
			if( value < 1000 ) return 3;
			if( value < 10000 ) return 4;
			throw new ArgumentOutOfRangeException( nameof(value), value, "Value must be in the range 0-9999" );
		}

		private static String ToBase3(Int32 value, Int32 minStringLength)
		{
			Int32 workingValue = value;

			Char[] output = new Char[ Math.Max( minStringLength, 10 ) ];
			for( Int32 i = 0; i < output.Length; i++ ) output[i] = _base3Digits[0];

			Int32 o = output.GetUpperBound(0);

			do
			{
				Int32 digit = workingValue % 3;
				workingValue = workingValue / 3;

				output[o--] = _base3Digits[digit];
			}
			while( o >= 0 && workingValue > 0 );

			//Int32 startIndex = o + 1;
			//Int32 length     = output.Length - startIndex;

			Int32 startIndex = output.Length - minStringLength;
			Int32 length     = minStringLength;

			String base3String = new String( output, startIndex, length );
			return base3String;
		}

		private static String Trim(String value)
		{
			// Fun-fact: String.Trim() uses Char.IsWhiteSpace() to determine what to remove.
			// However Char.IsWhiteSpace() returns false for the 3 characters we're using because they're considered "Format" characters instead of spacing.
			// So this reimplementation checks both.

			// optimization: return if it doesn't need trimming:

			Int32 trimFromStart = 0;
			for( Int32 i = 0; i < value.Length; i++ )
			{
				if( IsWhiteSpace( value[i] ) ) trimFromStart++;
				else break;
			}
			
			if( trimFromStart == value.Length ) return String.Empty;

			Int32 trimFromEnd = 0;
			for( Int32 i = value.Length - 1; i >= 0; i-- )
			{
				if( IsWhiteSpace( value[i] ) ) trimFromEnd++;
				else break;
			}

			if( trimFromStart == 0 && trimFromEnd == 0 ) return value;

			Int32 substringLength = ( value.Length - trimFromStart ) - trimFromEnd;

			String substring = value.Substring( trimFromStart, substringLength );
			return substring;
		}

		private static Boolean IsWhiteSpace(Char value)
		{
			switch( value )
			{
			case MongolianVowelSeparator:
			case ZeroWidthSpace:
			case ZeroWidthNoBreakSpace:
				return true;
			default:
				return Char.IsWhiteSpace( value );
			}
		}

		private static readonly Regex _startsWithDigits = new Regex( @"^\d+", RegexOptions.Compiled | RegexOptions.IgnoreCase );

		private static Boolean ValidateFile( LoadedFile file, Boolean albumArtistRequired, Boolean albumRequired, Boolean trackNumberRequired, Boolean warnIfTrackNumberPresent, Boolean warnMissingAlbumArt, List<Message> messages )
		{
			const Boolean titleRequired = true;
			const Boolean artistRequired = true;

			Boolean isValid = true;

			if( titleRequired )
			{
				if( String.IsNullOrWhiteSpace( file.Id3v2Tag.Title ) )
				{
					isValid = false;
					messages.AddFileError( file.FileInfo.FullName, "Title ID3V2 tag not set." );
				}
			}

			if( artistRequired )
			{
				if( String.IsNullOrWhiteSpace( file.Id3v2Tag.FirstPerformer ) )
				{
					isValid = false;
					messages.AddFileError( file.FileInfo.FullName, "Artist ID3V2 tag not set." );
				}
			}

			// Check for APE tags:
			{
				TagLib.Ape.Tag apeTag = (TagLib.Ape.Tag)file.AudioFile.GetTag(TagTypes.Ape);
				if( apeTag != null )
				{
					messages.AddFileWarning( file.FileInfo.FullName, "File has APE tags. Tesla's MCU may be unable to play this file." );
				}
			}

			if( file.Id3v2Tag.Performers?.Length > 1 ) messages.AddFileWarning( file.FileInfo.FullName, "Has multiple Artists: \"{0}\". The first value was used.", file.Id3v2Tag.JoinedPerformers );

			//

			if( albumArtistRequired )
			{
				if( String.IsNullOrWhiteSpace( file.Id3v2Tag.FirstAlbumArtist ) )
				{
					isValid = false;
					messages.AddFileError( file.FileInfo.FullName, "Album-Artist ID3V2 tag not set." );
				}
			}

			if( file.Id3v2Tag.AlbumArtists?.Length > 1 ) messages.AddFileWarning( file.FileInfo.FullName,  "Has multiple AlbumArtists: \"{0}\". The first value was used.", file.Id3v2Tag.JoinedAlbumArtists );

			//

			if( albumRequired )
			{
				if( String.IsNullOrWhiteSpace( file.Id3v2Tag.Album ) )
				{
					isValid = false;
					messages.AddFileError( file.FileInfo.FullName, "Album ID3V2 tag not set." );
				}
			}

			//

			if( trackNumberRequired )
			{
				if( file.Id3v2Tag.Track == 0 || file.Id3v2Tag.Track > 250 )
				{
					// Only fail a track if the filename starts with a digit and the track field is missing:
					if( _startsWithDigits.IsMatch( file.FileInfo.Name ) )
					{
						isValid = false;
						messages.AddFileError( file.FileInfo.FullName, "Filename starts with a number, but the TrackNumber ID3V2 tag is not set or is invalid." );
					}
					else
					{
						// don't fail a track if the only thing missing is the track number. Just make it a warning.
						messages.AddFileWarning( file.FileInfo.FullName, "TrackNumber ID3V2 tag not set or invalid." );
					}
				}
			}

			if( warnIfTrackNumberPresent )
			{
				if( file.Id3v2Tag.Track != 0 ) messages.AddFileWarning( file.FileInfo.FullName, "Assorted file has a track number. This program cannot currently remove track numbers. Please use another program to remove/clear this tag field." );
				
				if( file.Id3v2Tag.Disc  != 0 ) messages.AddFileWarning( file.FileInfo.FullName, "Assorted file has a disc number. This program cannot currently remove disc numbers. Please use another program to remove/clear this tag field." );
			}

			//

			if( warnMissingAlbumArt )
			{
				IPicture[] art = file.Id3v2Tag.Pictures;
				if( art == null || art.Length == 0 )
				{
					messages.AddFileWarning( file.FileInfo.FullName, "No Album Art." ); // TODO: Validate the name/description of the IPicture? I'm not sure how Id3v2 stores it (using a 0-255 byte enum? but there are strings too, so does it matter?)
				}
				else if( art.Length > 1 )
				{
					messages.AddFileWarning( file.FileInfo.FullName, "Multiple embedded pictures." );
				}
			}

			return isValid;
		}

		public static void RetagForArtistAlbum(List<LoadedFile> files, List<Message> messages)
		{
			// NOOP. Handled correctly.
			// But do file validation.

			HashSet<UInt32> uniqueDiscsAndTracks = new HashSet<UInt32>();

			foreach( LoadedFile file in files )
			{
				UInt32 discAndTrack = ( file.Id3v2Tag.Disc * 100 ) + file.Id3v2Tag.Track;
				if( discAndTrack != 0 )
				{
					Boolean isNewDiscAndTrack = uniqueDiscsAndTracks.Add( discAndTrack );
					if( !isNewDiscAndTrack ) messages.AddFileWarning( file.FileInfo.FullName, "Duplicate Disc {0} and Track {1} tuple.", file.Id3v2Tag.Disc, file.Id3v2Tag.Track );
				}

				ValidateFile( file, albumArtistRequired: true, albumRequired: true, trackNumberRequired: true, warnIfTrackNumberPresent: false, warnMissingAlbumArt: true, messages );
			}
		}

		public static void RetagForArtistAlbumWithGuestArtists(List<LoadedFile> files, List<Message> messages)
		{
			// 1. Copy Artist to Title.
			// 2. Use AlbumArtist as Artist (note that all tracks will have the same AlbumArtist value, so copy it from the first track).
			
			String albumArtist = files.First().Id3v2Tag.AlbumArtists.Single();

			foreach( LoadedFile file in files )
			{
				Boolean isValid = ValidateFile( file, albumArtistRequired: true, albumRequired: true, trackNumberRequired: true, warnIfTrackNumberPresent: false, warnMissingAlbumArt: true, messages );
				if( isValid )
				{
					String oldArtist      = file.Id3v2Tag.Performers.First();
					String oldTitle       = file.Id3v2Tag.Title;

					if( oldArtist != albumArtist )
					{
						String newArtist      = albumArtist;
						String newTitle       = oldArtist + " - " + oldTitle;

						messages.AddFileChange( file.FileInfo.FullName, "Artist", oldArtist, newArtist );
						messages.AddFileChange( file.FileInfo.FullName, "Title" , oldTitle , newTitle  );

						// 1:
						file.Id3v2Tag.Title = newTitle;
						// 2:
						file.Id3v2Tag.Performers = new String[] { albumArtist };

						file.IsModified = true;
					}
				}
			}
		}

		public const String Values_VariousArtists = "Various Artists";
		public const String Values_NoAlbum        = "No Album";

		public static void RetagForCompilationAlbum(List<LoadedFile> files, List<Message> messages)
		{
			// 1. Copy Artist as Title prefix.
			// 2. Set Artist to "Various Artists"

			foreach( LoadedFile file in files )
			{
				Boolean isValid = ValidateFile( file, albumArtistRequired: false, albumRequired: true, trackNumberRequired: false /*true*/, warnIfTrackNumberPresent: false, warnMissingAlbumArt: true, messages );
				if( isValid )
				{
					String oldArtist = file.Id3v2Tag.Performers.First();
					String oldTitle  = file.Id3v2Tag.Title;

					if( oldArtist != Values_VariousArtists )
					{
						String newArtist = Values_VariousArtists;
						String newTitle  = oldArtist + " - " + oldTitle;

						messages.AddFileChange( file.FileInfo.FullName, "Artist", oldArtist, newArtist );
						messages.AddFileChange( file.FileInfo.FullName, "Title" , oldTitle , newTitle  );

						// 1:
						file.Id3v2Tag.Title = newTitle;
						// 2:
						file.Id3v2Tag.Performers = new String[] { newArtist };

						file.IsModified = true;
					}
				}
			}
		}

		public static void RetagForAssortedFiles(List<LoadedFile> files, List<Message> messages)
		{
			// Artist and Title tags are correct as-is.
			// Clear the Album and TrackNumber tags.

			// TODO: Add "Genre" as the name of the folder or playlist?
			// Will they show-up at all as they lack albums?

			foreach( LoadedFile file in files )
			{
				Boolean isValid = ValidateFile( file, albumArtistRequired: false, albumRequired: false, trackNumberRequired: false, warnIfTrackNumberPresent: true, warnMissingAlbumArt: false, messages );
				if( isValid )
				{
					String oldAlbum = file.Id3v2Tag.Album;

					if( !String.IsNullOrWhiteSpace( oldAlbum ) )
					{
						messages.AddFileChange( file.FileInfo.FullName, "Album", oldAlbum, null );

						// 1:
						file.Id3v2Tag.Album = null;
						//file.Id3v2Tag.Track = 0; // it's kinda messy to clear tags using TagLib, it's a poorly-designed API (I noticed!): https://stackoverflow.com/questions/21343938/delete-all-pictures-of-an-id3-tag-with-taglib-sharp

						file.IsModified = true;

						//file.AudioFile.Tag. // https://stackoverflow.com/questions/21343938/delete-all-pictures-of-an-id3-tag-with-taglib-sharp
					}
				}
			}
		}

		public static void RetagForArtistAssortedFiles(List<LoadedFile> files, List<Message> messages)
		{
			// We want it displayed in the main Artists list, which means it needs an album set. Use "No album" for those:

			foreach( LoadedFile file in files )
			{
				Boolean isValid = ValidateFile( file, albumArtistRequired: false, albumRequired: false, trackNumberRequired: false, warnIfTrackNumberPresent: true, warnMissingAlbumArt: false, messages );
				if( isValid )
				{
					String oldAlbum = file.Id3v2Tag.Album;
					String newAlbum = Values_NoAlbum;

					if( oldAlbum != newAlbum )
					{
						messages.AddFileChange( file.FileInfo.FullName, "Album", oldAlbum, newAlbum );

						// 1:
						file.Id3v2Tag.Album = newAlbum;
						//file.Id3v2Tag.Track = 0; // uugghhh, but the user gets warned anyway.

						file.IsModified = true;
					}
				}
			}
		}
	}

	public enum FolderType
	{
		// TODO: How to consider Disc folders?
		Empty,
		ArtistAlbum,
		ArtistAlbumWithGuestArtists,
		ArtistAssorted,
		CompilationAlbum,
		AssortedFiles,
		Skipped,
		UnableToDetermine
	}
}
