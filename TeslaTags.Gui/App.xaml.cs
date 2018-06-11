using System;
using System.Configuration;
using System.Diagnostics;
using System.Text;
using System.Windows;

namespace TeslaTags.Gui
{
	public partial class App : Application
	{
		public static Boolean IsTestMode { get; } = ConfigurationManager.AppSettings["designMode"] == "true";

		public static Boolean ExcludeITunes { get; } = ConfigurationManager.AppSettings["excludeITunes"] == "true";

		static App()
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

			GalaSoft.MvvmLight.Threading.DispatcherHelper.Initialize();
		}

		private static void CurrentDomain_UnhandledException(Object sender, UnhandledExceptionEventArgs e)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine( "AppDomain Unhandled exception:" );

			Exception ex = e.ExceptionObject as Exception;
			while( ex != null )
			{
				sb.AppendLine( ex.Message );
				sb.AppendLine( ex.GetType().FullName );
				sb.AppendLine( ex.StackTrace );
				sb.AppendLine();

				ex = ex.InnerException;
			}

			String message = sb.ToString();

			try
			{
				using( EventLog eventLog = new EventLog( "Application" ) )
				{
					eventLog.Source = "Application";
					eventLog.WriteEntry( message, EventLogEntryType.Error, eventID: 101, category: 1 );
				}
			}
			catch
			{
				// er...?
			}
		}
	}
}
