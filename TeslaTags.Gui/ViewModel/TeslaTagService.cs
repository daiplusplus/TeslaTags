using System;
using System.Collections.Generic;

namespace TeslaTags.Gui
{
	public interface ITeslaTagsService
	{
		void Start(String directory, Boolean readOnly);
		
		void Stop();

		Boolean IsBusy { get; }

		ITeslaTagEventsListener EventsListener { get; set; }
	}

	public interface ITeslaTagEventsListener
	{
		void Started();
		void GotDirectories(List<String> directories);
		void DirectoryUpdate(String directory, FolderType folderType, Int32 modifiedCount, Int32 totalCount, Single totalPerc, List<Message> messages);
		void Complete(Boolean stoppedEarly);
	}
}
