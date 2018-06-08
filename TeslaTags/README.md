TeslaTags

Problem statement:
==================

The music-library in Tesla's infotainment system used in the Model S, Model X and Model 3 cars does not display audio tracks according to the convention shared by almost all other music library management tools (including iTunes, Windows Media Player, Winamp Library, etc) like so:

* Disregards "Album Artist" and uses only "Artist" exclusively.
* The "Artists" view lists all distinct artists (and their tracks) only if those tracks have a tagged Album, so tracks without an Album value are not listed.
* The "Albums" view identifies an album by "Artist" + "Album" tags and disregards the "Is Compilation" tag - so the library does not recognise albums with multiple artists (e.g. compilation albums) or albums by a primary artist with guest artists.
* The "Folders" view sorts tracks by their "Title" tag and disregards the original Filename and "Track number"/"Disc number" tags.

Solutions:

* By embedding invisible whitespace unicode characters into the "Title" tag to force a certain lexicographical ordering the tracks will be sorted by disc+track number in the Folders view without affecting their appearance.
* By copying "Album Artist" into the "Artist" value, it means that 
* The "Genre" field could be abused to provide another kind of grouping, perhaps for storing the original "Artist" value.

* Playlists could be supported by writing hard-links in the filesystem (FAT32 does support this) though custom ordering won't work and assuming that the software won't display these duplicate tracks.

Scenarios:
==========

-----------
Scenario 1:
-----------
* Single-artist albums.
* Filesystem is arranged like this: "{AlbumArtist}\{AlbumDate} - {AlbumName}\{DiscNumber}-{TrackNumber} - {TrackTitle}.mp3"
* Tracks are using the DiscNumber, TrackNumber, TrackTitle, Artist and AlbumAritst tags correctly.

Tesla media library Artist and Album view: Correct
Tesla media library Folder view: Incorrect, files are sorted by TrackTitle, not FileName or DiscNumber/TrackNumber

Proposed fix:
* Correct lexicographical sorting by prepending invisible unicode characters in the TrackTitle.

Disadvantage:
* Lose ability to show these tracks alphabetically.

-----------
Scenario 2:
-----------
* Multiple-artist albums, non-compilation (i.e. not "Various artists"), e.g. Starship Troopers soundtrack (AlbumArtist for all tracks and Artist for some tracks == "Basil Poledouris", but for a few other tracks Artist == "Zoe Poledouris")
* Filesystem is arranged like this: "{AlbumArtist}\{AlbumDate} - {AlbumName}\{DiscNumber}-{TrackNumber} - {TrackTitle}.mp3"
* Tracks are using the DiscNumber, TrackNumber, TrackTitle, Artist and AlbumAritst tags correctly.

Tesla media library Artist and Album view: Incorrect. "Artist\Zoe Poledouris" is listed.
Tesla media library Folder view: Incorrect, files are sorted by TrackTitle, not FileName or DiscNumber/TrackNumber
* Cannot play full album from Artist or Albums menus because contributing artists' songs are not listed.
* Cannot play full album from Folders view because songs are out-of-order.

Proposed fix:
* When the AlbumArtist != Artist && AlbumAritst != "Various Artists", then:
	1: Prepend/copy the Artist value into the TrackTitle tag with a hyphen.
	2: Copy the AlbumArtist tag value into the Artist tag.

Alternative fix:
* Just the track-title sorting fix described above (so that Folders view works as a way of playing these albums, though the Artists and Albums menus would still contain incorrect entries)

-----------
Scenario 3:
-----------
* "Various Artist" compilation albums, e.g. "Now That's What I Call Music" etc
* Filesystem is arranged like this: "Various Artists\{AlbumDate} - {AlbumName}\{DiscNumber}-{TrackNumber} - {Artist} - {TrackTitle}.mp3"
* Tracks are using the DiscNumber, TrackNumber, TrackTitle, Artist and AlbumAritst tags correctly.

Tesla media library Artist and Album view: Incorrect. Each artist is displayed separately. And the Album is listed for each artist in the top-level Albums view.
Tesla media library Folder view: Incorrect, files are sorted by TrackTitle, not FileName or DiscNumber/TrackNumber
* Cannot play full album from Artist or Albums menus because there is no single root artist in Artists list, and Album is listed for-each-artist in the Albums view.
* Cannot play full album from Folders view because songs are out-of-order.

Proposed fix:
* Prepend/copy the Artist value into the TrackTitle tag with a hyphen.
* Set Artist to "Various Artists".

-----------
Scenario 4:
-----------
* Various loose files, e.g. random downloaded files not part of any album.
* Filesystem is arranged like this: "Various Artists\{Genre}]\{Artist} - {TrackTitle}.mp3"
* AlbumArtist is always set to "Various Artists"
* Album and TrackNumber is always cleared. Consequently they don't spam-up the Artists menu.

Tesla media library Artist and Album view: Not listed. Artists and Albums are only listed if tracks have both tags present.
Tesla media library Folder view: Working, though tracks are sorted by Title rather than Artist.

Proposed fix:
* No action necessary, tracks will be displayed in Folder view correctly.

References:
===========

* TeslaTunes, for macOS.
	See the `twiddleTags` function in https://github.com/tattwamasi/TeslaTunes/blob/bb56bca7c86750b1b9b5f88f13297d8cc7678dcb/TeslaTunes/CopyConvertDirs.mm

Test files needed to confirm practicality of proposed fixes:
============================================================

* What happens if there's a mismatch between ID3v1 and ID3v2 values? which does Tesla MP prefer?
* What forms of ID3v2 Unicode encoding are supported? (We know it is supported as it shows non-Latin artist names correctly, and they sell their cars in China too)
* ID3v1 does not define text encoding, should try ASCII, UTF-8, UTF-16LE, UTF-16BE and see what happens. (Would UTF-16BE be different on ARM vs x86 MCUs?)
* How does it handle leading invisible Unicode whitespace? Does it do a Trim()?

Future work:
============


* Dan Wentz (Freespace 2 soundtrack). Tags are messed-up.
* Where's my Tubeway Army and other Gary Numan songs?
* Non-"Various artists" songs that are missing an album are not displayed
   * specifically, when they're not located under my "_Downloads" directory.
   * e.g. Art of Noise - Paranoimia feat. Max Headroom
   * Solution: Give them a bogus Album value, e.g. "No album"


Ideas:
* Ensure ID3v2 is present, warn otherwise.
* Warn if album-year is missing?
* Experiment to see if the (Artist/Album/etc)Sort tags (as seen in iTunes) work
* Make processing idempotent, so running it again on the same USB stick won't make any redundant or destructive changes.
