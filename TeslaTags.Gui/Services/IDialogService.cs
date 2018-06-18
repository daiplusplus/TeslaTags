using System;

using ShellFileDialogs;

namespace TeslaTags.Gui
{
	public interface IFileDialogService
	{
		String ShowFolderBrowseDialog(IntPtr hWnd, String title);

		String ShowFileOpenDialogForImages(IntPtr hWnd, String title, String initialDirectory);
	}

	public class ComFileDialogService : IFileDialogService
	{
		public String ShowFolderBrowseDialog(IntPtr hWnd, String title)
		{
			String path = FolderBrowserDialog.ShowDialog( hWnd, title, null );
			return path;
		}

		private static readonly Filter[] _imagesFilters = new Filter[]
		{
			 new Filter( "Images", "jpg", "jpeg", "png", "bmp", "gif" ),
			 new Filter( "All files", "*" ),
		};

		public String ShowFileOpenDialogForImages(IntPtr hWnd, String title, String initialDirectory)
		{
			String path = FileOpenDialog.ShowSingleSelectDialog( hWnd, title, initialDirectory, defaultFileName: null, filters: _imagesFilters, selectedFilterIndex: -1 );
			return path;
		}
	}
}
