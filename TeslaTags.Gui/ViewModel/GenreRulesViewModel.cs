using System;

namespace TeslaTags.Gui
{
	public class GenreRulesViewModel : BaseViewModel
	{
		private readonly GenreRules rules = new GenreRules();

		/// <summary>Creates a copy of the underlying rules model object.</summary>
		public GenreRules GetRules()
		{
			return new GenreRules()
			{
				DefaultClear             = this.rules.DefaultClear,
				AssortedFiles            = this.rules.AssortedFiles,
				CompilationUseArtistName = this.rules.CompilationUseArtistName,
				GuestArtistUseArtistName = this.rules.GuestArtistUseArtistName
			};
		}

		public Boolean DefaultPreserve
		{
			get { return this.rules.DefaultPreserve; }
			set
			{
				Boolean defaultClear = !value;
				if( defaultClear != this.rules.DefaultClear )
				{
					this.rules.DefaultClear = defaultClear;
					
					this.RaisePropertyChanged(nameof(this.DefaultPreserve));
					this.RaisePropertyChanged(nameof(this.DefaultClear));
				}
			}
		}

		public Boolean DefaultClear
		{
			get { return this.rules.DefaultClear; }
			set
			{
				if( value != this.rules.DefaultClear )
				{
					this.rules.DefaultClear = value;
					
					this.RaisePropertyChanged(nameof(this.DefaultPreserve));
					this.RaisePropertyChanged(nameof(this.DefaultClear));
				}
			}
		}

		public Boolean AssortedUseDefault
		{
			get { return this.rules.AssortedFiles == GenreAssortedFiles.UseDefault; }
			set
			{
				Boolean useDefaultNew = value;
				Boolean useDefaultOld = this.AssortedUseDefault;
				if( useDefaultNew != useDefaultOld )
				{
					if( useDefaultNew )
					{
						this.rules.AssortedFiles = GenreAssortedFiles.UseDefault;
					}
					else
					{
						// NOOP? What does it mean to set this property to false?
						this.rules.AssortedFiles = GenreAssortedFiles.UseFolderName;
					}
					
					this.RaisePropertyChanged(nameof(this.AssortedUseDefault));
					this.RaisePropertyChanged(nameof(this.AssortedUseFolderName));
					this.RaisePropertyChanged(nameof(this.AssortedUseArtistName));
				}
			}
		}

		public Boolean AssortedUseFolderName
		{
			get { return this.rules.AssortedFiles == GenreAssortedFiles.UseFolderName; }
			set
			{
				Boolean useFolderNameNew = value;
				Boolean useFolderNameOld = this.AssortedUseFolderName;
				if( useFolderNameNew != useFolderNameOld )
				{
					if( useFolderNameNew )
					{
						this.rules.AssortedFiles = GenreAssortedFiles.UseFolderName;
					}
					else
					{
						this.rules.AssortedFiles = GenreAssortedFiles.UseDefault;
					}
					
					this.RaisePropertyChanged(nameof(this.AssortedUseDefault));
					this.RaisePropertyChanged(nameof(this.AssortedUseFolderName));
					this.RaisePropertyChanged(nameof(this.AssortedUseArtistName));
				}
			}
		}

		public Boolean AssortedUseArtistName
		{
			get { return this.rules.AssortedFiles == GenreAssortedFiles.UseArtistName; }
			set
			{
				Boolean useArtistNameNew = value;
				Boolean useArtistNameOld = this.AssortedUseArtistName;
				if( useArtistNameNew != useArtistNameOld )
				{
					if( useArtistNameNew )
					{
						this.rules.AssortedFiles = GenreAssortedFiles.UseArtistName;
					}
					else
					{
						this.rules.AssortedFiles = GenreAssortedFiles.UseDefault;
					}
					
					this.RaisePropertyChanged(nameof(this.AssortedUseDefault));
					this.RaisePropertyChanged(nameof(this.AssortedUseFolderName));
					this.RaisePropertyChanged(nameof(this.AssortedUseArtistName));
				}
			}
		}

		public Boolean CompilationUseDefault
		{
			get { return this.rules.CompilationUseDefault; }
			set
			{
				Boolean useArtistNameNew = !value;
				Boolean useArtistNameOld = this.CompilationUseArtistName;
				if( useArtistNameNew != useArtistNameOld )
				{
					this.rules.CompilationUseArtistName = useArtistNameNew;
					
					this.RaisePropertyChanged(nameof(this.CompilationUseDefault));
					this.RaisePropertyChanged(nameof(this.CompilationUseArtistName));
				}
			}
		}

		public Boolean CompilationUseArtistName
		{
			get { return this.rules.CompilationUseArtistName; }
			set
			{
				Boolean useArtistNameNew = value;
				Boolean useArtistNameOld = this.CompilationUseArtistName;
				if( useArtistNameNew != useArtistNameOld )
				{
					this.rules.CompilationUseArtistName = useArtistNameNew;
					
					this.RaisePropertyChanged(nameof(this.CompilationUseDefault));
					this.RaisePropertyChanged(nameof(this.CompilationUseArtistName));
				}
			}
		}

		public Boolean GuestArtistUseDefault
		{
			get { return this.rules.GuestArtistUseDefault; }
			set
			{
				Boolean useArtistNameNew = !value;
				Boolean useArtistNameOld = this.GuestArtistUseArtistName;
				if( useArtistNameNew != useArtistNameOld )
				{
					this.rules.GuestArtistUseArtistName = useArtistNameNew;
					
					this.RaisePropertyChanged(nameof(this.GuestArtistUseDefault));
					this.RaisePropertyChanged(nameof(this.GuestArtistUseArtistName));
				}
			}
		}

		public Boolean GuestArtistUseArtistName
		{
			get { return this.rules.GuestArtistUseArtistName; }
			set
			{
				Boolean useArtistNameNew = value;
				Boolean useArtistNameOld = this.GuestArtistUseArtistName;
				if( useArtistNameNew != useArtistNameOld )
				{
					this.rules.GuestArtistUseArtistName = useArtistNameNew;
					
					this.RaisePropertyChanged(nameof(this.GuestArtistUseDefault));
					this.RaisePropertyChanged(nameof(this.GuestArtistUseArtistName));
				}
			}
		}
	}
}
