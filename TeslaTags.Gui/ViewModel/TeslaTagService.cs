using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace TeslaTags.Gui
{
	public interface ITeslaTagsService
	{
		void Start(String directory);
		
		void Stop();

		Boolean IsBusy { get; }

		void AddSubscriber(ITeslaTagEvents recipient);

		void RemoveSubscriber(ITeslaTagEvents recipient);
	}

	public interface ITeslaTagEvents
	{
		void IsBusyChanged(Boolean isBusy);
		void GotDirectories(List<String> directories);
		void DirectoryUpdate(String directory, FolderType folderType, Int32 modifiedCount, Int32 totalCount);
		void FileError(String fileName, String message);
		void FileWarning(String fileName, String message);
	}

	public class DesignTeslaTagService : ITeslaTagsService
	{
		private readonly DispatcherTimer timer;

		private static readonly List<String> _directories = new List<string>()
		{
			@"C:\Foobar",
			@"C:\Foobar\Artist1",
			@"C:\Foobar\Artist1\Album",
			@"C:\Foobar\ArtistB\Singles",
			@"C:\Foobar\Foo",
		};

		public DesignTeslaTagService()
		{
			this.timer = new DispatcherTimer();
			this.timer.Interval = new TimeSpan( hours: 0, minutes: 0, seconds: 2 );
			this.timer.Tick += this.Timer_Tick;
		}

		private Int32 processState = 0; // 0 = Start, 1 = Getting directories, 2 = Got directories/Processing files, 3 = Done.
		private DateTime stateStart = DateTime.MinValue;
		private Int32 directoryIdx = 0;

		private void Timer_Tick(Object sender, EventArgs e)
		{
			if( this.processState == 0 )
			{
				this.processState = 1;
				this.stateStart = DateTime.UtcNow;

				this.subscriber?.IsBusyChanged( isBusy: true );
			}
			else if( this.processState == 1 )
			{
				TimeSpan time = DateTime.UtcNow - this.stateStart;
				if( time.TotalSeconds > 6 )
				{
					this.processState = 2;
					this.subscriber?.GotDirectories( _directories );
				}
			}
			else if( this.processState == 2 )
			{
				TimeSpan time = DateTime.UtcNow - this.stateStart;
				if( this.directoryIdx < _directories.Count )
				{
					Random rng = new Random();

					FolderType randomType = (FolderType)rng.Next( 0, 6 );
					Int32 totalCount = rng.Next( 0, 30 );
					Int32 modifiedCount = rng.Next( 0, totalCount + 1 );

					this.subscriber?.DirectoryUpdate( _directories[this.directoryIdx], randomType, modifiedCount, totalCount );

					this.directoryIdx++;
				}
				else
				{
					this.processState = 3;
				}
			}
			else if( this.processState == 3 )
			{
				this.Stop();
				this.subscriber?.IsBusyChanged( isBusy: false );
			}
		}

		public Boolean IsBusy
		{
			get; private set;
		}

		private ITeslaTagEvents subscriber;

		public void AddSubscriber(ITeslaTagEvents recipient)
		{
			this.subscriber = recipient;
		}

		public void RemoveSubscriber(ITeslaTagEvents recipient)
		{
			this.subscriber = null;
		}

		public void Start(String directory)
		{
			this.IsBusy = true;
			this.timer.Start();
		}

		public void Stop()
		{
			this.timer.Stop();
			this.IsBusy = false;
		}
	}

	public class RealTeslaTagService : ITeslaTagsService
	{
		public Boolean IsBusy
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public void AddSubscriber(ITeslaTagEvents recipient)
		{
			throw new NotImplementedException();
		}

		public void RemoveSubscriber(ITeslaTagEvents recipient)
		{
			throw new NotImplementedException();
		}

		public void Start(String directory)
		{
			throw new NotImplementedException();
		}

		public void Stop()
		{
			throw new NotImplementedException();
		}
	}
}
