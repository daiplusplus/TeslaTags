using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using System.Threading.Tasks;
using GalaSoft.MvvmLight.CommandWpf;

namespace TeslaTags.Gui
{
	public class MainViewModel : BaseViewModel, ITeslaTagEventsListener
	{
		private readonly ITeslaTagsService teslaTagsService;
		private readonly ITeslaTagUtilityService utilityService;
		private readonly IConfigurationService configurationService;
		private readonly IWindowService windowService;

		public MainViewModel(ITeslaTagsService teslaTagsService, ITeslaTagUtilityService utilityService, IConfigurationService configurationService, IWindowService windowService)
		{
			this.teslaTagsService     = teslaTagsService;
			this.teslaTagsService.EventsListener = new DispatchTeslaTagEventsListener( this );
			this.utilityService       = utilityService;
			this.configurationService = configurationService;
			this.windowService        = windowService;

			this.WindowLoadedCommand  = new RelayCommand( this.WindowLoaded );
			this.WindowClosingCommand = new RelayCommand( this.WindowClosing );
			this.StartCommand         = this.CreateBusyCommand( this.Start ); // , additionalCanExecute: this.DirectoryPathIsValid ); // weird, why doesn't this work? my `DirectoryPathIsValid` function isn't invoked after editing the textbox.
			this.StopCommand          = this.CreateBusyCommand( this.Stop, enabledWhenBusy: true );

			///////////
			
			this.DirectoryPath     = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
			this.OnlyValidate      = true;

			this.Version = typeof(MainViewModel).Assembly.GetName().Version.ToString() + " (Release 4)";
			this.ReadmeLink = @"https://github.com/Jehoel/TeslaTags/blob/release-4/README.md";

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
			set
			{
				Boolean diff = this.Set( nameof(this.DirectoryPath), ref this.directoryPath, value );
				//if( diff ) this.StartCommand.RaiseCanExecuteChanged();
			}
		}

		private Boolean DirectoryPathIsValid()
		{
			return !String.IsNullOrWhiteSpace( this.DirectoryPath ) && Directory.Exists( this.DirectoryPath );
		}

		private Boolean onlyValidate;
		public Boolean OnlyValidate
		{
			get { return this.onlyValidate; }
			set { this.Set( nameof(this.OnlyValidate), ref this.onlyValidate, value ); }
		}

		private String excludeLines;
		public String ExcludeLines
		{
			get { return this.excludeLines; }
			set { this.Set( nameof(this.ExcludeLines), ref this.excludeLines, value ); }
		}

		private Boolean restoreFiles;
		public Boolean RestoreFiles
		{
			get { return this.restoreFiles; }
			set { this.Set( nameof(this.RestoreFiles), ref this.restoreFiles, value ); }
		}

		private Boolean hideEmptyDirectories;
		public Boolean HideEmptyDirectories
		{
			get { return this.hideEmptyDirectories; }
			set { this.Set( nameof(this.HideEmptyDirectories), ref this.hideEmptyDirectories, value ); }
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

		public String Version { get; }
		public String ReadmeLink { get; }

		#endregion

		#region Commands

		public RelayCommand WindowLoadedCommand { get; }

		public RelayCommand WindowClosingCommand { get; }

		private void WindowLoaded()
		{
			this.LoadConfig();
		}

		private void WindowClosing()
		{
			this.SaveConfig();
		}

		private void LoadConfig()
		{
			Config config = this.configurationService.Config;

			if( !String.IsNullOrWhiteSpace( config.RootDirectory ) && Directory.Exists( config.RootDirectory ) )
			{
				this.DirectoryPath        = config.RootDirectory;
			}
			
			this.HideEmptyDirectories = config.HideEmptyDirectories;
			this.ExcludeLines         = String.Join( "\r\n", config.ExcludeList ?? new String[0] );
			
			if( config.GenreRules != null )
			{
				this.GenreRules.LoadFrom( config.GenreRules );
			}

			var window = this.windowService.GetWindowByDataContext( this );
			if( window != null )
			{
				var pos = config.RestoredWindowPosition;

				if( pos.Width > 0 && pos.Height > 0 )
				{
					window.Top    = pos.Y;
					window.Left   = pos.X;
					window.Width  = pos.Width;
					window.Height = pos.Height;
				}

				if( config.IsMaximized ) window.WindowState = System.Windows.WindowState.Maximized;
			}
		}

		private void SaveConfig()
		{
			Config config = this.configurationService.Config;

			config.RootDirectory        = this.DirectoryPath;
			config.HideEmptyDirectories = this.HideEmptyDirectories;
			config.ExcludeList          = this.ExcludeLines?.Split( new String[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries ) ?? new String[0];
			this.GenreRules.SaveTo( config.GenreRules );

			var window = this.windowService.GetWindowByDataContext( this );
			var rb = window.RestoreBounds;

			config.RestoredWindowPosition = ( X: (Int32)rb.Left, Y: (Int32)rb.Top, Width: (Int32)rb.Width, Height: (Int32)rb.Height );
			config.IsMaximized = window.WindowState == System.Windows.WindowState.Maximized;

			/////

			this.configurationService.SaveConfig( config );
		}

		public RelayCommand StartCommand { get; }

		public RelayCommand StopCommand { get; }

		private void Start()
		{
			if( !this.DirectoryPathIsValid() )
			{
				// TODO: Show error message?
				return;
			}

			this.IsBusy = this.teslaTagsService.IsBusy;
			this.ProgressPerc = -1;

			// Save config:
			this.SaveConfig();

			// Start for real:

			this.teslaTagsService.Start( this.DirectoryPath, this.OnlyValidate, this.RestoreFiles, this.GenreRules.GetRules() );
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
			if( !String.IsNullOrWhiteSpace( this.ExcludeLines ) )
			{
				String[] excl = this.ExcludeLines.Split( new String[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries );

				for( Int32 i = 0; i < directories.Count; i++ )
				{
					String d = directories[i];
					if( excl.Any( e => d.IndexOf( e, StringComparison.OrdinalIgnoreCase ) > -1 ) )
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

	internal static partial class Extensions
	{
		public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
		{
			if( items != null )
			{
				foreach( T item in items ) collection.Add( item );
			}
		}
	}
}