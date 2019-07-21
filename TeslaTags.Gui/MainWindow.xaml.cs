using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Navigation;

using GalaSoft.MvvmLight.Ioc;

namespace TeslaTags.Gui
{
	public partial class MainWindow : Window
	{
		private MainViewModel ViewModel => (MainViewModel)this.DataContext;

		public MainWindow()
		{
			this.InitializeComponent();

			this.browseButton.Click += this.BrowseButton_Click;

			this.Loaded  += this.MainWindow_Loaded;
			this.Closing += this.MainWindow_Closing;

			// Nudge the popup targets: // https://stackoverflow.com/questions/1600218/how-can-i-move-a-wpf-popup-when-its-anchor-element-moves
			// The main fix in that QA is for Popups inside a UserControl - as they're in a Window we can access events directly:
			this.LocationChanged += this.WindowRectangleChange;
			this.SizeChanged     += this.WindowRectangleChange;

			//this.DataContextChanged += this.MainWindow_DataContextChanged;
		}

		private void WindowRectangleChange( Object sender, EventArgs e )
		{
			Double offset = this.excludePopup.HorizontalOffset;
			this.excludePopup.HorizontalOffset = offset + 1; // Trigger reflow
			this.excludePopup.HorizontalOffset = offset; // ...but don't make it a visual change.

			offset = this.genrePopup.HorizontalOffset;
			this.genrePopup.HorizontalOffset = offset + 1;
			this.genrePopup.HorizontalOffset = offset;
		}

		#region Window Events

		// HACK: Using events to avoid taking a dependency on the Blend SDK, as it's a simple application:
		private void MainWindow_Loaded(Object sender, RoutedEventArgs e)
		{
			this.cvs = (CollectionViewSource)this.FindResource( "directoriesProgressCvs" );

			this.ViewModel.WindowLoadedCommand.Execute( parameter: null );
		}

		private void MainWindow_Closing(Object sender, System.ComponentModel.CancelEventArgs e)
		{
			this.ViewModel.WindowClosingCommand.Execute( parameter: null );
		}

		#endregion

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

		private void Hyperlink_RequestNavigate(Object sender, RequestNavigateEventArgs e)
		{
			if( e.Uri != null )
			{
				try
				{
					Process p = Process.Start( e.Uri.ToString() );
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

		private CollectionViewSource cvs;

		// Remember, WPF has separate `Checked` and `Unchecked` events, not just a single `CheckedChanged` event like WinForms.
		// BTW - this still causes incorrect DataGrid scrollbar calculations resulting in a scrollbar thumb that changes height as you drag it
		// The only *trivial* solution is to switch to pixel-based scrolling instead of row-item-based scrolling: http://wpfthoughts.blogspot.com/2014/05/datagrid-vertical-scrolling-issues.html
		// ...but is there a way to get both - so it does item height calculations for scrolling based on pixels but the scroll-stops are snapped to each row-item? (i.e. support for variable-height rows)? TODO.
		private void DirectoryFilterCheckedChanged( Object sender, RoutedEventArgs e )
		{
			if( this.cvs == null ) return;

			this.cvs.View.Refresh();
		}

		// This method can be set as `this.csv.View.Filter = DataGridFilter` but it's unclear what the *best* way to do that is given `this.csv.View` could be recreated... I think?
		private Boolean DataGridFilter( Object item )
		{
			DirectoryViewModel dvm = (DirectoryViewModel)item;
			
			Boolean isChecked = this.boringFilterCheckbox.IsChecked ?? false;
			if( isChecked )
			{
				// TODO: Consider moving this logic into DirectoryViewModel directly.
				Boolean isBoring =
					( dvm.FolderType == FolderType.Empty )
					||
					( dvm.FilesModifiedProposed == 0 && dvm.InfoCount == 0 && dvm.WarnCount == 0 && dvm.ErrorCount == 0 );

				return !isBoring;
			}
			else
			{
				return true; // Include all rows.
			}
		}

		// ...alternatively, this alternate approach is provided:
		private void CollectionViewSource_Filter( Object sender, FilterEventArgs e )
		{
			e.Accepted = this.DataGridFilter( e.Item );
		}
	}
}
