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

		public MainViewModel(ITeslaTagsService teslaTagsService, ITeslaTagUtilityService utilityService, IConfigurationService configurationService, IWindowService windowService)
		{
			this.teslaTagsService = teslaTagsService;
			this.teslaTagsService.EventsListener = new DispatchTeslaTagEventsListener( this );

			this.utilityService = utilityService;
			this.configurationService = configurationService;

			this.WindowLoadedCommand = new RelayCommand( this.WindowLoaded );
			this.StartCommand = this.CreateBusyCommand( this.Start );
			this.StopCommand  = this.CreateBusyCommand( this.Stop, enabledWhenBusy: true );

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
			set { this.Set( nameof(this.DirectoryPath), ref this.directoryPath, value ); }
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

		public void WindowLoaded()
		{
			this.LoadConfig( this.configurationService.Config );
		}

		private void LoadConfig( Config config )
		{
			this.DirectoryPath        = config.RootDirectory;
			this.HideEmptyDirectories = config.HideEmptyDirectories;
			this.ExcludeLines         = String.Join( "\r\n", config.ExcludeList );
			this.GenreRules.LoadFrom( config.GenreRules );
		}

		public RelayCommand StartCommand { get; }

		public RelayCommand StopCommand { get; }

		private void Start()
		{
			this.IsBusy = this.teslaTagsService.IsBusy;
			this.ProgressPerc = -1;

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