using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;

namespace TeslaTags.Gui
{
	public abstract class BaseViewModel : ViewModelBase
	{
		protected List<RelayCommand> BusyDisabledCommands { get; } = new List<RelayCommand>();

		private Boolean isBusy;
		public Boolean IsBusy
		{
			get { return this.isBusy; }
			set {
				this.Set( nameof(this.IsBusy), ref this.isBusy, value );
				this.RaisePropertyChanged( nameof(this.IsNotBusy) );

				foreach( RelayCommand cmd in this.BusyDisabledCommands ) cmd.RaiseCanExecuteChanged();
			}
		}
		public Boolean IsNotBusy => !this.IsBusy;

		protected Boolean CanExecuteWhenNotBusy()
		{
			return !this.IsBusy;
		}
	}

	public class MainViewModel : BaseViewModel, ITeslaTagEventsListener
	{
		private readonly ITeslaTagsService teslaTagsService;
		private readonly ITeslaTagUtilityService utilityService;

		public MainViewModel(ITeslaTagsService teslaTagsService, ITeslaTagUtilityService utilityService)
		{
			this.teslaTagsService = teslaTagsService;
			this.teslaTagsService.EventsListener = new DispatchTeslaTagEventsListener( this );

			this.utilityService = utilityService;

			this.StartCommand = new RelayCommand( this.Start, canExecute: () => !this.teslaTagsService.IsBusy );
			this.StopCommand  = new RelayCommand( this.Stop , canExecute: () =>  this.teslaTagsService.IsBusy );

			this.BusyDisabledCommands.Add( this.StartCommand );
			this.BusyDisabledCommands.Add( this.StopCommand );

			this.OnlyValidate = true;

			this.DirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

			if( this.IsInDesignMode )
			{
				for( Int32 i = 0; i < 10; i++ )
				{
					this.DirectoriesProgress.Add( new DirectoryProgressViewModel( utilityService, @"C:\TestData\Folder" + i, @"C:\TestData" ) );
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

		private readonly Dictionary<String,DirectoryProgressViewModel> viewModelDict = new Dictionary<String,DirectoryProgressViewModel>( StringComparer.OrdinalIgnoreCase );

		public ObservableCollection<DirectoryProgressViewModel> DirectoriesProgress { get; } = new ObservableCollection<DirectoryProgressViewModel>();

		private DirectoryProgressViewModel selectedDirectory;
		public DirectoryProgressViewModel SelectedDirectory
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
			this.teslaTagsService.Start( this.DirectoryPath, this.OnlyValidate );
			this.ProgressPerc = -1;
			this.IsBusy = this.teslaTagsService.IsBusy;
		}

		private void Stop()
		{
			this.teslaTagsService.Stop();
		}

		#endregion

		#region ITeslaTagEvents

		private DirectoryProgressViewModel GetDirectoryProgressViewModel( String directory )
		{
			DirectoryProgressViewModel dirVM;
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

				DirectoryProgressViewModel dirVM = new DirectoryProgressViewModel( this.utilityService, directoryPath, prefix: this.DirectoryPath );
				this.viewModelDict.Add( directoryPath, dirVM );
				this.DirectoriesProgress.Add( dirVM );
			}
		}

		void ITeslaTagEventsListener.DirectoryUpdate(String directory, FolderType folderType, Int32 modifiedCount, Int32 totalCount, Single totalPerc, List<Message> messages)
		{
			DirectoryProgressViewModel dirVM = this.GetDirectoryProgressViewModel( directory );
			dirVM.FilesModified = modifiedCount;
			dirVM.FolderType    = folderType;
			dirVM.TotalFiles    = totalCount;

			foreach( Message message in messages ) dirVM.Messages.Add( message );

			this.ProgressPerc = totalPerc;
		}

		void ITeslaTagEventsListener.Complete(Boolean stoppedEarly)
		{
			this.IsBusy = this.teslaTagsService.IsBusy;
			if( !stoppedEarly ) this.ProgressPerc = 1;
		}

		#endregion
	}

	public class DirectoryProgressViewModel : BaseViewModel
	{
		private readonly ITeslaTagUtilityService utilityService;

		public DirectoryProgressViewModel(ITeslaTagUtilityService utilityService, String directoryPath, String prefix)
		{
			this.utilityService = utilityService;

			this.FullDirectoryPath    = directoryPath;
			this.DisplayDirectoryPath = directoryPath.StartsWith( prefix, StringComparison.OrdinalIgnoreCase ) ? directoryPath.Substring( prefix.Length ) : directoryPath;

			this.Messages.CollectionChanged += this.Messages_CollectionChanged;

			this.OpenFolderCommand = new RelayCommand( this.OpenFolder );
			this.ApplyAlbumArtCommand   = new RelayCommand( this.ApplyAlbumArt  , this.CanExecuteWhenNotBusy );
			this.RemoveApeTagsCommand   = new RelayCommand( this.RemoveApeTags  , this.CanExecuteWhenNotBusy );
			this.SetTrackNumbersCommand = new RelayCommand( this.SetTrackNumbers, this.CanExecuteWhenNotBusy );

			this.BusyDisabledCommands.Add( this.ApplyAlbumArtCommand );
			this.BusyDisabledCommands.Add( this.RemoveApeTagsCommand );
			this.BusyDisabledCommands.Add( this.SetTrackNumbersCommand );

			IEnumerable<String> imageFiles = Enumerable
				.Empty<String>()
				.Concat( Directory.GetFiles( this.FullDirectoryPath, "*.jpg" ) )
				.Concat( Directory.GetFiles( this.FullDirectoryPath, "*.jpeg" ) )
				.Concat( Directory.GetFiles( this.FullDirectoryPath, "*.png" ) )
				.Concat( Directory.GetFiles( this.FullDirectoryPath, "*.bmp" ) )
				.Concat( Directory.GetFiles( this.FullDirectoryPath, "*.gif" ) )
				.Select( fn => Path.GetFileName( fn ) )
				.OrderBy( fn => fn );

			foreach( String fn in imageFiles )
			{
				this.ImagesInFolder.Add( fn );
			}
		}

