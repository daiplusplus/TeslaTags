using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TeslaTags
{
	public static class Values
	{
		internal const String VariousArtistsConst = "Various Artists";
		internal const String NoAlbumConst        = "No Album";

		public static String VariousArtists => VariousArtistsConst;
		public static String NoAlbum        => NoAlbumConst;

	}

	public static class DiscAndTrackNumberHelper
	{
		public static Regex FileNameDiscNumberRegex { get; } = new Regex( @"\bdisc\D{0,3}(\d{1,3})", RegexOptions.Compiled | RegexOptions.IgnoreCase );

		public static (Boolean hasBest,Dictionary<String,(Int32? disc, Int32? track, String err)> files) GetDiscTrackNumberForAllFiles(String directoryPath)
		{
			DirectoryInfo directoryInfo = new DirectoryInfo( directoryPath );

			Match parentDirectoryDiscNumberMatch = FileNameDiscNumberRegex.Match( directoryInfo.Name );

			Int32? discNumberFromParentDirectory = null;
			if( parentDirectoryDiscNumberMatch.Success )
			{
				discNumberFromParentDirectory = Int32.Parse( parentDirectoryDiscNumberMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture );
			}
			
			var files = directoryInfo.GetFiles();

			Regex bestRegex = GetBestDiscTrackNumberRegexForDirectory( directoryPath );
			if( bestRegex == null )
			{
				var dict = files
					.ToDictionary(
						fi => fi.Name,
						fi => GetDiscTrackNumberFromFileNameOnly( fi.FullName )
					);

				return ( false, dict );
			}
			else
			{
				var dict = files
					.ToDictionary(
						fi => fi.Name,
						fi =>
						{
							Match match = bestRegex.Match( Path.GetFileNameWithoutExtension( fi.Name ) );
							var tuple = GetDiscTrackNumberFromMatch( match );
							return tuple;
						}
					);

				return ( true, dict );
			}
		}

		public static (Int32? disc, Int32? track, String err) GetDiscTrackNumberFromFileName(String fileName, Boolean checkSiblings)
		{
			// 1. If there's no disc information in the parent folder path (only look at the parent directory, not other ancestors):
			FileInfo fileInfo = new FileInfo( fileName );
			
			Match parentDirectoryDiscNumberMatch = FileNameDiscNumberRegex.Match( fileInfo.Directory.Name );
			if( parentDirectoryDiscNumberMatch.Success )
			{
				Int32 discNumberFromParentDirectory = Int32.Parse( parentDirectoryDiscNumberMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture );

				// As disc information is in the directory, we must assert that it is either NOT in the filename itself, or the filename matches:

				Match fileNameDiscNumberMatch = FileNameDiscNumberRegex.Match( fileInfo.Name );
				if( fileNameDiscNumberMatch.Success )
				{
					Int32 discNumberFromFileName = Int32.Parse( fileNameDiscNumberMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture );
					if( discNumberFromFileName != discNumberFromParentDirectory ) return ( null, null, "Different disc numbers in parent directory and filename." );
				}

				(Int32? fnDisc, Int32? fnTrack, String fnErr ) = checkSiblings ? GetDiscTrackNumberFromFileNameAndSiblings( fileName ) : GetDiscTrackNumberFromFileNameOnly( fileName );
				if( fnErr != null ) return ( null, null, fnErr );

				if( fnDisc != null && discNumberFromParentDirectory != fnDisc.Value )
				{
					return ( null, null, "Different disc numbers in parent directory and filename." );
				}

				return ( discNumberFromParentDirectory, fnTrack, null );
			}
			else
			{
				// 2. If not, just check the filename.
				return checkSiblings ? GetDiscTrackNumberFromFileNameAndSiblings( fileName ) : GetDiscTrackNumberFromFileNameOnly( fileName );
			}
		}

		// These expressions assume the file extension (with leading dot) is not included, as that can include digits (e.g. ".mp3", ".mp4", etc)
		private static readonly Regex[] _fileNamePatterns = new String[]
			{
				// We can assume if there's 2 groups matched, then the first one will be the disc number. I can't think of any naming system where the disc number comes after the track number, or the track title coming after the track number.

				@"^(\d{1,3})\b", // File starts with 1 to 3 digits, presumably the track number (4 digits would be a year, I imagine. I wonder if 3 digits is too many and might conflate with a number in the band's name or album name if included in the filename)
				@"^(?:\D+)(\d{1,2})\b", // Non-digits, then 1-2 digits
				@"^(\d{1,2})(?:\D+)\b(\d{1,2})", // File starts with 1-2 digit, followed by non-digits (non-capturing group), then a word-boundary, then 1-2 digits again
				@"^(?:\D+)(\d{1,2})(?:\D+)\b(\d{1,2})", // Non-digits, then digits, then non-digits, then digits again.
			}
			.Select( s => new Regex( s, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline ) )
			.ToArray();

		public static Regex GetBestDiscTrackNumberRegexForDirectory(String directoryPath)
		{
			List<String> fileNames = Directory
				.GetFiles( directoryPath )
				.Select( fn => Path.GetFileNameWithoutExtension( fn ) )
				.ToList();

			if( fileNames.Count == 0 ) return null;

			Int32[] regexScores = new Int32[ _fileNamePatterns.Length ];
			for( Int32 i = 0; i < _fileNamePatterns.Length; i++ )
			{
				Int32 score = fileNames.Count( fn => _fileNamePatterns[i].IsMatch( fn ) );
				regexScores[i] = score;
			}

			// Is there an outright winner?
			Regex maxScoreRegex = null;
			{
				// The winning regex must match at least half of the songs in the directory, and at least 1 match.
				Int32 maxScore = Math.Max( 1, fileNames.Count / 2 );
				for( Int32 i = 0; i < regexScores.Length; i++ )
				{
					// If the score is the same, then use the current regex, because higher _fileNamePattern indexes have higher specificity.
					if( regexScores[i] >= maxScore )
					{
						maxScore = regexScores[i];
						maxScoreRegex = _fileNamePatterns[i];
					}
				}
			}

			return maxScoreRegex;
		}

		private static (Int32? disc, Int32? track, String err) GetDiscTrackNumberFromFileNameOnly(String fileName)
		{
			String fn = Path.GetFileNameWithoutExtension( fileName );

			// Use the most-specific regex:
			for( Int32 i = _fileNamePatterns.GetUpperBound(0); i >= 0; i-- )
			{
				Match match = _fileNamePatterns[i].Match( fn );
				if( match.Success ) return GetDiscTrackNumberFromMatch( match );
			}

			return ( null, null, "File name did not match any common pattern." );
		}

		private static (Int32? disc, Int32? track, String err) GetDiscTrackNumberFromFileNameAndSiblings(String fileName)
		{
			// Because track filenames can contain extra digits, just as part of their name, it's best to see which regex matches the most of the file's siblings, so we're sure there's a good pattern to use.
			String fn = Path.GetFileNameWithoutExtension( fileName );

			Regex mostCommonPatternInDirectory = GetBestDiscTrackNumberRegexForDirectory( Path.GetDirectoryName( fileName ) );
			if( mostCommonPatternInDirectory == null )
			{
				return ( null, null, "No pattern matches at least half of the files in the directory. Cannot confidently extract disc and track information from \"" + fn + "\"." );
			}

			Match match = mostCommonPatternInDirectory.Match( fn );
			if( !match.Success )
			{
				// If this file bucks the trend, then it probably doesn't contain the track number in the filename, as a one-off or minority (e.g. those "assorted" tracks I often put in albums that they're related to, e.g. as B-sides or demo material).
				return ( null, null, "File does not match most commonly-used pattern in directory." );
			}
			else
			{
				return GetDiscTrackNumberFromMatch( match );
			}
		}

		private static (Int32? disc, Int32? track, String err) GetDiscTrackNumberFromMatch(Match match)
		{
			if( !match.Success )
			{
				return ( null, null, "File name does not match pattern." );
			}
			else if( match.Groups.Count == 2 ) // [0] = Input, [1] = Track
			{
				Int32 trackNumber = Int32.Parse( match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture );
				return ( null, trackNumber, null );
			}
			else if( match.Groups.Count == 3 ) // [0] = Input, [1] = Disc, [2] = Track
			{
				Int32 discNumber  = Int32.Parse( match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture );
				Int32 trackNumber = Int32.Parse( match.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture );
				return ( discNumber, trackNumber, null );
			}
			else
			{
				throw new Exception( "Unexpected pattern match results." );
			}
		}
	}
}
