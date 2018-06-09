using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace TeslaTags.Gui
{
	public class RealTeslaTagService : ITeslaTagsService
	{
		public Boolean IsBusy { get; private set; }

		public ITeslaTagEventsListener EventsListener { get; set; }

		private Boolean isReadOnlyMode;

		public void Start(String directory, Boolean readOnly)
		{
			this.isReadOnlyMode = readOnly;

			ThreadPool.QueueUserWorkItem( new WaitCallback( this.StartThread ), directory );
			this.IsBusy = true;
		}

		public void Stop()
		{
			this.stopRequested = true;
		}

		private Boolean stopRequested = false;

		private void StartThread(Object state)
		{
			this.EventsListener?.Started();

			String root = (String)state;

			using( TeslaTagsProcesser tp = new TeslaTagsProcesser( root ) )
			{
				List<String> directories = tp.GetDirectories();
				this.EventsListener?.GotDirectories( directories );

				Single count = 0;
				Single total = directories.Count( s => s != null );

				foreach( String directoryPath in directories )
				{
					if( this.stopRequested ) break;

					if( directoryPath != null )
					{
						DirectoryResult result = tp.ProcessDirectory( directoryPath, this.isReadOnlyMode );
						this.EventsListener?.DirectoryUpdate( directoryPath, result.FolderType, result.ModifiedFiles, result.TotalFiles, ++count / total, result.Messages );
					}
				}

				if( !this.stopRequested ) tp.DeleteDirectoryLog();
			}

			this.IsBusy = false;
			this.EventsListener?.Complete( this.stopRequested );
			this.stopRequested = false;
		}
	}
}