		#region Messages

		private void Messages_CollectionChanged(Object sender, NotifyCollectionChangedEventArgs e)
		{
			this.RaisePropertyChanged( nameof(this.InfoCount) );
			this.RaisePropertyChanged( nameof(this.WarnCount) );
			this.RaisePropertyChanged( nameof(this.ErrorCount) );
		}

		public ObservableCollection<Message> Messages { get; } = new ObservableCollection<Message>();

		public Int32 InfoCount  => this.Messages.Count( m => m.Severity == MessageSeverity.Info );
		public Int32 WarnCount  => this.Messages.Count( m => m.Severity == MessageSeverity.Warning );
		public Int32 ErrorCount => this.Messages.Count( m => m.Severity == MessageSeverity.Error );

		#endregion

		public String FullDirectoryPath { get; }
		public String DisplayDirectoryPath { get; }
		
		private Int32? filesModified;
		public Int32? FilesModified
		{
			get { return this.filesModified; }
			set { this.Set( nameof(this.FilesModified), ref this.filesModified, value ); }
		}

		private Int32? totalFiles;
		public Int32? TotalFiles
		{
			get { return this.totalFiles; }
			set { this.Set( nameof(this.TotalFiles), ref this.totalFiles, value ); }
		}

		private FolderType? folderType;
		public FolderType? FolderType
		{
			get { return this.folderType; }
			set { this.Set( nameof(this.FolderType), ref this.folderType, value ); }
		}
		
		public RelayCommand OpenFolderCommand { get; }
		public RelayCommand ApplyAlbumArtCommand { get; }
		public RelayCommand RemoveApeTagsCommand { get; }
		public RelayCommand SetTrackNumbersCommand { get; }

		public void OpenFolder()
		{
			using( System.Diagnostics.Process.Start( this.FullDirectoryPath ) ) { } // `Process.Start()` returns null if it's handled by an existing process, e.g. explorer.exe
		}

		#region Apply Album Art

		private String selectedImageFileName;
		public String SelectedImageFileName
		{
			get { return this.selectedImageFileName; }
			set { this.Set( nameof(this.SelectedImageFileName), ref this.selectedImageFileName, value ); }
		}

		private String albumArtMessage;
		public String AlbumArtMessage
		{
			get { return this.albumArtMessage; }
			set { this.Set( nameof(this.AlbumArtMessage), ref this.albumArtMessage, value ); }
		}

		private Boolean replaceAllAlbumArt;
		public Boolean ReplaceAllAlbumArt
		{
			get { return this.replaceAllAlbumArt; }
			set { this.Set( nameof(this.ReplaceAllAlbumArt), ref this.replaceAllAlbumArt, value ); }
		}

		public ObservableCollection<String> ImagesInFolder { get; } = new ObservableCollection<String>();

		public async void ApplyAlbumArt()
		{
			if( String.IsNullOrWhiteSpace( this.SelectedImageFileName ) )
			{
				this.AlbumArtMessage = "No file specified.";
				return;
			}

			String fileName = Path.IsPathRooted( this.SelectedImageFileName ) ? this.SelectedImageFileName : Path.Combine( this.FullDirectoryPath, this.SelectedImageFileName );

			if( !File.Exists( fileName ) )
			{
				this.AlbumArtMessage = "File \"" + this.SelectedImageFileName + "\" does not exist.";
				return;
			}

			this.IsBusy = true;

			List<Message> messages = await this.utilityService.SetAlbumArtAsync( this.FullDirectoryPath, this.SelectedImageFileName, this.ReplaceAllAlbumArt ? AlbumArtSetMode.Replace : AlbumArtSetMode.AddIfMissing );
			this.Messages.AddRange( messages );

			this.IsBusy = false;
		}

		#endregion

		public async void RemoveApeTags()
		{
			this.IsBusy = true;

			List<Message> messages = await this.utilityService.RemoveApeTagsAsync( this.FullDirectoryPath );
			this.Messages.AddRange( messages );

			this.IsBusy = false;
		}

		#region Track numbers

		private Int32 trackNumberOffset;
		public Int32 TrackNumberOffset
		{
			get { return this.trackNumberOffset; }
			set { this.Set( nameof(this.TrackNumberOffset), ref this.trackNumberOffset, value ); }
		}

		private Int32? discNumber;
		public Int32? DiscNumber
		{
			get { return this.discNumber; }
			set { this.Set( nameof(this.DiscNumber), ref this.discNumber, value ); }
		}

		public async void SetTrackNumbers()
		{
			this.IsBusy = true;

			List<Message> messages = await this.utilityService.SetTrackNumbersFromFileNamesAsync( this.FullDirectoryPath, this.TrackNumberOffset, this.DiscNumber );
			this.Messages.AddRange( messages );

			this.IsBusy = false;
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
}