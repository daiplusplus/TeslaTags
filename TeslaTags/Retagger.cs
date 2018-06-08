using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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

		public static void RetagForArtistAlbum(List<LoadedFile> files, List<String> errors)
		{
			// NOOP. Handled correctly.
			// But ensure album-art is present:
			foreach( LoadedFile file in files )
			{
				if( String.IsNullOrWhiteSpace( file.Id3v2Tag.Album ) || String.IsNullOrWhiteSpace( file.Id3v2Tag.FirstPerformer ) ) errors.Add( file.FileInfo.FullName + "\t" + "Album or Artist ID3V2 tag not set." );

				Int32 pictureCount = file.Id3v2Tag.Pictures?.Length ?? 0;
				if( pictureCount == 0 ) errors.Add( file.FileInfo.FullName + "\t" + "Contains no ID3 images (i.e. album art)." );
			}
		}

		public static Int32 RetagForArtistAlbumWithGuestArtists(List<LoadedFile> files, List<String> errors)
		{
			// 1. Copy Artist to Title.
			// 2. Use AlbumArtist as Artist (note that all tracks will have the same AlbumArtist value, so copy it from the first track).
			
			String albumArtist = files.First().Id3v2Tag.AlbumArtists.Single();

			Int32 modified = 0;

			foreach( LoadedFile file in files )
			{
				if( file.Id3v2Tag.Performers.Length > 1 ) errors.Add( file.FileInfo.FullName + "\t" + "Contains multiple Performers: \"" + file.Id3v2Tag.JoinedPerformers + "\"." );

				String artist      = file.Id3v2Tag.Performers.First();
				String title       = file.Id3v2Tag.Title;

				// 1:
				file.Id3v2Tag.Title = artist + " - " + title;
				// 2:
				file.Id3v2Tag.Performers = new String[] { albumArtist };

				file.IsModified = true;
				modified++;

				Int32 pictureCount = file.Id3v2Tag.Pictures?.Length ?? 0;
				if( pictureCount == 0 ) errors.Add( file.FileInfo.FullName + "\t" + "Contains no ID3 images (i.e. album art)." );
			}

			return modified;
		}

		public static Int32 RetagForCompilationAlbum(List<LoadedFile> files, List<String> errors)
		{
			// 1. Copy Artist as Title prefix.
			// 2. Set Artist to "Various Artists"

			Int32 modified = 0;
			foreach( LoadedFile file in files )
			{
				String artist      = file.Id3v2Tag.Performers.First();
				String title       = file.Id3v2Tag.Title;

				if( artist != "Various Artists" )
				{
					// 1:
					file.Id3v2Tag.Title = artist + " - " + title;
					// 2:
					file.Id3v2Tag.Performers = new String[] { "Various Artists" };

					file.IsModified = true;
					modified++;

					Int32 pictureCount = file.Id3v2Tag.Pictures?.Length ?? 0;
					if( pictureCount == 0 ) errors.Add( file.FileInfo.FullName + "\t" + "Contains no ID3 images (i.e. album art)." );
				}
			}
			return modified;
		}

		public static Int32 RetagForAssortedFiles(List<LoadedFile> files)
		{
			// Artist and Title tags are correct as-is.
			// Clear the Album and TrackNumber tags.

			// TODO: Add "Genre" as the name of the folder or playlist?
			// Will they show-up at all as they lack albums?

			Int32 modified = 0;
			foreach( LoadedFile file in files )
			{
				if( !String.IsNullOrWhiteSpace( file.Id3v2Tag.Album ) )
				{
					// 1:
					file.Id3v2Tag.Album = null;
					//file.Id3v2Tag.Track = 0; // it's kinda messy to clear tags using TagLib, it's a poorly-designed API (I noticed!): https://stackoverflow.com/questions/21343938/delete-all-pictures-of-an-id3-tag-with-taglib-sharp

					file.IsModified = true;

					modified++;
				}
			}
			return modified;
		}

		public static Int32 RetagForArtistAssortedFiles(List<LoadedFile> files, List<String> errors)
		{
			// We want it displayed in the main Artists list, which means it needs an album set. Use "No album" for those:

			Int32 modified = 0;
			foreach( LoadedFile file in files )
			{
				if( String.IsNullOrWhiteSpace( file.Id3v2Tag.FirstPerformer ) ) errors.Add( file.FileInfo.FullName + "\t" + "Album ID3V2 tag not set." );

				if( String.IsNullOrWhiteSpace( file.Id3v2Tag.Album ) )
				{
					// 1:
					file.Id3v2Tag.Album = "No Album";
					//file.Id3v2Tag.Track = 0; // it's kinda messy to clear tags using TagLib, it's a poorly-designed API (I noticed!): https://stackoverflow.com/questions/21343938/delete-all-pictures-of-an-id3-tag-with-taglib-sharp

					file.IsModified = true;

					modified++;
				}
			}
			return modified;
		}
	}

	public enum FolderType
	{
		// TODO: How to consider Disc folders?
		Container,
		ArtistAlbum,
		ArtistAlbumWithGuestArtists,
		ArtistAssorted,
		CompilationAlbum,
		AssortedFiles
	}
}
