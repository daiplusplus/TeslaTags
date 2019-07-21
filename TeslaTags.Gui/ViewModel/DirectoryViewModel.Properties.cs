using System;
using System.Collections.ObjectModel;

using GalaSoft.MvvmLight.CommandWpf;

namespace TeslaTags.Gui
{
	public partial class DirectoryViewModel
	{
		// Commands:

		public RelayCommand OpenFolderCommand      { get; }
		public RelayCommand ApplyAlbumArtCommand   { get; }
		public RelayCommand RemoveApeTagsCommand   { get; }
		public RelayCommand SetTrackNumbersCommand { get; }

		#region Messages

		private Int32? filesModifiedProposed;
		public Int32? FilesModifiedProposed
		{
			get { return this.filesModifiedProposed; }
			set { this.Set( nameof(this.FilesModifiedProposed), ref this.filesModifiedProposed, value ); }
		}

		private Int32? filesModifiedActual;
		public Int32? FilesModifiedActual
		{
			get { return this.filesModifiedActual; }
			set { this.Set( nameof(this.FilesModifiedActual), ref this.filesModifiedActual, value ); }
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
		
		#endregion

		#region Apply Album Art:

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

		#endregion

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

		#endregion
	}
}
