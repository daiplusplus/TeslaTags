using System;
using System.Collections.Generic;
using System.Windows.Threading;

using GalaSoft.MvvmLight.Threading;

namespace TeslaTags.Gui
{
	/*
	class DispatchTeslaTagEventsListener : ITeslaTagEventsListener
	{
		private readonly ITeslaTagEventsListener sink;

		public DispatchTeslaTagEventsListener( ITeslaTagEventsListener sink )
		{
			this.sink = sink;
		}

		private static void Invoke(Boolean wait, Action action)
		{
			// DispatcherHelper.CheckBeginInvokeOnUI doesn't return an DispatcherOperation, grrr
			Boolean canRunSynchronously = DispatcherHelper.UIDispatcher.CheckAccess();
			if( canRunSynchronously )
			{
				action();
			}
			else
			{
				DispatcherOperation op = DispatcherHelper.UIDispatcher.BeginInvoke( action, new Object[0] );
				if( wait ) op.Wait();
			}
		}

		public void Complete(Boolean stoppedEarly)
		{
			Invoke( false, () => this.sink.Complete( stoppedEarly ) );
		}

		public void DirectoryUpdate(String directory, FolderType folderType, Int32 modifiedCount, Int32 totalCount, Single totalPerc, List<Message> messages)
		{
			Invoke( false, () => this.sink.DirectoryUpdate( directory, folderType, modifiedCount, totalCount, totalPerc, messages ) );
		}

		public void GotDirectories(List<String> directories)
		{
			Invoke( true, () => this.sink.GotDirectories( directories ) );
		}

		public void Started()
		{
			Invoke( false, this.sink.Started );
		}
	}*/
}
