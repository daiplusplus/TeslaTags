using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace TeslaTags.Gui
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public static Boolean IsTestMode { get; } = ConfigurationManager.AppSettings["designMode"] == "true";

		public static Boolean ExcludeITunes { get; } = ConfigurationManager.AppSettings["excludeITunes"] == "true";

		static App()
		{
			GalaSoft.MvvmLight.Threading.DispatcherHelper.Initialize();
		}

	}
}
