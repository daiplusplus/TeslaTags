using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Navigation;

using GalaSoft.MvvmLight.Ioc;

namespace TeslaTags.Gui
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			this.InitializeComponent();

			this.browseButton.Click += this.BrowseButton_Click;

			this.Loaded += this.MainWindow_Loaded;
		}

		private void MainWindow_Loaded(Object sender, RoutedEventArgs e)
		{
			// HACK: Using events to avoid taking a dependency on the Blend SDK, as it's a simple application:

		}

		private void BrowseButton_Click(Object sender, RoutedEventArgs e)
		{
			WindowInteropHelper helper = new WindowInteropHelper( this );
			IntPtr hWnd = helper.Handle;

			String path = SimpleIoc.Default.GetInstance<IFileDialogService>().ShowFolderBrowseDialog( hWnd, "Browse for root of Tesla music collection directory structure." );

			if( Directory.Exists( path ) )
			{
				this.ViewModel.DirectoryPath = path;
			}
		}

		private MainViewModel ViewModel => (MainViewModel)this.DataContext;

		private void Hyperlink_RequestNavigate(Object sender, RequestNavigateEventArgs e)
		{
			if( e.Uri != null )
			{
				try
				{
					Process p = System.Diagnostics.Process.Start( e.Uri.ToString() );
					if( p != null ) p.Dispose();
				}
				catch
				{
				}
			}
		}

		private void AlbumArtBrowseButton_Click(Object sender, RoutedEventArgs e)
		{
			DirectoryViewModel dvm = this.ViewModel.SelectedDirectory;
		
			WindowInteropHelper helper = new WindowInteropHelper( this );
			IntPtr hWnd = helper.Handle;

			String path = SimpleIoc.Default.GetInstance<IFileDialogService>().ShowFileOpenDialogForImages( hWnd, "Browse for new album art image.", dvm.FullDirectoryPath );

			if( Directory.Exists( path ) )
	{
				if( path.StartsWith( dvm.FullDirectoryPath, StringComparison.OrdinalIgnoreCase ) )
		{
					path = path.Substring( dvm.FullDirectoryPath.Length );
		}

				this.ViewModel.SelectedDirectory.SelectedImageFileName = path;
		}
	}

	}
}
