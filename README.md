# <img src="https://raw.githubusercontent.com/Jehoel/TeslaTags/master/TeslaTags.Gui/Icons/Artboard%20164.png" alt="Logo" /> TeslaTags

* By Dai Rees - https://github.com/Jehoel/TeslaTags  
* <a href="https://teslamotorsclub.com/tmc/threads/teslatags-mp3-flac-retagger-for-windows.117430/">DaiPlusPlus on TeslaMotorsClub Forums</a>
* On <a href="https://www.reddit.com/r/teslamotors/comments/8q0fpe/hi_rteslamotors_i_wrote_an_mp3flac_retagging_tool/">reddit.com/r/teslamotors</a> too

## Instructions for use

<img src="https://user-images.githubusercontent.com/1693078/41264291-c961e97a-6da0-11e8-894e-f330bef6083f.png" width="300" />

1. Copy your MP3 and FLAC music from your computer onto your USB stick
    * Don't forget to use FAT32 or ext4 because (as of early 2018) NTFS and exFAT are still not supported.
    * To format large (32GB+) USB sticks as FAT32 on Windows use a tool like Rufus ( https://rufus.akeo.ie/ ) as Windows' own disk format tools will not let you.
1. I strongly recommend validating your MP3 files first, see the [Recommended MP3 Tools section below](#recommended-mp3-tools) 
1. Run `TeslaTags.Gui.exe`
1. Change the "Music root" directory to your USB stick.
1. Set the options:

    1. **Validate only**:
    Leave the "Validate-only" checkbox checked if you want to see what changes will be made to your files. **Your files will not be modified if you leave the "Validate-only" checkbox checked!**. Uncheck the box if you're comfortable having your files re-tagged. (This feature is a safety device to prevent people accidentally re-tagging their music files)
    1. **Restore files**:
    This option will undo a previous retagging operation, but only to MP3 files (not FLAC) and is experimental. Use at your own risk. The program stores original tag data in a new ID3v2 frame.
    1. **Exclude iTunes folders**:
    This excludes the "iTunes Media" folder, and other folders with "iTunes" in their name from being processed by this tool, it helps save time if you don't have any music files in your iTunes folders.
    1. **Genre handling**:
    This repurposes the `Genre` tag in your files so you can use it to jump to Artists (that don't have any albums) or to assorted-files folders as neither show-up in the normal Tesla music library.
	      * **Preserve**: This keeps all your `Genre` tag values the way they are and doesn't change them, unless overridden for "Assorted files folders", "Compilation folders", and "Albums' guest artists" below.
	      * **Clear**: This clears the `Genre` tag in all your files.
	      * **Use artist name**: This always stores the artist name in the `Genre` field, so the Tesla Genres menu then becomes a "All songs by this artist" menu. This menu can fill up if you have lots of assorted files or compilations.
	      * **Assorted files folders**: Only applies to folders classified as `AssortedFiles` in the main table.
	      * **Compilation folders**: Only applies to folders classified as `CompilationAlbum` in the main table.
	      * **Albums' guest artists**: Only applies to tracks by Guest Artists in folders classified as `ArtistAlbumWithGuestArtists` in the main table.

1. A progress bar appears at the top. It disappears when the program is complete. Provided that "Validate only" is unchecked then your USB stick is ready to be put back into your Tesla car.

## Background Problem statement:

The music-library in Tesla's infotainment system used in the Model S, Model X and Model 3 cars does not display audio tracks according to the convention shared by almost all other music library management tools (including iTunes, Windows Media Player, Winamp Library, etc) like so:

* It disregards "Album Artist" and uses only "Artist" exclusively.
* The "Artists" view lists all distinct artists (and their tracks) only if those tracks have a tagged Album, so tracks without an Album value are not listed.
* The "Albums" view identifies an album by "Artist" + "Album" tags and disregards the "Is Compilation" tag - so the library does not recognise albums with multiple artists (e.g. compilation albums) or albums by a primary artist with guest artists, instead it treats these tracks as belonging to separate albums.
* The "Folders" view sorts tracks by their "Title" tag and disregards the original Filename and "Track number"/"Disc number" tags.

Initially proposed solutions:
* I thought of embedding invisible whitespace Unicode characters into the "Title" tag field to force a certain lexicographical ordering the tracks will be sorted by disc+track number in the Folders view without affecting their appearance, however my experiments revealed that the library software ignores these characters when sorting (this is intentional, as the Unicode specification says these zero-width characters don't affect text collation). This also caused some "missing character" gylphs to be rendered on-screen, interestingly enough.
* Adding the track number as a prefix to the track title would work, however if you don't care about Folder view for playing albums this is unnecessary and takes up space in the UI.

* Playlists could be supported by writing hard-links in the filesystem (FAT32 does support this) though custom ordering won't work and assuming that the software won't display these duplicate tracks.

* The "Genre" field could be abused to provide another kind of grouping, perhaps for storing the original "Artist" value.


## Scenarios:

### Scenario 1 `ArtistAlbum`:

Single artist albums, which are the most common type of album, such as Pink Floyd's [Dark Side of the Moon](https://en.wikipedia.org/wiki/The_Dark_Side_of_the_Moon). Both the album itself and all songs on the album are attributed to the same single artist.

Scenario:
* All tracks have the same value in both `AlbumArtist` and `Artist` tags (e.g. "Pink Floyd").
* All tracks have the same `Album` tag value (e.g. "The Dark Side of the Moon").
* All track files are in the same filesystem directory or child directories (for multi-disc albums).
* Tracks are using the `DiscNumber`, `TrackNumber`, `TrackTitle`, `Artist`, `Album`, and `AlbumArtist` tag fields correctly.

Example tags for _The Dark Side of the Moon_:

    File                                AlbumArtist    Artist        Album                        TrackNumber     Title
    ----------------------------------------------------------------------------------------------------------------------------
    01 - Speak To Me (Breathe).mp3      Pink Floyd     Pink Floyd    The Dark Side of the Moon    1               Speak To Me (Breathe)
    02 - On The Run.mp3                 Pink Floyd     Pink Floyd    The Dark Side of the Moon    2               On The Run
    03 - Time.mp3                       Pink Floyd     Pink Floyd    The Dark Side of the Moon    3               Time
    04 - The Great Gig In The Sky.mp3   Pink Floyd     Pink Floyd    The Dark Side of the Moon    4               The Great Gig In The Sky
    05 - Money.mp3                      Pink Floyd     Pink Floyd    The Dark Side of the Moon    5               Money
    06 - Us and Them.mp3                Pink Floyd     Pink Floyd    The Dark Side of the Moon    6               Us and Them
    07 - Any Colour You Like.mp3        Pink Floyd     Pink Floyd    The Dark Side of the Moon    7               Any Colour You Like
    08 - Brain Damage.mp3               Pink Floyd     Pink Floyd    The Dark Side of the Moon    8               Brain Damage
    09 - Eclipse.mp3                    Pink Floyd     Pink Floyd    The Dark Side of the Moon    9               Eclipse

Result:
* Tesla media library Artist and Album view: **Correct**
* Tesla media library Folder view: **Incorrect**, files are sorted by TrackTitle, not FileName or DiscNumber/TrackNumber

Solution:
* No action. The main Artist > Album view is sufficient.

### Scenario 2 `ArtistAlbumWithGuestArtists`:

Sometimes a band or artist will have guest artists credited for a few tracks on an album. While the album and most of the tracks will be attributed to a single artist, a few songs will have a different `Artist` tag compared to the `AlbumArtist` tag. For example, the Deluxe edition of the Starship Troopers soundtrack is attributed to Basil Poledouris as the `AlbumArtist` on **all tracks** and as the `Artist` on most of the tracks, but two tracks have an `Artist` of "Zoe Poledouris".

Note that this is not the same thing as a featured artist on a track, e.g. "[Eve - Let Me Blow Ya Mind (feat. Gwen Stefani)](https://www.youtube.com/watch?v=Wt88GMJmVk0)".

Scenario:
* Multiple-artist albums that are not compilations (e.g. guest artists), e.g. Starship Troopers soundtrack (AlbumArtist for all tracks and Artist for some tracks is "Basil Poledouris", but for a few other tracks the Artist is "Zoe Poledouris")
* Tracks are using the `DiscNumber`, `TrackNumber`, `TrackTitle`, `Artist`, `Album`, and `AlbumArtist` tag fields correctly.

Example tags for _Starship Troopers Soundtrack Deluxe Edition_:

    File                                         AlbumArtist          Artist              Album                TrackNumber     Title
    ----------------------------------------------------------------------------------------------------------------------------
    1-01 Fed-Net #1, Bug Attack on News.mp3      Basil Poledouris     Basil Poledouris    Starship Troopers    1               Fed-Net #1, Bug Attack on News Reporter
    1-02 Kiss In The Park (Unused).mp3           Basil Poledouris     Basil Poledouris    Starship Troopers    2               Kiss in the Park (Unused)
    ...
    2-08 Into It.mp3                             Basil Poledouris     Zoe Poledouris      Starship Troopers    32              Into It
    2-09 I Have Not Been to Oxford Town.mp3      Basil Poledouris     Zoe Poledouris      Starship Troopers    33              I Have Not Been to Oxford Town
    2-10 Klendathu Battle (Version 1).mp3        Basil Poledouris     Basil Poledouris    Starship Troopers    34              Klendathu Battle (Version 1)
    ...

Result:
* Tesla media library Artist and Album view: **Incorrect**: "Zoe Poledouris" is listed as a separate artist under the Artists menu.
* Tesla media library Folder view: **Incorrect**, files are sorted by TrackTitle, not FileName or DiscNumber/TrackNumber.
* Problem: Cannot play full album from Artist or Albums menus because contributing artists' songs are not listed.
* Problem: Cannot play full album from Folders view because songs are out-of-order.

Solution:
* Prepend/copy the `Artist` value into the `TrackTitle` field with a hyphen.
* Copy the `AlbumArtist` value into the `Artist` field.

Output:

    File                                         AlbumArtist          Artist              Album                TrackNumber     Title
    ----------------------------------------------------------------------------------------------------------------------------
    1-01 Fed-Net #1, Bug Attack on News.mp3      Basil Poledouris     Basil Poledouris    Starship Troopers    1               Fed-Net #1, Bug Attack on News Reporter
    1-02 Kiss In The Park (Unused).mp3           Basil Poledouris     Basil Poledouris    Starship Troopers    2               Kiss in the Park (Unused)
    ...
    2-08 Into It.mp3                             Basil Poledouris     Basil Poledouris    Starship Troopers    32              Zoe Poledouris - Into It
    2-09 I Have Not Been to Oxford Town.mp3      Basil Poledouris     Basil Poledouris    Starship Troopers    33              Zoe Poledouris - I Have Not Been to Oxford Town
    2-10 Klendathu Battle (Version 1).mp3        Basil Poledouris     Basil Poledouris    Starship Troopers    34              Klendathu Battle (Version 1)
    ...

### Scenario 3 `CompilationAlbum`:

Music publishers often release albums that are compilations of the top charting singles from the previous year (e.g. "[Now That's What I Call Music](https://en.wikipedia.org/wiki/Now_That%27s_What_I_Call_Music!)"), or top hits from a particular genre (e.g. "100 Hits - The Best Rock and Power Ballads"). As there is no single artist to which the album is attributed to the `AlbumArtist` tag is set to "Various Artists".

Scenario:
* "Various Artists" compilation albums with no primary album artist, e.g.  etc "Now That's What I Call Music".
* All tracks have the `AlbumArtist` tag set to "Various Artists" with the actual artist in the `Artist` tag.
* Tracks are using the `DiscNumber`, `TrackNumber`, `TrackTitle`, `Artist`, `Album`, and `AlbumArtist` tag fields correctly.

Example tags for [_Moods 2 - A Contemporary Soundtrack_](https://www.discogs.com/Various-Moods-2-A-Contemporary-Soundtrack/release/6106123):

    File                            AlbumArtist         Artist           Album      TrackNumber     Title
    ----------------------------------------------------------------------------------------------------------------------------
    01 Caribbean Blue.mp3           Various Artists     Enya             Moods 2    1               Caribbean Blue
    02 Albatross.mp3                Various Artists     Fleetwood Mac    Moods 2    2               Albatross
    03 Tubular Bells (Part 1).mp3   Various Artists     Mike Oldfield    Moods 2    3               Tubular Bells (Part 1)
    ...

Result:
* Tesla media library Artist and Album view: **Incorrect**. Each artist is displayed separately. And the Album is listed for each artist in the top-level Tesla Albums view.
* Tesla media library Folder view: **Incorrect**, files are sorted by `TrackTitle`, not FileName or DiscNumber/TrackNumber.
* Cannot play full album from Artist or Albums menus because there is no single root artist in Artists list, and Album is listed for-each-artist in the Tesla Albums view.
* Cannot play full album from Folders view because songs are out-of-order.

Solution:
* Prepend/copy the `Artist` value into the `TrackTitle` fields with a hyphen.
* Set `Artist` to "Various Artists".

Output:

    File                            AlbumArtist         Artist           Album      TrackNumber     Title
    ----------------------------------------------------------------------------------------------------------------------------
    01 Caribbean Blue.mp3           Various Artists     Various Artists  Moods 2    1               Enya - Caribbean Blue
    02 Albatross.mp3                Various Artists     Various Artists  Moods 2    2               Fleetwood Mac - Albatross
    03 Tubular Bells (Part 1).mp3   Various Artists     Various Artists  Moods 2    3               Mike Oldfield - Tubular Bells (Part 1)
    ...

### Scenario 4 `AssortedFiles`:

Just a folder with songs by different artists and not part of any album, e.g. a folder with individually downloaded songs from Amazon's MP3 store. All files will have `AlbumArtist` set to "Various Artists" (this must be done manually by you as typically songs from online music stores have `AlbumArtist` set to `Artist`).

    File                              AlbumArtist         Artist           Album   TrackNumber   Title
    ----------------------------------------------------------------------------------------------------------------------------
    Ace of Base - Sign, The.mp3       Various Artists     Ace of Base                            The Sign
    Adele - Rumour Has It.mp3         Various Artists     Adele                                  Rumour Has It
    Afroman - Because I Got High.mp3  Various Artists     Afroman                                Because I Got High
    ...

Scenario:
* Various loose files, e.g. random downloaded files not part of any album.
* `AlbumArtist` is always set to "Various Artists"
* `Album` and `TrackNumber` are both always cleared. Consequently they don't spam-up the Artists menu.

Result:
* Tesla media library Artist and Album view: **Not listed**. Artists and Albums are only listed if tracks have both `Artist` and `Album` tag fields present.
* Tesla media library Folder view: **Working**, though tracks are sorted by Title rather than Artist.

Solution:
* No main action necessary, tracks will be displayed in Folder view correctly.
* Possible improvement: Set the `Genre` tag field to the name of the track's Folder or the track's Artist to enable direct access via the Tesla Genres menu.
* Alternative: Set the `Album` field to "No Album" - but only if the `Artist` is seen in at least one Album (or has at least 2 other assorted tracks) to prevent spamming the Tesla Artists view.

### Scenario 5 `ArtistAssorted`:

Scenario:
* Various loose files all by the same artist in the same folder. All tracks in the folder have the same `Artist` tag value (i.e. there are no songs by other artists in the same folder).
* Tracks are using the `TrackTitle`, `Artist` and `AlbumArtist` tag fields correctly. The `DiscNumber`, `TrackNumber`, and `Album` tag fields are cleared.

Result:
* Tesla media library Artist and Album view: **Not listed**. Artists and Albums are only listed if tracks have both `Artist` and `Album` tag fields present.
* Tesla media library Folder view: **Correct**. Tracks are sorted by Title correctly.

Solution:
* Set the `Album` tag value to "No Album" so they're accessible under the Tesla Artists menu without going into Folder view.

## References:

* TeslaTunes, for macOS.
	See the `twiddleTags` function in https://github.com/tattwamasi/TeslaTunes/blob/bb56bca7c86750b1b9b5f88f13297d8cc7678dcb/TeslaTunes/CopyConvertDirs.mm

## Unanswered questions:

* Q: What happens if there's a mismatch between ID3v1 and ID3v2 values? which does Tesla MP prefer?
* Q: What forms of ID3v2 Unicode encoding are supported? (We know it is supported as it shows non-Latin artist names correctly, and they sell their cars in China too)
  * A: UTF-8 seems to work fine.
* Q: ID3v1 does not define text encoding, should try ASCII, UTF-8, UTF-16LE, UTF-16BE and see what happens. (Would UTF-16BE be different on ARM vs x86 MCUs?)
* Q: How does it handle leading invisible Unicode whitespace? Does it do a Trim()?
  * Leading whitespace (including zero-width whitespace) is not rendered and does not affect track collation order (i.e. the Tesla software performs Unicode collation, not "dumb" binary sorting). Certain zero-width whitespace characters are rendered with a "missing character" glypth and rendered as an overlay on top of other characters.
  
## Recommended MP3 tools

I personally strongly recommend these tools for automatically validating and repairing with MP3 files. They pick up validation issues and other problems with MP3 files and can repair most of them. This has fixed many files that refused to play in my Tesla's music player.

* MP3Val: http://mp3val.sourceforge.net/
* MP3Diags: http://mp3val.sourceforge.net/

You should run both MP3Val and MP3Diags on your MP3 collection because sometimes one tool will identify issues the other tool didn't.

I also recommend MP3DirectCut for editing, slicing and cutting MP3 files because it edits MP3 block data directly without any transcoding and saves files without reencoding, preserving quality: http://mpesch3.de1.cc/mp3dc.html

