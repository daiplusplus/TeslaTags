using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;

namespace TeslaTags.Gui
{
	public class MainViewModel : ViewModelBase, ITeslaTagEvents
	{
		private readonly ITeslaTagsService teslaTagsService;

		public MainViewModel(ITeslaTagsService teslaTagsService)
		{
			this.teslaTagsService = teslaTagsService;
			this.teslaTagsService.AddSubscriber( this );

			this.StartCommand = new RelayCommand( this.Start, canExecute: () => !this.teslaTagsService.IsBusy );
			this.StopCommand  = new RelayCommand( this.Stop , canExecute: () =>  this.teslaTagsService.IsBusy );

			if( App.IsTestMode )
			{
				this.DirectoryPath = @"F:\MyMusic\Music";
			}
		}

		private String directoryPath;
		public String DirectoryPath
		{
			get { return this.directoryPath; }
			set { this.Set( nameof(this.DirectoryPath), ref this.directoryPath, value ); }
		}

		private readonly Dictionary<String,DirectoryProgressViewModel> viewModelDict = new Dictionary<String,DirectoryProgressViewModel>( StringComparer.OrdinalIgnoreCase );

		public ObservableCollection<DirectoryProgressViewModel> DirectoriesProgress { get; } = new ObservableCollection<DirectoryProgressViewModel>();

		private Boolean isBusy;
		public Boolean IsBusy
		{
			get { return this.isBusy; }
			set {
				this.Set( nameof(this.IsBusy), ref this.isBusy, value );
				this.RaisePropertyChanged( nameof(this.IsNotBusy) );
			}
		}
		public Boolean IsNotBusy => !this.IsBusy;

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

		public RelayCommand StartCommand { get; }

		public RelayCommand StopCommand { get; }

		private void Start()
		{
			this.teslaTagsService.Start( this.DirectoryPath );
			this.ProgressPerc = -1;
		}

		private void Stop()
		{
			this.teslaTagsService.Stop();
		}

		private DirectoryProgressViewModel GetDirectoryProgressViewModel( String directory )
		{
			DirectoryProgressViewModel dirVM;
			if( !this.viewModelDict.TryGetValue( directory, out dirVM ) )
			{
				throw new InvalidOperationException( "Event raised in previously unreported directory: " + directory );
			}
			return dirVM;
		}

		#region ITeslaTagEvents

		void ITeslaTagEvents.IsBusyChanged(Boolean isBusy)
		{
			this.IsBusy = isBusy;
			this.StartCommand.RaiseCanExecuteChanged();
			this.StopCommand.RaiseCanExecuteChanged();
		}

		void ITeslaTagEvents.GotDirectories(List<String> directories)
		{
			this.viewModelDict.Clear();
			this.DirectoriesProgress.Clear();
			foreach( String directoryPath in directories )
			{
				DirectoryProgressViewModel dirVM = new DirectoryProgressViewModel( directoryPath, prefix: this.DirectoryPath );
				this.viewModelDict.Add( directoryPath, dirVM );
				this.DirectoriesProgress.Add( dirVM );
			}
		}

		void ITeslaTagEvents.DirectoryUpdate(String directory, FolderType folderType, Int32 modifiedCount, Int32 totalCount, Single totalPerc)
		{
			DirectoryProgressViewModel dirVM = this.GetDirectoryProgressViewModel( directory );
			dirVM.FilesModified = modifiedCount;
			dirVM.FolderType    = folderType;
			dirVM.TotalFiles    = totalCount;

			this.ProgressPerc = totalPerc;
		}

		void ITeslaTagEvents.FileError(String fileName, String message)
		{
			String directory = Path.GetDirectoryName( fileName );

			DirectoryProgressViewModel dirVM = this.GetDirectoryProgressViewModel( directory );

			String filesName = Path.GetFileName( fileName );
			dirVM.Errors.Add( new FileMessageViewModel( fileName, filesName, message ) );
		}

		void ITeslaTagEvents.FileWarning(String fileName, String message)
		{
			String directory = Path.GetDirectoryName( fileName );

			DirectoryProgressViewModel dirVM = this.GetDirectoryProgressViewModel( directory );

			String filesName = Path.GetFileName( fileName );
			dirVM.Warnings.Add( new FileMessageViewModel( fileName, filesName, message ) );
		}

		#endregion
	}

	public class DirectoryProgressViewModel : ViewModelBase
	{
		public DirectoryProgressViewModel(String directoryPath, String prefix)
		{
			this.FullDirectoryPath    = directoryPath;
			this.DisplayDirectoryPath = directoryPath.StartsWith( prefix, StringComparison.OrdinalIgnoreCase ) ? directoryPath.Substring( prefix.Length ) : directoryPath;
		}

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

		public ObservableCollection<FileMessageViewModel> Errors   { get; } = new ObservableCollection<FileMessageViewModel>();
		public ObservableCollection<FileMessageViewModel> Warnings { get; } = new ObservableCollection<FileMessageViewModel>();
	}
	
	public class FileMessageViewModel : ViewModelBase
	{
		public FileMessageViewModel( String fileName, String displayFileName, String message )
		{
			this.FullFileName    = fileName;
			this.DisplayFileName = displayFileName;
			this.Message         = message;
		}

		public String FullFileName { get; }
		public String DisplayFileName { get; }
		public String Message { get; }
	}
}