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
	public partial class DirectoryViewModel : BaseViewModel
	{
		private readonly ITeslaTagsService         teslaTagService;
		private readonly ILiveConfigurationService liveConfiguration;

		public DirectoryViewModel( ITeslaTagsService teslaTagService, ILiveConfigurationService liveConfiguration, String directoryPath, String prefix, IEnumerable<FileInfo> imagesInFolder )
		{
			this.teslaTagService   = teslaTagService   ?? throw new ArgumentNullException(nameof(teslaTagService)); 
			this.liveConfiguration = liveConfiguration ?? throw new ArgumentNullException(nameof(liveConfiguration));

			if( imagesInFolder == null ) throw new ArgumentNullException(nameof(imagesInFolder));

			//

			this.FullDirectoryPath    = directoryPath ?? throw new ArgumentNullException(nameof(directoryPath));
			this.DisplayDirectoryPath = directoryPath.StartsWith( prefix, StringComparison.OrdinalIgnoreCase ) ? directoryPath.Substring( prefix.Length ) : directoryPath;

			this.Messages.CollectionChanged += this.Messages_CollectionChanged;

			this.OpenFolderCommand      = new RelayCommand( this.OpenFolder );
			this.ApplyAlbumArtCommand   = this.CreateBusyCommand( this.ApplyAlbumArt );
			this.RemoveApeTagsCommand   = this.CreateBusyCommand( this.RemoveApeTags );
			this.SetTrackNumbersCommand = this.CreateBusyCommand( this.SetTrackNumbers );

			//

			this.ImagesInFolder.AddRange( imagesInFolder.Select( fi => fi.Name ) );
		}

		#region Messages

		private void Messages_CollectionChanged(Object sender, NotifyCollectionChangedEventArgs e)
		{
			this.RaisePropertyChanged( nameof(this.InfoCount) );
			this.RaisePropertyChanged( nameof(this.WarnCount) );
			this.RaisePropertyChanged( nameof(this.ErrorCount) );

			this.RaisePropertyChanged( nameof(this.ShowWarnColor) );
			this.RaisePropertyChanged( nameof(this.ShowErrorColor) );
		}

		public ObservableCollection<Message> Messages { get; } = new ObservableCollection<Message>();

		public Int32   InfoCount      => this.Messages.Count( m => m.Severity == MessageSeverity.Info || m.Severity == MessageSeverity.FileModification );
		public Int32   WarnCount      => this.Messages.Count( m => m.Severity == MessageSeverity.Warning );
		public Int32   ErrorCount     => this.Messages.Count( m => m.Severity == MessageSeverity.Error   );

		public Boolean ShowWarnColor  => this.Messages.Any( m => m.Severity == MessageSeverity.Warning );
		public Boolean ShowErrorColor => this.Messages.Any( m => m.Severity == MessageSeverity.Error   );

		#endregion

		public String FullDirectoryPath { get; }
		public String DisplayDirectoryPath { get; }
		
		public void OpenFolder()
		{
			using( System.Diagnostics.Process.Start( this.FullDirectoryPath ) ) { } // `Process.Start()` returns null if it's handled by an existing process, e.g. explorer.exe
		}

		#region Apply Album Art

		internal static HashSet<String> AlbumArtFileExtensions { get; } = FileSystemPredicate.CreateFileExtensionHashSet( FileSystemPredicate.DefaultAlbumartImageFileExtensions );

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

			FileSystemPredicate fsp = this.liveConfiguration.CreateFileSystemPredicate();

			List<Message> messages = await this.teslaTagService.SetAlbumArtAsync( this.FullDirectoryPath, fsp.FileExtensionsToLoad, this.SelectedImageFileName, this.ReplaceAllAlbumArt ? AlbumArtSetMode.Replace : AlbumArtSetMode.AddIfMissing );
			this.Messages.AddRange( messages );

			String summary = "Set album art in {0:N0} files.".FormatCurrent( messages.GetModifiedFileCount() );
			this.LastOperationSummary = summary;

			this.IsBusy = false;
		}

		#endregion

		public async void RemoveApeTags()
		{
			this.IsBusy = true;

			FileSystemPredicate fsp = this.liveConfiguration.CreateFileSystemPredicate();

			List<Message> messages = await this.teslaTagService.RemoveApeTagsAsync( this.FullDirectoryPath, fsp.FileExtensionsToLoad );
			this.Messages.AddRange( messages );

			String summary = "Removed APE tags from {0:N0} files.".FormatCurrent( messages.GetModifiedFileCount() );
			this.LastOperationSummary = summary;

			this.IsBusy = false;
		}

		#region Track numbers

		public async void SetTrackNumbers()
		{
			this.IsBusy = true;

			FileSystemPredicate fsp = this.liveConfiguration.CreateFileSystemPredicate();

			List<Message> messages = await this.teslaTagService.SetTrackNumbersFromFileNamesAsync( this.FullDirectoryPath, fsp.FileExtensionsToLoad, this.TrackNumberOffset, this.DiscNumber );
			this.Messages.AddRange( messages );

			String summary = "Set track numbers in {0:N0} files.".FormatCurrent( messages.GetModifiedFileCount() );
			this.LastOperationSummary = summary;

			this.IsBusy = false;
		}

		#endregion
	}
}
