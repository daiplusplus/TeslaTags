using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace TeslaTags.Gui
{
    public class DesignTeslaTagService : ITeslaTagsService
	{
		private String rootDirectory;

		private readonly DispatcherTimer timer;

		private static readonly List<String> _directories = new List<String>()
		{
			@"Foobar",
			@"Foobar\Artist1",
			@"Foobar\Artist1\Album",
			@"Foobar\ArtistB\Singles",
			@"Foobar\Foo",
		};

		private List<String> directories;

		public DesignTeslaTagService()
		{
			this.timer = new DispatcherTimer();
			this.timer.Interval = new TimeSpan( hours: 0, minutes: 0, seconds: 1 );
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

				this.EventsListener?.Started();
			}
			else if( this.processState == 1 )
			{
				TimeSpan time = DateTime.UtcNow - this.stateStart;
				if( time.TotalSeconds > 2 )
				{
					this.processState = 2;
					this.EventsListener?.GotDirectories( this.directories );
				}
			}
			else if( this.processState == 2 )
			{
				TimeSpan time = DateTime.UtcNow - this.stateStart;
				if( this.directoryIdx < this.directories.Count )
				{
					String directory = this.directories[this.directoryIdx];

					Random rng = new Random();

					FolderType randomType = (FolderType)rng.Next( 0, 6 );
					Int32 totalCount = rng.Next( 0, 30 );
					Int32 modifiedCount = rng.Next( 0, totalCount + 1 );

					List<Message> messages = new List<Message>();
					if( rng.Next( 0, 2 ) == 1 ) messages.Add( new Message( MessageSeverity.Warning, directory, directory, "A directory warning" ) );
					for( Int32 i = 0; i < totalCount; i++ )
					{
						String fileName = Path.Combine( directory, "File_" + i + ".mp3" );
						if( rng.Next(0, 4) == 3 ) messages.Add( new Message( MessageSeverity.Warning, directory, fileName, "Some file warning" ) );
						if( rng.Next(0, 8) == 3 ) messages.Add( new Message( MessageSeverity.Warning, directory, fileName, "Some file error" ) );
					}

					this.EventsListener?.DirectoryUpdate( directory, randomType, modifiedCount, totalCount, ((Single)this.directoryIdx + 1f) / (Single)this.directories.Count, messages );

					this.directoryIdx++;
				}
				else
				{
					this.processState = 3;
				}
			}
			else if( this.processState == 3 )
			{
				this.timer.Stop();
				this.IsBusy = false;
				this.EventsListener?.Complete( false );
			}
		}

		public Boolean IsBusy
		{
			get; private set;
		}

		public ITeslaTagEventsListener EventsListener { get; set; }

		public void Start(String directory, Boolean readOnly, Boolean undo, GenreRules genreRules)
		{
			this.rootDirectory = directory;
			this.directories = _directories
				.Select( p => Path.Combine( directory, p ) )
				.ToList();

			this.IsBusy = true;
			this.timer.Start();
		}

		public void Stop()
		{
			this.timer.Stop();
			this.IsBusy = false;
			this.EventsListener?.Complete( true );
		}
	}

	public class DesignTeslaTagUtilityService : ITeslaTagUtilityService
	{
		public Task<List<Message>> RemoveApeTagsAsync(String directoryPath)
		{
			List<Message> messages = new List<Message>();
			messages.Add( new Message( MessageSeverity.Warning, @"C:\Music", @"C:\Music\Foo.mp3", "Nothing happened. This is a design-mode class." ) );

			return Task.FromResult( messages );
		}

		public Task<List<Message>> SetAlbumArtAsync(String directoryPath, String imageFileName, AlbumArtSetMode mode)
		{
			List<Message> messages = new List<Message>();
			messages.Add( new Message( MessageSeverity.Warning, @"C:\Music", @"C:\Music\Foo.mp3", "Nothing happened. This is a design-mode class." ) );

			return Task.FromResult( messages );
		}

		public Task<List<Message>> SetTrackNumbersFromFileNamesAsync(String directoryPath, Int32 offset, Int32? discNumber)
		{
			List<Message> messages = new List<Message>();
			messages.Add( new Message( MessageSeverity.Warning, @"C:\Music", @"C:\Music\Foo.mp3", "Nothing happened. This is a design-mode class." ) );

			return Task.FromResult( messages );
		}
	}
}
