using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace TeslaTags.Gui
{
	public class RealTeslaTagService : ITeslaTagsService
	{
		public Boolean IsBusy { get; private set; }

		public ITeslaTagEventsListener EventsListener { get; set; }

		public void Start(String directory, Boolean readOnly, GenreRules genreRules)
		{
			ThreadPool.QueueUserWorkItem( new WaitCallback( ( state ) => this.StartThread( directory, readOnly, genreRules ) ) );
			this.IsBusy = true;
		}

		public void Stop()
		{
			this.stopRequested = true;
		}

		private Boolean stopRequested = false;

		private void StartThread(String directory, Boolean readOnly, GenreRules genreRules)
		{
			this.EventsListener?.Started();

			using( TeslaTagsProcesser tp = new TeslaTagsProcesser( directory ) )
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
						DirectoryResult result = tp.ProcessDirectory( directoryPath, readOnly, genreRules );
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
