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
		private readonly ITeslaTagsService teslaTagService;

		public DirectoryViewModel(ITeslaTagsService teslaTagService, String directoryPath, String prefix)
		{
			this.teslaTagService = teslaTagService;

			this.FullDirectoryPath    = directoryPath;
			this.DisplayDirectoryPath = directoryPath.StartsWith( prefix, StringComparison.OrdinalIgnoreCase ) ? directoryPath.Substring( prefix.Length ) : directoryPath;

			this.Messages.CollectionChanged += this.Messages_CollectionChanged;

			this.OpenFolderCommand      = new RelayCommand( this.OpenFolder );
			this.ApplyAlbumArtCommand   = this.CreateBusyCommand( this.ApplyAlbumArt );
			this.RemoveApeTagsCommand   = this.CreateBusyCommand( this.RemoveApeTags );
			this.SetTrackNumbersCommand = this.CreateBusyCommand( this.SetTrackNumbers );

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
		
		public void OpenFolder()
		{
			using( System.Diagnostics.Process.Start( this.FullDirectoryPath ) ) { } // `Process.Start()` returns null if it's handled by an existing process, e.g. explorer.exe
		}

		#region Apply Album Art

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

			List<Message> messages = await this.teslaTagService.SetAlbumArtAsync( this.FullDirectoryPath, this.SelectedImageFileName, this.ReplaceAllAlbumArt ? AlbumArtSetMode.Replace : AlbumArtSetMode.AddIfMissing );
			this.Messages.AddRange( messages );

			this.IsBusy = false;
		}

		#endregion

		public async void RemoveApeTags()
		{
			this.IsBusy = true;

			List<Message> messages = await this.teslaTagService.RemoveApeTagsAsync( this.FullDirectoryPath );
			this.Messages.AddRange( messages );

			this.IsBusy = false;
		}

		#region Track numbers

		public async void SetTrackNumbers()
		{
			this.IsBusy = true;

			List<Message> messages = await this.teslaTagService.SetTrackNumbersFromFileNamesAsync( this.FullDirectoryPath, this.TrackNumberOffset, this.DiscNumber );
			this.Messages.AddRange( messages );

			this.IsBusy = false;
		}

		#endregion
	}
}
