using System;

using ShellFileDialogs;

namespace TeslaTags.Gui
{
	interface IFileDialogService
	{
		String PromptForDirectoryPath(IntPtr hWnd);
	}

	public class ComFileDialogService : IFileDialogService
	{
		public String PromptForDirectoryPath(IntPtr hWnd)
		{
			String path = FolderBrowserDialog.ShowDialog( hWnd, "Browse for root of Tesla music collection directory structure.", null );
			return path;
		}
	}
}
