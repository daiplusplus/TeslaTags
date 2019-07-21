using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace TeslaTags.Gui
{
	/// <summary>From https://stackoverflow.com/questions/1027051/how-to-autoscroll-on-wpf-datagrid</summary>
	public static class DataGridBehavior
	{
		public static readonly DependencyProperty AutoscrollProperty = DependencyProperty.RegisterAttached( name: "Autoscroll", propertyType: typeof(Boolean), ownerType: typeof(DataGridBehavior), new PropertyMetadata( defaultValue: default(Boolean), propertyChangedCallback: AutoscrollChangedCallback ) );

		private static readonly Dictionary<DataGrid, NotifyCollectionChangedEventHandler> _handlersDict = new Dictionary<DataGrid, NotifyCollectionChangedEventHandler>();

		private static void AutoscrollChangedCallback( DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args )
		{
			DataGrid dataGrid = dependencyObject as DataGrid;
			if( dataGrid == null )
			{
				throw new InvalidOperationException( "Dependency object is not DataGrid." );
			}

			if( (Boolean)args.NewValue )
			{
				Subscribe( dataGrid );
			}
			else
			{
				Unsubscribe( dataGrid );
			}
		}

		private static void Subscribe( DataGrid dataGrid )
		{
			if( !_handlersDict.ContainsKey( dataGrid ) )
			{
				NotifyCollectionChangedEventHandler handler = new NotifyCollectionChangedEventHandler( ( Object sender, NotifyCollectionChangedEventArgs e ) => ScrollToEnd( dataGrid ) ); // `sender` is a Collection, not the DataGrid.

				( (INotifyCollectionChanged)dataGrid.Items ).CollectionChanged += handler;

				_handlersDict.Add( dataGrid, handler );

				dataGrid.Unloaded += DataGridOnUnloaded;
				dataGrid.Loaded   += DataGridOnLoaded;

				ScrollToEnd( dataGrid );
			}
		}

		private static void Unsubscribe( DataGrid dataGrid )
		{
			if( _handlersDict.TryGetValue( dataGrid, out NotifyCollectionChangedEventHandler handler ) )
			{
				( (INotifyCollectionChanged)dataGrid.Items ).CollectionChanged -= handler;

				_handlersDict.Remove( dataGrid );

				dataGrid.Unloaded -= DataGridOnUnloaded;
				dataGrid.Loaded   -= DataGridOnLoaded;
			}
		}

		private static void DataGridOnLoaded( Object sender, RoutedEventArgs routedEventArgs )
		{
			DataGrid dataGrid = (DataGrid)sender;
			if( GetAutoscroll( dataGrid ) )
			{
				Subscribe( dataGrid );
			}
		}

		private static void DataGridOnUnloaded( Object sender, RoutedEventArgs routedEventArgs )
		{
			DataGrid dataGrid = (DataGrid)sender;
			if( GetAutoscroll( dataGrid ) )
			{
				Unsubscribe( dataGrid );
			}
		}

		private static void ScrollToEnd( DataGrid datagrid )
		{
			if( datagrid.Items.Count > 0 )
			{
				datagrid.ScrollIntoView( datagrid.Items[datagrid.Items.Count - 1] );
			}
		}

		public static void SetAutoscroll( DependencyObject element, Boolean value )
		{
			element.SetValue( AutoscrollProperty, value );
		}

		public static Boolean GetAutoscroll( DependencyObject element )
		{
			return (Boolean)element.GetValue( AutoscrollProperty );
		}
	}
}
