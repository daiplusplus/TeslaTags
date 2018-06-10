using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TeslaTags.Tests
{
	[TestClass]
	public class DiscTrackNumberTests
	{
		private class TestCase
		{
			public TestCase(Int32? fileNameDisc, Int32? fileNameTrack, Int32? directoryDisc, Int32? directoryTrack, Int32? tagDisc, Int32? tagTrack, String fileName)
			{
				this.FileName = fileName;
				this.FileNameDisc = fileNameDisc;
				this.FileNameTrack = fileNameTrack;
				this.DirectoryDisc = directoryDisc;
				this.DirectoryTrack = directoryTrack;
				this.TagDisc = tagDisc;
				this.TagTrack = tagTrack;
			}

			public String FileName { get; }
			
			public Int32? FileNameDisc  { get; }
			public Int32? FileNameTrack { get; }

			public Int32? DirectoryDisc  { get; }
			public Int32? DirectoryTrack { get; }

			public Int32? TagDisc  { get; }
			public Int32? TagTrack { get; }
		}

		private static readonly Int32? N = null;

		private static readonly TestCase[] _testCases = new TestCase[]
		{
			new TestCase( fileNameDisc: 1, fileNameTrack: 11, directoryDisc: 1, directoryTrack: 11, tagDisc: 1, tagTrack: 11, fileName: @"C:\Users\David\Music\Genesis\2004 - Platinum Collection\Disc 1\11 That's All.mp3" ),
			new TestCase( fileNameDisc: 1, fileNameTrack:  1, directoryDisc: 1, directoryTrack:  1, tagDisc: 1, tagTrack:  1, fileName: @"C:\Users\David\Music\Unkle\UNKLESounds - 2001 - Do Androids Dream of Electric Beats\Disc 1\01 - UNKLE - Intro.mp3" ),
			new TestCase( fileNameDisc: 3, fileNameTrack: 16, directoryDisc: 3, directoryTrack: 16, tagDisc: 3, tagTrack: 16, fileName: @"C:\Users\David\Music\Unkle\UNKLESounds - 2001 - Do Androids Dream of Electric Beats\Disc 3\16 - UNKLE - Rabbit In Your Headlights (UNKLEsounds Edit).mp3" ),
			new TestCase( fileNameDisc: N, fileNameTrack:  1, directoryDisc: N, directoryTrack:  1, tagDisc: 1, tagTrack:  1, fileName: @"C:\Users\David\Music\_Games\Grey Goo\01 - Grey Goo Main Theme.mp3" ),
			new TestCase( fileNameDisc: N, fileNameTrack: 18, directoryDisc: N, directoryTrack: 18, tagDisc: 1, tagTrack: 18, fileName: @"C:\Users\David\Music\_Games\Grey Goo\18 - War Has Given You a Voice  Scene05 Outro.mp3" ), // note the "Scene05", it should not be interpreted as a track or disc number
			new TestCase( fileNameDisc: N, fileNameTrack: 19, directoryDisc: N, directoryTrack: 19, tagDisc: 2, tagTrack:  1, fileName: @"C:\Users\David\Music\_Games\Grey Goo\19 - The Humans.mp3" ), // This file already has tags saying Disc=2,Track=1
			new TestCase( fileNameDisc: N, fileNameTrack: 34, directoryDisc: N, directoryTrack: 34, tagDisc: 2, tagTrack: 16, fileName: @"C:\Users\David\Music\_Games\Grey Goo\34 - Catalyst Detonation  Scene10 Outro.mp3" ), // This file already has tags saying Disc=2,Track=16
			new TestCase( fileNameDisc: N, fileNameTrack: 35, directoryDisc: N, directoryTrack: 35, tagDisc: 3, tagTrack:  1, fileName: @"C:\Users\David\Music\_Games\Grey Goo\35 - The Goo.mp3" ), // This file already has tags saying Disc=3,Track=1
			new TestCase( fileNameDisc: N, fileNameTrack: 50, directoryDisc: N, directoryTrack: 50, tagDisc: 3, tagTrack: 16, fileName: @"C:\Users\David\Music\_Games\Grey Goo\50 - War is Evolving.mp3" ), // This file already has tags saying Disc=3,Track=16
			new TestCase( fileNameDisc: 1, fileNameTrack:  1, directoryDisc: 1, directoryTrack:  1, tagDisc: 1, tagTrack:  1, fileName: @"C:\Users\David\Music\_Various Artists\100 Hits - The Best Rock and Power Ballads\1-01 Bat Out Of Hell.mp3" ),
			new TestCase( fileNameDisc: 1, fileNameTrack: 20, directoryDisc: 1, directoryTrack: 20, tagDisc: 1, tagTrack: 20, fileName: @"C:\Users\David\Music\_Various Artists\100 Hits - The Best Rock and Power Ballads\1-20 When I See You Smile.mp3" ),
			new TestCase( fileNameDisc: 2, fileNameTrack:  1, directoryDisc: 2, directoryTrack:  1, tagDisc: 2, tagTrack:  1, fileName: @"C:\Users\David\Music\_Various Artists\100 Hits - The Best Rock and Power Ballads\2-01 Carry On Wayward Son.mp3" ),
			new TestCase( fileNameDisc: 2, fileNameTrack: 20, directoryDisc: 2, directoryTrack: 20, tagDisc: 2, tagTrack: 20, fileName: @"C:\Users\David\Music\_Various Artists\100 Hits - The Best Rock and Power Ballads\2-20 Rock And Roll Dreams Come Throu.mp3" ),
			new TestCase( fileNameDisc: 5, fileNameTrack: 20, directoryDisc: 5, directoryTrack: 20, tagDisc: 5, tagTrack: 20, fileName: @"C:\Users\David\Music\_Various Artists\100 Hits - The Best Rock and Power Ballads\5-20 Dancing In The Moonlight.mp3" ),
			new TestCase( fileNameDisc: 1, fileNameTrack:  1, directoryDisc: 1, directoryTrack:  1, tagDisc: 1, tagTrack:  1, fileName: @"C:\Users\David\Music\Unkle\UNKLESounds - 2005 - Edit Music for a Film\Disc 1\Disc 1 - Widescreen Edit - A New Hope - 01 - Intro_ 20th Century Fox, Money_Power_Respect, THX Deep Note.mp3" ), // note the "20" in the filename in addition to "01"
			new TestCase( fileNameDisc: 1, fileNameTrack: 15, directoryDisc: 1, directoryTrack: 15, tagDisc: 1, tagTrack: 15, fileName: @"C:\Users\David\Music\Unkle\UNKLESounds - 2005 - Edit Music for a Film\Disc 1\Disc 1 - Widescreen Edit - A New Hope - 15 - The Second Star to the Right.mp3" ),
			new TestCase( fileNameDisc: 2, fileNameTrack:  1, directoryDisc: 2, directoryTrack:  1, tagDisc: 2, tagTrack:  1, fileName: @"C:\Users\David\Music\Unkle\UNKLESounds - 2005 - Edit Music for a Film\Disc 2\Disc 2 - Bonus Material Edit - Strikes Back - 01 - MGM, Lost in Translation, 2010.mp3" ),
			new TestCase( fileNameDisc: 2, fileNameTrack:  7, directoryDisc: 2, directoryTrack:  7, tagDisc: 2, tagTrack:  7, fileName: @"C:\Users\David\Music\Unkle\UNKLESounds - 2005 - Edit Music for a Film\Disc 2\Disc 2 - Bonus Material Edit - Strikes Back - 07 - Dylan Rhymes - The Way, Assault on Precinct 13 Radio Spot.mp3" ),
			new TestCase( fileNameDisc: N, fileNameTrack:  8, directoryDisc: N, directoryTrack:  8, tagDisc: N, tagTrack:  8, fileName: @"C:\Users\David\Music\_Games\MDK\2001 - MDK2\track-08.mp3" ),
			new TestCase( fileNameDisc: N, fileNameTrack:  N, directoryDisc: N, directoryTrack:  N, tagDisc: N, tagTrack:  N, fileName: @"C:\Users\David\Music\_Various Artists\_Downloaded\IndieRock\Tube Tops 2000 - Rock and Roll, Part 2.mp3" ),
			new TestCase( fileNameDisc: N, fileNameTrack:  0, directoryDisc: N, directoryTrack:  0, tagDisc: N, tagTrack:  0, fileName: @"C:\Users\David\Music\Lemon Jelly\2005-01-31 - '64-'95\00 - Yes.mp3" ), // note the use of "0"
			new TestCase( fileNameDisc: 1, fileNameTrack: 12, directoryDisc: 1, directoryTrack: 12, tagDisc: 1, tagTrack: 12, fileName: @"C:\Users\David\Music\Unkle\UNKLESounds - 2015 - Global Underground 41 Naples\1-12 …Like Clockwork (UNKLE Remix).mp3" ) // note the many digits in the album name
		};

		[TestMethod]
		public void TestDiscTrackNumberExtractionFromFileNameOnly()
		{
			Int32 i = 0;
			foreach( TestCase testCase in _testCases )
			{
				(Int32? actualDisc, Int32? actualTrack, String err) = Values.GetDiscTrackNumberFromFileName( testCase.FileName, checkSiblings: false );

				//Assert.IsNull( err );
				Boolean fnContainsDigits = System.IO.Path.GetFileNameWithoutExtension( testCase.FileName ).Any( c => Char.IsDigit(c) );
				if( testCase.FileNameDisc == null && testCase.FileNameTrack == null && fnContainsDigits )
				{
					Assert.IsNotNull( err, "Expected error for \"" + testCase.FileName + "\"." );
				}
				else
				{
					Assert.IsNull( err, "Expected no error for \"" + testCase.FileName + "\"." );
				}

				Int32? expectedDisc  = testCase.FileNameDisc;
				Int32? expectedTrack = testCase.FileNameTrack;

				Assert.AreEqual( expectedDisc , actualDisc , "Disc number" );
				Assert.AreEqual( expectedTrack, actualTrack, "Track number" );

				i++;
			}
		}

		[TestMethod]
		public void TestDiscTrackNumberExtractionFromFileNameInFolderWithSiblings()
		{
			Int32 i = 0;
			foreach( TestCase testCase in _testCases )
			{
				(Int32? actualDisc, Int32? actualTrack, String err) = Values.GetDiscTrackNumberFromFileName( testCase.FileName, checkSiblings: true );

				//Assert.IsNull( err );
				Boolean fnContainsDigits = System.IO.Path.GetFileNameWithoutExtension( testCase.FileName ).Any( c => Char.IsDigit(c) );
				if( testCase.FileNameDisc == null && testCase.FileNameTrack == null && fnContainsDigits )
				{
					Assert.IsNotNull( err, "Expected error for \"" + testCase.FileName + "\"." );
				}
				else
				{
					Assert.IsNull( err, "Expected no error for \"" + testCase.FileName + "\"." );
				}

				Int32? expectedDisc  = testCase.DirectoryDisc;
				Int32? expectedTrack = testCase.DirectoryTrack;

				Assert.AreEqual( expectedDisc , actualDisc , "Disc number" );
				Assert.AreEqual( expectedTrack, actualTrack, "Track number" );

				i++;
			}
		}
	}
}
