using System;
using System.Globalization;
using System.Windows.Data;

namespace TeslaTags.Gui
{
	public class GenreRulesViewModel : BaseViewModel
	{
		/// <summary>Creates a new <see cref="GenreRules"/> object.</summary>
		public GenreRules GetRules()
		{
			if( this.OverrideGenreTagEnabled )
			{
				return new GenreRules()
				{
					AssortedFilesAction               = this.AssortedFilesAction,
					ArtistAlbumWithGuestArtistsAction = this.ArtistAlbumWithGuestArtistsAction,
					ArtistAssortedAction              = this.ArtistAssortedAction,
					ArtistAlbumAction                 = this.ArtistAlbumAction,
					CompilationAlbumAction            = this.CompilationAlbumAction
				};
			}
			else
			{
				return new GenreRules()
				{
					AssortedFilesAction               = AssortedFilesGenreAction.Preserve,
					ArtistAlbumWithGuestArtistsAction = GenreAction.Preserve,
					ArtistAssortedAction              = GenreAction.Preserve,
					ArtistAlbumAction                 = GenreAction.Preserve,
					CompilationAlbumAction            = GenreAction.Preserve
				};
			}
		}

		public void LoadFrom( GenreRules newRules )
		{
			if( newRules == null ) throw new ArgumentNullException(nameof(newRules));

			this.OverrideGenreTagEnabled      = !newRules.AlwaysNoop;

			this.AssortedFilesAction               = newRules.AssortedFilesAction;
			this.ArtistAlbumWithGuestArtistsAction = newRules.ArtistAlbumWithGuestArtistsAction;
			this.ArtistAssortedAction              = newRules.ArtistAssortedAction;
			this.ArtistAlbumAction                 = newRules.ArtistAlbumAction;
			this.CompilationAlbumAction            = newRules.CompilationAlbumAction;
		}

		private Boolean overrideGenreTagEnabled;
		public Boolean OverrideGenreTagEnabled
		{
			get { return this.overrideGenreTagEnabled; }
			set { this.Set( nameof(this.OverrideGenreTagEnabled), ref this.overrideGenreTagEnabled, value ); }
		}

		// https://stackoverflow.com/questions/397556/how-to-bind-radiobuttons-to-an-enum

		private AssortedFilesGenreAction assortedFilesAction;
		public AssortedFilesGenreAction AssortedFilesAction
		{
			get { return this.assortedFilesAction; }
			set { this.Set( nameof(this.AssortedFilesAction), ref this.assortedFilesAction, value ); }
		}

		private GenreAction artistAlbumWithGuestArtistsAction;
		public GenreAction ArtistAlbumWithGuestArtistsAction
		{
			get { return this.artistAlbumWithGuestArtistsAction; }
			set { this.Set( nameof(this.ArtistAlbumWithGuestArtistsAction), ref this.artistAlbumWithGuestArtistsAction, value ); }
		}

		private GenreAction artistAssortedAction;
		public GenreAction ArtistAssortedAction
		{
			get { return this.artistAssortedAction; }
			set { this.Set( nameof(this.ArtistAssortedAction), ref this.artistAssortedAction, value ); }
		}

		private GenreAction artistAlbumAction;
		public GenreAction ArtistAlbumAction
		{
			get { return this.artistAlbumAction; }
			set { this.Set( nameof(this.ArtistAlbumAction), ref this.artistAlbumAction, value ); }
		}

		private GenreAction compilationAlbumAction;
		public GenreAction CompilationAlbumAction
		{
			get { return this.compilationAlbumAction; }
			set { this.Set( nameof(this.CompilationAlbumAction), ref this.compilationAlbumAction, value ); }
		}
	}

	public class ComparisonConverter : IValueConverter
	{
		public Object Convert( Object value, Type targetType, Object parameter, CultureInfo culture )
		{
			return value?.Equals( parameter );
		}

		public Object ConvertBack( Object value, Type targetType, Object parameter, CultureInfo culture )
		{
			return value?.Equals( true ) == true ? parameter : Binding.DoNothing;
		}
	}
}
