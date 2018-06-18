using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GalaSoft.MvvmLight;

namespace TeslaTags.Gui
{
	public interface IWindowService
	{
		Window GetWindowByDataContext(Object dataContext);
	}

	public class WindowService : IWindowService
	{
		public Window GetWindowByDataContext(Object dataContext)
		{
			foreach( Window window in Application.Current.Windows )
			{
				if( window.DataContext == dataContext )
				{
					return window;
				}
			}

			return null;
		}

		private static void CreateBinding( INotifyPropertyChanged source, String sourcePropertyPath, DependencyObject target, DependencyProperty targetProperty, BindingMode mode = BindingMode.TwoWay )
		{
			Binding binding = new Binding();
			binding.Source = source;
			binding.Path = new PropertyPath( sourcePropertyPath );
			binding.Mode = mode;
			binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

			BindingOperations.SetBinding( target, targetProperty, binding );
		}
	}
}
