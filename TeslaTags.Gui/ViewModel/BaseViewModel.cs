using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;

namespace TeslaTags.Gui
{
	public abstract class BaseViewModel : ViewModelBase
	{
		protected RelayCommand CreateBusyCommand( Action action, Boolean enabledWhenBusy = false )
		{
			RelayCommand cmd;
			if( enabledWhenBusy )
			{
				cmd = new RelayCommand( action, canExecute: this.CanExecuteWhenBusy );
			}
			else
			{
				cmd = new RelayCommand( action, canExecute: this.CanExecuteWhenNotBusy );
			}
			this.busyCommands.Add( cmd );
			return cmd;
		}

		private readonly List<RelayCommand> busyCommands = new List<RelayCommand>();

		private Boolean isBusy;
		public Boolean IsBusy
		{
			get { return this.isBusy; }
			set {
				this.Set( nameof(this.IsBusy), ref this.isBusy, value );
				this.RaisePropertyChanged( nameof(this.IsNotBusy) );

				foreach( RelayCommand cmd in this.busyCommands ) cmd.RaiseCanExecuteChanged();
			}
		}
		public Boolean IsNotBusy => !this.IsBusy;

		protected Boolean CanExecuteWhenNotBusy()
		{
			return !this.IsBusy;
		}

		protected Boolean CanExecuteWhenBusy()
		{
			return this.IsBusy;
		}

		protected static ObservableCollection<ValueOption<TEnum>> CreateOptions<TEnum>()
			where TEnum : System.Enum /* make sure you're using the C# 7.3 compiler for this: https://github.com/dotnet/csharplang/issues/104 */
		{
			//TEnum[] values = (TEnum[])Enum.GetValues( typeof(TEnum) );

			Type type = typeof(TEnum);
			var options = type
				.GetFields( BindingFlags.Static | BindingFlags.Public )
				.Select( fi => new {
					Description = fi.GetCustomAttribute<DescriptionAttribute>()?.Description,
					Value       = (TEnum)fi.GetValue( null )
				}  )
				.Select( t => new ValueOption<TEnum>( t.Value, t.Description ?? t.Value.ToString() ) );
				
			ObservableCollection<ValueOption<TEnum>> ret = new ObservableCollection<ValueOption<TEnum>>();
			ret.AddRange( options );
			return ret;
		}
	}

	public class ValueOption<T>
	{
		public ValueOption( T value, String text )
		{
			this.Value = value;
			this.Text = text;
		}

		public T Value { get; }
		public String Text { get; }
	}
}
