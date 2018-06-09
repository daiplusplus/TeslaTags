using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using TeslaTags;

namespace TeslaTags.Gui
{
	public class RealTeslaTagService : ITeslaTagsService
	{
		public Boolean IsBusy { get; private set; }

		public ITeslaTagEventsListener EventsListener { get; set; }

		public void Start(String directory)
		{
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
				Single total = directories.Count;

				foreach( String directoryPath in directories )
				{
					if( this.stopRequested ) break;

					DirectoryResult result = tp.ProcessDirectory( directoryPath );
					this.EventsListener?.DirectoryUpdate( directoryPath, result.FolderType, result.ModifiedFiles, result.TotalFiles, ++count / total );

					foreach( String warning in result.Warnings )
					{
						String[] comps = warning.Split( '\t' );
						this.EventsListener?.FileWarning( comps[0], comps[1] );
					}

					foreach( String error in result.Errors )
					{
						String[] comps = error.Split( '\t' );
						this.EventsListener?.FileError( comps[0], comps[1] );
					}
				}
			}

			this.IsBusy = false;
			this.EventsListener?.Complete( this.stopRequested );
			this.stopRequested = false;
		}
	}
}
