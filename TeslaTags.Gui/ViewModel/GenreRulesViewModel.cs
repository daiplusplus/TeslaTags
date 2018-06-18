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
				Default                  = this.rules.Default,
				AssortedFiles            = this.rules.AssortedFiles,
				CompilationUseArtistName = this.rules.CompilationUseArtistName,
				GuestArtistUseArtistName = this.rules.GuestArtistUseArtistName
			};
		}

		public Boolean DefaultPreserve
		{
			get { return this.rules.Default == GenreDefault.Preserve; }
			set
			{
				Boolean defaultPreserveNew = value;
				Boolean defaultPreserveOld = this.DefaultPreserve;

				if( defaultPreserveNew != defaultPreserveOld )
				{
					if( defaultPreserveNew )
					{
						this.rules.Default = GenreDefault.Preserve;
					}
					else
					{
						this.rules.Default = GenreDefault.Clear;
					}
					
					this.RaisePropertyChanged(nameof(this.DefaultPreserve));
					this.RaisePropertyChanged(nameof(this.DefaultClear));
					this.RaisePropertyChanged(nameof(this.DefaultUseArtist));
				}
			}
		}

		public Boolean DefaultClear
		{
			get { return this.rules.Default == GenreDefault.Clear; }
			set
			{
				Boolean defaultClearNew = value;
				Boolean defaultClearOld = this.DefaultClear;

				if( defaultClearNew != defaultClearOld )
				{
					if( defaultClearNew )
					{
						this.rules.Default = GenreDefault.Clear;
					}
					else
					{
						this.rules.Default = GenreDefault.Preserve;
					}
					
					this.RaisePropertyChanged(nameof(this.DefaultPreserve));
					this.RaisePropertyChanged(nameof(this.DefaultClear));
					this.RaisePropertyChanged(nameof(this.DefaultUseArtist));
				}
			}
		}

		public Boolean DefaultUseArtist
		{
			get { return this.rules.Default == GenreDefault.UseArtist; }
			set
			{
				Boolean defaultUseArtistNew = value;
				Boolean defaultUseArtistOld = this.DefaultUseArtist;

				if( defaultUseArtistNew != defaultUseArtistOld )
				{
					if( defaultUseArtistNew )
					{
						this.rules.Default = GenreDefault.UseArtist;
					}
					else
					{
						this.rules.Default = GenreDefault.Preserve;
					}
					
					this.RaisePropertyChanged(nameof(this.DefaultPreserve));
					this.RaisePropertyChanged(nameof(this.DefaultClear));
					this.RaisePropertyChanged(nameof(this.DefaultUseArtist));
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
