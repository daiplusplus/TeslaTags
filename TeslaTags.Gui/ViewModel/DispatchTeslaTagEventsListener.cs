using System;
using System.Collections.Generic;

using GalaSoft.MvvmLight.Threading;

namespace TeslaTags.Gui
{
	class DispatchTeslaTagEventsListener : ITeslaTagEventsListener
	{
		private readonly ITeslaTagEventsListener sink;

		public DispatchTeslaTagEventsListener( ITeslaTagEventsListener sink )
		{
			this.sink = sink;
		}

		public void Complete(Boolean stoppedEarly)
		{
			DispatcherHelper.CheckBeginInvokeOnUI( () => this.sink.Complete( stoppedEarly ) );
		}

		public void DirectoryUpdate(String directory, FolderType folderType, Int32 modifiedCount, Int32 totalCount, Single totalPerc, List<Message> messages)
		{
			DispatcherHelper.CheckBeginInvokeOnUI( () => this.sink.DirectoryUpdate( directory, folderType, modifiedCount, totalCount, totalPerc, messages ) );
		}

		public void GotDirectories(List<String> directories)
		{
			DispatcherHelper.CheckBeginInvokeOnUI( () => this.sink.GotDirectories( directories ) );
		}

		public void Started()
		{
			DispatcherHelper.CheckBeginInvokeOnUI( this.sink.Started );
		}
	}
}
