using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;

namespace TeslaTags.Gui
{
	public class MainViewModel : ViewModelBase, ITeslaTagEventsListener
	{
		private readonly ITeslaTagsService teslaTagsService;

		public MainViewModel(ITeslaTagsService teslaTagsService)
		{
			this.teslaTagsService = teslaTagsService;
			this.teslaTagsService.EventsListener = new DispatchTeslaTagEventsListener( this );

			this.StartCommand = new RelayCommand( this.Start, canExecute: () => !this.teslaTagsService.IsBusy );
			this.StopCommand  = new RelayCommand( this.Stop , canExecute: () =>  this.teslaTagsService.IsBusy );

			this.OnlyValidate = true;
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

		private Boolean isBusy;
		public Boolean IsBusy
		{
			get { return this.isBusy; }
			set {
				this.Set( nameof(this.IsBusy), ref this.isBusy, value );
				this.RaisePropertyChanged( nameof(this.IsNotBusy) );

				this.StartCommand.RaiseCanExecuteChanged();
				this.StopCommand.RaiseCanExecuteChanged();
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
			this.viewModelDict.Clear();
			this.DirectoriesProgress.Clear();
			foreach( String directoryPath in directories )
			{
				DirectoryProgressViewModel dirVM = new DirectoryProgressViewModel( directoryPath, prefix: this.DirectoryPath );
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

		public ObservableCollection<Message> Messages { get; } = new ObservableCollection<Message>();
	}
}