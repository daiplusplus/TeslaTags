using System;
using System.Linq;
using System.Windows;

namespace TeslaTags.Gui
{
	public interface IWindowService
	{
		Window GetWindowByDataContext(Object dataContext);

		void ShowMessageBoxWarningDialog(Object dataContext, String title, String message);

		void ShowMessageBoxErrorDialog(Object dataContext, String title, String message);
	}

	public class WindowService : IWindowService
	{
		public Window GetWindowByDataContext(Object dataContext)
		{
			return Application.Current.Windows
				.Cast<Window>()
				.SingleOrDefault( w => w.DataContext == dataContext );
		}

		public void ShowMessageBoxWarningDialog(Object dataContext, String title, String message)
		{
			Window window = this.GetWindowByDataContext( dataContext );

			MessageBox.Show(
				owner         : window,
				messageBoxText: message,
				caption       : title,
				button        : MessageBoxButton.OK,
				icon          : MessageBoxImage.Warning,
				defaultResult : MessageBoxResult.OK,
				options       : MessageBoxOptions.None
			);
		}

		public void ShowMessageBoxErrorDialog(Object dataContext, String title, String message)
		{
			Window window = this.GetWindowByDataContext( dataContext );

			MessageBox.Show(
				owner         : window,
				messageBoxText: message,
				caption       : title,
				button        : MessageBoxButton.OK,
				icon          : MessageBoxImage.Error,
				defaultResult : MessageBoxResult.OK,
				options       : MessageBoxOptions.None
			);
		}

		// This function was intended for binding to the WPF Window size+position+state, but it's easier to do it directly without binding.
		/*
		private static void CreateBinding( INotifyPropertyChanged source, String sourcePropertyPath, DependencyObject target, DependencyProperty targetProperty, BindingMode mode = BindingMode.TwoWay )
		{
			Binding binding = new Binding();
			binding.Source = source;
			binding.Path = new PropertyPath( sourcePropertyPath );
			binding.Mode = mode;
			binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

			BindingOperations.SetBinding( target, targetProperty, binding );
		}*/
	}
}
