using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.CommandWpf;

namespace TeslaTags.Gui
{
	public class MainViewModel : BaseViewModel
	{
		private readonly ITeslaTagsService     teslaTagsService;
		private readonly IConfigurationService configurationService;
		private readonly IWindowService        windowService;

		//private readonly DispatchTeslaTagEventsListener teslaTagsListener;

		private CancellationTokenSource teslaTagsCts;

		public MainViewModel(ITeslaTagsService teslaTagsService, IConfigurationService configurationService, IWindowService windowService)
		{
			this.teslaTagsService     = teslaTagsService;
			this.configurationService = configurationService;
			this.windowService        = windowService;

			//this.teslaTagsListener    = new DispatchTeslaTagEventsListener( this );

			this.WindowLoadedCommand  = new RelayCommand( this.WindowLoaded );
			this.WindowClosingCommand = new RelayCommand( this.WindowClosing );
			this.StartCommand         = this.CreateBusyCommand( this.Start ); // , additionalCanExecute: this.DirectoryPathIsValid ); // weird, why doesn't this work? my `DirectoryPathIsValid` function isn't invoked after editing the textbox.
			this.StopCommand          = this.CreateBusyCommand( this.Stop, enabledWhenBusy: true );

			///////////
			
			this.directoryPath     = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
			this.onlyValidate      = true;

			this.Version = typeof(MainViewModel).Assembly.GetName().Version.ToString() + " (Release 6)";
			this.ReadmeLink = @"https://github.com/Jehoel/TeslaTags/blob/release-4/README.md";

			//////////

			if( this.IsInDesignMode )
			{
				for( Int32 i = 0; i < 10; i++ )
				{
					this.DirectoriesProgress.Add( new DirectoryViewModel( teslaTagsService, @"C:\TestData\Folder" + i, @"C:\TestData" ) );
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

		private Boolean hideBoringDirectories;
		public Boolean HideBoringDirectories
		{
			get { return this.hideBoringDirectories; }
			set { this.Set( nameof(this.HideBoringDirectories), ref this.hideBoringDirectories, value ); }
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

		private Single progressPerc;
		public Single ProgressPerc
		{
			get { return this.progressPerc; }
			set { this.Set( nameof(this.ProgressPerc), ref this.progressPerc, value ); }
		}

		private ProgressState progressStatus;
		public ProgressState ProgressStatus
		{
			get { return this.progressStatus; }
			set { this.Set( nameof(this.ProgressStatus), ref this.progressStatus, value ); }
		}

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
				this.DirectoryPath = config.RootDirectory;
			}
			
			this.HideBoringDirectories = config.HideEmptyDirectories;
			this.ExcludeLines          = String.Join( "\r\n", config.ExcludeList ?? new String[0] );
			
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
			config.HideEmptyDirectories = this.HideBoringDirectories;
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

		private async void Start()
		{
			if( !this.DirectoryPathIsValid() )
			{
				this.windowService.ShowMessageBoxErrorDialog( this, title: "TeslaTags", message: "The music root directory is not specified, or does not exist." );
				return;
			}

			this.IsBusy = true;
			this.ProgressStatus = ProgressState.StartingIndeterminate;
			try
			{
				// Save config (so we can restore, in case it crashes during processing):
				this.SaveConfig();

				String[] directoryExcludes = this.ExcludeLines?.Split( new String[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries ).Select( s => s.Trim() ).ToArray() ?? new String[0];

				Boolean directoryFilter(String dirPath)
				{
					if( directoryExcludes.Any( exclSubPath => dirPath.IndexOf( exclSubPath, StringComparison.OrdinalIgnoreCase ) > -1 ) ) return false;
					return true;
				}

				RetaggingOptions opts = new RetaggingOptions( this.DirectoryPath, this.OnlyValidate, this.RestoreFiles, directoryFilter, this.GenreRules.GetRules() );
			
				Single total = 0;
				Single progress = 0;

				Progress<IReadOnlyList<String>> directoriesReceiver = new Progress<IReadOnlyList<String>>( directories =>
				{
					this.viewModelDict.Clear();
					this.DirectoriesProgress.Clear();
					total = directories.Count;

					foreach( String directoryPath in directories )
					{
						if( directoryPath == null ) continue;

						DirectoryViewModel dirVM = new DirectoryViewModel( this.teslaTagsService, directoryPath, prefix: this.DirectoryPath );
						this.viewModelDict.Add( directoryPath, dirVM );
						this.DirectoriesProgress.Add( dirVM );
					}

					this.ProgressStatus = ProgressState.Running;
				} );

				Progress<DirectoryResult> directoryReceiver = new Progress<DirectoryResult>( result =>
				{
					DirectoryViewModel dirVM;
					if( !this.viewModelDict.TryGetValue( result.DirectoryPath, out dirVM ) )
					{
						throw new InvalidOperationException( "Event raised in previously unreported directory: " + result.DirectoryPath );
					}

					dirVM.FilesModifiedActual   = result.ActualModifiedFiles;
					dirVM.FilesModifiedProposed = result.ProposedModifiedFiles;
					dirVM.FolderType    = result.FolderType;
					dirVM.TotalFiles    = result.TotalFiles;

					foreach( Message message in result.Messages ) dirVM.Messages.Add( message );

					progress++;

					this.ProgressPerc = progress / total;
				} );

				// Start for real:
				CancellationTokenSource cts = this.teslaTagsCts = new CancellationTokenSource();

				Task task = this.teslaTagsService.StartRetaggingAsync( opts, directoriesReceiver, directoryReceiver, cts.Token );
				await task;

				if( task.IsCanceled )
				{
					this.ProgressStatus = ProgressState.Canceled;
				}
				else
				{
					this.ProgressStatus = ProgressState.Completed;
				}
			}
			catch( OperationCanceledException ) // includes TaskCanceledException
			{
				this.ProgressStatus = ProgressState.Canceled;
			}
			catch
			{
				this.ProgressStatus = ProgressState.Error;
				throw;
			}
			finally
			{
				this.IsBusy = false;
				this.teslaTagsCts = null;
			}
		}

		private void Stop()
		{
			if( this.teslaTagsCts == null ) throw new InvalidOperationException( "CancellationTokenSource is null." );

			this.teslaTagsCts.Cancel();
		}

		#endregion
	}

	public enum ProgressState
	{
		NotStarted,
		StartingIndeterminate,
		Running,
		Completed,
		Canceled,
		Error
	}
}