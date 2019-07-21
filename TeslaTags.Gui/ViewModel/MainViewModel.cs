using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using GalaSoft.MvvmLight.CommandWpf;

namespace TeslaTags.Gui
{
	public partial class MainViewModel : BaseViewModel, ILiveConfigurationService
	{
		private readonly ITeslaTagsService     teslaTagsService;
		private readonly IConfigurationService configurationService;
		private readonly IWindowService        windowService;

		private CancellationTokenSource teslaTagsCts;

		public MainViewModel(ITeslaTagsService teslaTagsService, IConfigurationService configurationService, IWindowService windowService)
		{
			this.teslaTagsService     = teslaTagsService;
			this.configurationService = configurationService;
			this.windowService        = windowService;

			this.WindowLoadedCommand  = new RelayCommand( this.WindowLoaded );
			this.WindowClosingCommand = new RelayCommand( this.WindowClosing );
			this.StartCommand         = this.CreateBusyCommand( this.Start ); // , additionalCanExecute: this.DirectoryPathIsValid ); // weird, why doesn't this work? my `DirectoryPathIsValid` function isn't invoked after editing the textbox.
			this.StopCommand          = this.CreateBusyCommand( this.Stop, enabledWhenBusy: true );

			///////////
			
			this.directoryPath        = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
			this.onlyValidate         = true;

			this.Version = typeof(MainViewModel).Assembly.GetName().Version.ToString() + " (Release 6)";
			this.ReadmeLink = @"https://github.com/Jehoel/TeslaTags/blob/release-4/README.md";

			//////////

			if( this.IsInDesignMode )
			{
				for( Int32 i = 0; i < 10; i++ )
				{
					this.DirectoriesProgress.Add( new DirectoryViewModel( teslaTagsService, liveConfiguration: this, @"C:\TestData\Folder" + i, @"C:\TestData", imagesInFolder: Array.Empty<FileInfo>() ) );
				}
				this.SelectedDirectory = this.DirectoriesProgress[2];

				String dir = this.SelectedDirectory.FullDirectoryPath;
				this.SelectedDirectory.Messages.Add( new Message( MessageSeverity.Error           , dir, Path.Combine( dir, "File.mp3" ), "Message text" ) );
				this.SelectedDirectory.Messages.Add( new Message( MessageSeverity.FileModification, dir, Path.Combine( dir, "File.mp3" ), "Message text" ) );
				this.SelectedDirectory.Messages.Add( new Message( MessageSeverity.Info            , dir, Path.Combine( dir, "File.mp3" ), "Message text" ) );
				this.SelectedDirectory.Messages.Add( new Message( MessageSeverity.Warning         , dir, Path.Combine( dir, "File.mp3" ), "Message text" ) );
			}
		}

		private Boolean DirectoryPathIsValid()
		{
			return !String.IsNullOrWhiteSpace( this.DirectoryPath ) && Directory.Exists( this.DirectoryPath );
		}

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
			this.ExcludeLines          = String.Join( "\r\n", ( config.ExcludeList    ?? Array.Empty<String>() ).OrderBy( s => s ) );
			this.FileExtensionsToLoad  = String.Join( "\r\n", ( config.FileExtensions ?? Array.Empty<String>() ).OrderBy( s => s ) );

			if( config.GenreRules != null )
			{
				this.GenreRules.LoadFrom( config.GenreRules );
			}

			var window = this.windowService.GetWindowByDataContext( this );
			if( window != null )
			{
				var pos = config.RestoredWindowPosition; // `#pragma warning disable 42` does not suppress the IDE0042 message here.

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
			config.ExcludeList          = this.ExcludeLines        .Split( new String[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries ) ?? Array.Empty<String>();
			config.FileExtensions       = this.FileExtensionsToLoad.Split( new String[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries ) ?? Array.Empty<String>();
			config.GenreRules           = this.GenreRules.GetRules();

			var window = this.windowService.GetWindowByDataContext( this );
			var rb = window.RestoreBounds;

			config.RestoredWindowPosition = ( X: (Int32)rb.Left, Y: (Int32)rb.Top, Width: (Int32)rb.Width, Height: (Int32)rb.Height );
			config.IsMaximized = window.WindowState == System.Windows.WindowState.Maximized;

			/////

			this.configurationService.SaveConfig( config );
		}

		public FileSystemPredicate CreateFileSystemPredicate()
		{
			List<String> directoryExclusions = this.ExcludeLines
					.Split( new String[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries )
					.Select( line => line.Trim( ' ', '\\', '/' ) )
					.ToList();

			IDirectoryPredicate directoryFilterPredicate = ( directoryExclusions.Count == 0 ) ? (IDirectoryPredicate)new EmptyDirectoryPredicate() : new ExactPathComponentMatchPredicate( directoryExclusions );

			IEnumerable<String> extensions = this.FileExtensionsToLoad
				.Split( new String[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries ); // `FileSystemPredicate` sanitizes the extensions and is case-insensitive too.

			FileSystemPredicate fsp = new FileSystemPredicate( directoryFilterPredicate, extensions );
			return fsp;
		}

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

				FileSystemPredicate fsp = this.CreateFileSystemPredicate();

				RetaggingOptions opts = new RetaggingOptions( this.DirectoryPath, this.OnlyValidate, this.RestoreFiles, fsp, this.GenreRules.GetRules() );
			
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

						// HACK: Get it so this list is loaded on-demand instead of eagerly:
						DirectoryInfo dir = new DirectoryInfo( directoryPath );

						List<FileInfo> imageFiles = dir.GetFiles()
							.Where( fi => DirectoryViewModel.AlbumArtFileExtensions.Contains( fi.Extension ) )
							.OrderBy( fi => fi.FullName )
							.ToList();

						DirectoryViewModel dirVM = new DirectoryViewModel( this.teslaTagsService, liveConfiguration: this, directoryPath, prefix: this.DirectoryPath, imageFiles );
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
					dirVM.FolderType            = result.FolderType;
					dirVM.TotalFiles            = result.TotalFiles;

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