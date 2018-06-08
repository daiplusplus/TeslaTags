using System;
using GalaSoft.MvvmLight;

namespace TeslaTags.Gui
{
	public class MainViewModel : ViewModelBase
	{
		public MainViewModel()
		{
			if( this.IsInDesignMode )
			{

			}
			else
			{

			}
		}

		private String directoryPath;
		public String DirectoryPath
		{
			get { return this.directoryPath; }
			set { this.Set( nameof(this.DirectoryPath), ref this.directoryPath, value ); }
		}
	}
}