using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.WindowsAPICodePack.Dialogs;

namespace TeslaTags.Gui
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			this.InitializeComponent();

			this.browseButton.Click += this.BrowseButton_Click;
		}

		private void BrowseButton_Click(Object sender, RoutedEventArgs e)
		{
			using( CommonOpenFileDialog fbd = new CommonOpenFileDialog() )
			{
				fbd.IsFolderPicker = true;
				fbd.Title = "Browse for root of music directory";
				fbd.EnsurePathExists = true;
				fbd.EnsureValidNames = true;

				if( !String.IsNullOrWhiteSpace( this.ViewModel.DirectoryPath ) )
				{
					if( System.IO.Directory.Exists( this.ViewModel.DirectoryPath ) )
					{
						fbd.InitialDirectory = this.ViewModel.DirectoryPath;
					}
				}

				CommonFileDialogResult result = fbd.ShowDialog( this );
				if( result == CommonFileDialogResult.Ok )
				{
					this.ViewModel.DirectoryPath = fbd.FileName;
				}
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
	}

	public class FullPathToRelativePathConverter : IValueConverter
	{
		public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
		{
			String fullPath = (String)value;
			String prefix = (String)parameter;

			if( fullPath.StartsWith( prefix, StringComparison.OrdinalIgnoreCase ) ) return fullPath.Substring( prefix.Length );

			return fullPath;
		}

		public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
