using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;

namespace TeslaTags.Gui
{
	public class MainViewModel : BaseViewModel, ITeslaTagEventsListener
	{
		private readonly ITeslaTagsService teslaTagsService;
		private readonly ITeslaTagUtilityService utilityService;

		public MainViewModel(ITeslaTagsService teslaTagsService, ITeslaTagUtilityService utilityService)
		{
			this.teslaTagsService = teslaTagsService;
			this.teslaTagsService.EventsListener = new DispatchTeslaTagEventsListener( this );

			this.utilityService = utilityService;

			this.StartCommand = this.CreateBusyCommand( this.Start );
			this.StopCommand  = this.CreateBusyCommand( this.Stop, enabledWhenBusy: true );

			///////////
			
			this.DirectoryPath     = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
			this.OnlyValidate      = true;

			//////////

			if( this.IsInDesignMode )
			{
				for( Int32 i = 0; i < 10; i++ )
				{
					this.DirectoriesProgress.Add( new DirectoryViewModel( utilityService, @"C:\TestData\Folder" + i, @"C:\TestData" ) );
				}
				this.SelectedDirectory = this.DirectoriesProgress[2];

				String dir = this.SelectedDirectory.FullDirectoryPath;
				this.SelectedDirectory.Messages.Add( new Message( MessageSeverity.Error           , dir, Path.Combine( dir, "File.mp3" ), "Message text" ) );
				this.SelectedDirectory.Messages.Add( new Message( MessageSeverity.FileModification, dir, Path.Combine( dir, "File.mp3" ), "Message text" ) );
				this.SelectedDirectory.Messages.Add( new Message( MessageSeverity.Info            , dir, Path.Combine( dir, "File.mp3" ), "Message text" ) );
				this.SelectedDirectory.Messages.Add( new Message( MessageSeverity.Warning         , dir, Path.Combine( dir, "File.mp3" ), "Message text" ) );
			}
		}

		#region Two-way

		private String directoryPath;
		public String DirectoryPath
		{
			get { return this.directoryPath; }
			set { this.Set( nameof(this.DirectoryPath), ref this.directoryPath, value ); }
		}

		private Boolean onlyValidate;
		public Boolean OnlyValidate
		{
			get { return this.onlyValidate; }
			set { this.Set( nameof(this.OnlyValidate), ref this.onlyValidate, value ); }
		}

		public GenreRulesViewModel GenreRules { get; } = new GenreRulesViewModel();

		private readonly Dictionary<String,DirectoryViewModel> viewModelDict = new Dictionary<String,DirectoryViewModel>( StringComparer.OrdinalIgnoreCase );

		public ObservableCollection<DirectoryViewModel> DirectoriesProgress { get; } = new ObservableCollection<DirectoryViewModel>();

		private DirectoryViewModel selectedDirectory;
		public DirectoryViewModel SelectedDirectory
		{
			get { return this.selectedDirectory; }
			set { this.Set( nameof(this.SelectedDirectory), ref this.selectedDirectory, value ); }
		}

		#endregion

		#region One-way from ViewModel

		public Boolean IsValid
		{
			get
			{
				try
				{
					return !String.IsNullOrWhiteSpace( this.DirectoryPath ) && Directory.Exists( this.DirectoryPath );
				}
				catch
				{
					return false;
				}
			}
		}

		private Single progressPerc;
		public Single ProgressPerc
		{
			get { return this.progressPerc; }
			set {
				this.Set( nameof(this.ProgressPerc), ref this.progressPerc, value );
				Boolean indeterminate = value == -1;
				if( indeterminate != this.ProgressIndeterminate )
				{
					this.ProgressIndeterminate = indeterminate;
					this.RaisePropertyChanged( nameof(this.ProgressIndeterminate) );
				}
			}
		}
		public Boolean ProgressIndeterminate { get; private set; }

		#endregion

		#region Commands

		public RelayCommand StartCommand { get; }

		public RelayCommand StopCommand { get; }

		private void Start()
		{
			this.IsBusy = this.teslaTagsService.IsBusy;
			this.ProgressPerc = -1;
			this.teslaTagsService.Start( this.DirectoryPath, this.OnlyValidate, this.GenreRules.GetRules() );
		}

		private void Stop()
		{
			this.teslaTagsService.Stop();
		}

		#endregion

		#region ITeslaTagEvents

		private DirectoryViewModel GetDirectoryProgressViewModel( String directory )
		{
			DirectoryViewModel dirVM;
			if( !this.viewModelDict.TryGetValue( directory, out dirVM ) )
			{
				throw new InvalidOperationException( "Event raised in previously unreported directory: " + directory );
			}
			return dirVM;
		}

		void ITeslaTagEventsListener.Started()
		{
		}

		void ITeslaTagEventsListener.GotDirectories(List<String> directories)
		{
			if( App.ExcludeITunes )
			{
				for( Int32 i = 0; i < directories.Count; i++ )
				{
					String d = directories[i];
					if( d.IndexOf( "iTunes", StringComparison.OrdinalIgnoreCase ) > -1 )
					{
						directories[i] = null;
					}
				}
			}

			this.viewModelDict.Clear();
			this.DirectoriesProgress.Clear();
			foreach( String directoryPath in directories )
			{
				if( directoryPath == null ) continue;

				DirectoryViewModel dirVM = new DirectoryViewModel( this.utilityService, directoryPath, prefix: this.DirectoryPath );
				this.viewModelDict.Add( directoryPath, dirVM );
				this.DirectoriesProgress.Add( dirVM );
			}
		}

		void ITeslaTagEventsListener.DirectoryUpdate(String directory, FolderType folderType, Int32 modifiedCount, Int32 totalCount, Single totalPerc, List<Message> messages)
		{
			DirectoryViewModel dirVM = this.GetDirectoryProgressViewModel( directory );
			dirVM.FilesModified = modifiedCount;
			dirVM.FolderType    = folderType;
			dirVM.TotalFiles    = totalCount;

			foreach( Message message in messages ) dirVM.Messages.Add( message );

			this.ProgressPerc = totalPerc;
		}

		void ITeslaTagEventsListener.Complete(Boolean stoppedEarly)
		{
			this.IsBusy = this.teslaTagsService.IsBusy;
			if( !stoppedEarly ) this.ProgressPerc = 0;
		}

		#endregion
	}

	internal static class Extensions
	{
		public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
		{
			if( items != null )
			{
				foreach( T item in items ) collection.Add( item );
			}
		}
	}

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