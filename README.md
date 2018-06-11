TeslaTags

By Dai Rees - https://github.com/Jehoel/TeslaTags
DaiPlusPlus on TeslaMotorsClub Forums

Problem statement:
==================

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


Scenarios:
==========

-----------
Scenario 1 `ArtistAlbum`:
-----------
Scenario:
* Single-artist albums.
* Filesystem is arranged like this: "`{AlbumArtist}\{AlbumDate} - {AlbumName}\{DiscNumber}-{TrackNumber} - {TrackTitle}.mp3`"
* Tracks are using the `DiscNumber`, `TrackNumber`, `TrackTitle`, `Artist`, `Album`, and `AlbumArtist` tag fields correctly.

Result:
* Tesla media library Artist and Album view: Correct
* Tesla media library Folder view: Incorrect, files are sorted by TrackTitle, not FileName or DiscNumber/TrackNumber

Solution:
* No action. The main Artist > Album view is sufficient.

-----------
Scenario 2 `ArtistAlbumWithGuestArtists`:
-----------
Scenario:
* Multiple-artist albums that are not compilations (e.g. guest artists), e.g. Starship Troopers soundtrack (AlbumArtist for all tracks and Artist for some tracks is "Basil Poledouris", but for a few other tracks the Artist is "Zoe Poledouris")
* Filesystem is arranged like this: "`{AlbumArtist}\{AlbumDate} - {AlbumName}\{DiscNumber}-{TrackNumber} - {TrackTitle}.mp3`"
* Tracks are using the `DiscNumber`, `TrackNumber`, `TrackTitle`, `Artist`, `Album`, and `AlbumArtist` tag fields correctly.

Result:
* Tesla media library Artist and Album view: Incorrect. "Artist\Zoe Poledouris" is listed.
* Tesla media library Folder view: Incorrect, files are sorted by TrackTitle, not FileName or DiscNumber/TrackNumber.
* Problem: Cannot play full album from Artist or Albums menus because contributing artists' songs are not listed.
* Problem: Cannot play full album from Folders view because songs are out-of-order.

Solution:
* Prepend/copy the `Artist` value into the `TrackTitle` field with a hyphen.
* Copy the `AlbumArtist` value into the `Artist` field.

-----------
Scenario 3 `CompilationAlbum`:
-----------
Scenario:
* "Various Artists" compilation albums with no primary album artist, e.g. "Now That's What I Call Music" etc
* Filesystem is arranged like this: "`Various Artists\{AlbumDate} - {AlbumName}\{DiscNumber}-{TrackNumber} - {Artist} - {TrackTitle}.mp3`"
* Tracks are using the `DiscNumber`, `TrackNumber`, `TrackTitle`, `Artist`, `Album`, and `AlbumArtist` tag fields correctly.

Result:
* Tesla media library Artist and Album view: Incorrect. Each artist is displayed separately. And the Album is listed for each artist in the top-level Tesla Albums view.
* Tesla media library Folder view: Incorrect, files are sorted by TrackTitle, not FileName or DiscNumber/TrackNumber.
* Cannot play full album from Artist or Albums menus because there is no single root artist in Artists list, and Album is listed for-each-artist in the Tesla Albums view.
* Cannot play full album from Folders view because songs are out-of-order.

Solution:
* Prepend/copy the `Artist` value into the `TrackTitle` fields with a hyphen.
* Set `Artist` to "Various Artists".

-----------
Scenario 4 `AssortedFiles`:
-----------
Scenario:
* Various loose files, e.g. random downloaded files not part of any album.
* Filesystem is arranged like this: "`Various Artists\{Genre}\{Artist} - {TrackTitle}.mp3`"
* `AlbumArtist` is always set to "Various Artists"
* `Album` and `TrackNumber` are both always cleared. Consequently they don't spam-up the Artists menu.

Result:
* Tesla media library Artist and Album view: Not listed. Artists and Albums are only listed if tracks have both `Artist` and `Album` tag fields present.
* Tesla media library Folder view: Working, though tracks are sorted by Title rather than Artist.

Solution:
* No main action necessary, tracks will be displayed in Folder view correctly.
* Possible improvement: Set the `Genre` tag field to the name of the track's Folder or the track's Artist to enable direct access via the Tesla Genres menu.
* Alternative: Set the `Album` field to "No Album" - but only if the `Artist` is seen in at least one Album (or has at least 2 other assorted tracks) to prevent spamming the Tesla Artists view.

-----------
Scenario 5 `ArtistAssorted`:
-----------
Scenario:
* Various loose files by the same artist, located under an artist's folder.
* Filesystem is arranged like this: "`{AlbumArtist}\{Artist} - {TrackTitle}.mp3`"
* Tracks are using the `TrackTitle`, `Artist` and `AlbumArtist` tag fields correctly. The `DiscNumber`, `TrackNumber`, and `Album` tag fields are cleared.

Result:
* Tesla media library Artist and Album view: Not listed. Artists and Albums are only listed if tracks have both `Artist` and `Album` tag fields present.
* Tesla media library Folder view: Correct. Tracks are sorted by Title correctly.

Solution:
* Set the `Album` tag value to "No Album" so they're accessible under the Tesla Artists menu without going into Folder view.

References:
===========

* TeslaTunes, for macOS.
	See the `twiddleTags` function in https://github.com/tattwamasi/TeslaTunes/blob/bb56bca7c86750b1b9b5f88f13297d8cc7678dcb/TeslaTunes/CopyConvertDirs.mm

Test files needed to confirm practicality of proposed fixes:
============================================================

* Q: What happens if there's a mismatch between ID3v1 and ID3v2 values? which does Tesla MP prefer?
* Q: What forms of ID3v2 Unicode encoding are supported? (We know it is supported as it shows non-Latin artist names correctly, and they sell their cars in China too)
  * A: UTF-8 seems to work fine.
* Q: ID3v1 does not define text encoding, should try ASCII, UTF-8, UTF-16LE, UTF-16BE and see what happens. (Would UTF-16BE be different on ARM vs x86 MCUs?)
* Q: How does it handle leading invisible Unicode whitespace? Does it do a Trim()?
  * Leading whitespace (including zero-width whitespace) is not rendered and does not affect track collation order (i.e. the Tesla software performs Unicode collation, not "dump" binary sorting). Certain zero-width whitespace characters are rendered with a "missing character" glypth and rendered as an overlay on top of other characters.
