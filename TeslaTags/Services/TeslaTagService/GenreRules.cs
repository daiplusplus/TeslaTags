using System;

namespace TeslaTags
{
	public class GenreRules
	{
		public AssortedFilesGenreAction AssortedFilesAction { get; set; }

		public GenreAction ArtistAlbumWithGuestArtistsAction { get; set; }
		
		public GenreAction ArtistAssortedAction { get; set; }

		public GenreAction ArtistAlbumAction { get; set; }

		public GenreAction CompilationAlbumAction { get; set; }

		public Boolean AlwaysNoop =>
			this.AssortedFilesAction               == AssortedFilesGenreAction.Preserve &&
			this.ArtistAlbumWithGuestArtistsAction == GenreAction.Preserve &&
			this.ArtistAssortedAction              == GenreAction.Preserve &&
			this.ArtistAlbumAction                 == GenreAction.Preserve &&
			this.CompilationAlbumAction            == GenreAction.Preserve;
	}

	public enum GenreAction
	{
		Preserve  = 0,
		Clear     = 1,
		UseArtist = 2
	}

	public enum AssortedFilesGenreAction
	{
		Preserve      = 0,
		Clear         = 1,
		UseArtist     = 2,
		UseFolderName = 3
	}
}
