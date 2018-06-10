using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TeslaTags
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

	public interface ITeslaTagUtilityService
	{
		Task<List<Message>> SetTrackNumbersFromFileNamesAsync(String directoryPath, Int32 offset, Int32? discNumber);

		Task<List<Message>> RemoveApeTagsAsync(String directoryPath);

		Task<List<Message>> SetAlbumArtAsync(String directoryPath, String imageFileName, AlbumArtSetMode mode);
	}

	public enum AlbumArtSetMode
	{
		Replace,
		AddIfMissing
	}
}
