/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocator xmlns:vm="clr-namespace:TeslaTags.Gui" x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"

  You can also use Blend to do all this with the tool's support.
  See http://www.galasoft.ch/mvvm
*/

using CommonServiceLocator;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;

namespace TeslaTags.Gui.ViewModel
{

	public class ViewModelLocator
	{
		public ViewModelLocator()
		{
			ServiceLocator.SetLocatorProvider( () => SimpleIoc.Default );

			if( ViewModelBase.IsInDesignModeStatic || App.IsTestMode )
			{
				// Create design time view services and models
				SimpleIoc.Default.Register<ITeslaTagsService, DesignTeslaTagService>();
			}
			else
			{
				// Create run time view services and models
				SimpleIoc.Default.Register<ITeslaTagsService,RealTeslaTagService>();
			}

			SimpleIoc.Default.Register<ITeslaTagUtilityService,RealTeslaTagUtilityService>();

			SimpleIoc.Default.Register<MainViewModel>();
		}

		public MainViewModel MainWindow
		{
			get
			{
				return ServiceLocator.Current.GetInstance<MainViewModel>();
			}
		}

		public static void Cleanup()
		{
			// TODO Clear the ViewModels
		}
	}
}