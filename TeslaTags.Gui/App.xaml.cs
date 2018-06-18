using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using CommonServiceLocator;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;

//using Nito.AsyncEx;

namespace TeslaTags.Gui
{
	public static class Program
	{
		// This is the exact same code that the PresentationBuildTasks compiler builds.
		// Except with async before starting the application object, to prevent issues caused by running async code in synchronous "OnFoo" event-handlers (i.e. `OnStartup`).
		// It also sets-up IoC before any WPF code starts too.
		[STAThread]
		//public static async Task Main()
		public static Int32 Main(String[] args)
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

			#region Attempts at running async code before WPF:
			#if NEVER

			//AsyncContext.Run( XmlDocumentConfigurationService.Instance.LoadConfigAsync );

			//System.Threading.SynchronizationContext.SetSynchronizationContext(  )

			Int32 th0 = Thread.CurrentThread.ManagedThreadId;
			SynchronizationContext th0Context = SynchronizationContext.Current;

			Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
			DispatcherSynchronizationContext context = new DispatcherSynchronizationContext( dispatcher );
			SynchronizationContext.SetSynchronizationContext( context );

			SynchronizationContext th1Context = SynchronizationContext.Current;

			await XmlDocumentConfigurationService.Instance.LoadConfigAsync();

			Int32 th2 = Thread.CurrentThread.ManagedThreadId;
			SynchronizationContext th2Context = SynchronizationContext.Current;

			#endif
			#endregion

			XmlDocumentConfigurationService.Instance.LoadConfig();

			////////////////////

			RegisterDependencies( SimpleIoc.Default );

			////////////////////

			// Then call into WPF's PresentationBuildTasks-generated Main:
			TeslaTagsApplication.Main();

			return 0;
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

		private static void RegisterDependencies(SimpleIoc ioc)
		{
			ServiceLocator.SetLocatorProvider( () => ioc );

			ioc.Register<IConfigurationService>( () => XmlDocumentConfigurationService.Instance );

			IConfigurationService configService = ioc.GetInstance<IConfigurationService>();

			if( ViewModelBase.IsInDesignModeStatic || configService.Config.DesignMode )
			{
				// https://olitee.com/2015/01/mvvmlight-simpleioc-design-time-error/
				if( !ioc.IsRegistered<ITeslaTagsService>() )
				{
					ioc.Register<ITeslaTagsService, DesignTeslaTagService>();
				}

				if( !ioc.IsRegistered<ITeslaTagUtilityService>() )
				{
					ioc.Register<ITeslaTagUtilityService, DesignTeslaTagUtilityService>();
				}
			}
			else
			{
				// Create run time view services and models
				ioc.Register<ITeslaTagsService,RealTeslaTagService>();
				ioc.Register<ITeslaTagUtilityService,RealTeslaTagUtilityService>();
			}

			ioc.Register<IWindowService,WindowService>();

			ioc.Register<MainViewModel>();
		}
	}

	public partial class TeslaTagsApplication : Application
	{
		static TeslaTagsApplication()
		{
			GalaSoft.MvvmLight.Threading.DispatcherHelper.Initialize();
		}

		/*
		protected override async void OnStartup(StartupEventArgs e)
		{
			RegisterDependencies();

			// https://stackoverflow.com/questions/49701102/wpf-app-run-async-task-before-opening-window

			IConfigurationService configService = SimpleIoc.Default.GetInstance<IConfigurationService>();
			await configService.LoadConfigAsync();

			base.OnStartup( e );
		}*/

	}
}
