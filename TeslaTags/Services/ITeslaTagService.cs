using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TeslaTags
{
	public interface ITeslaTagsService
	{
		Task StartRetaggingAsync( RetaggingOptions options, IProgress<IReadOnlyList<String>> directories, IProgress<DirectoryResult> directoryProgress, CancellationToken cancellationToken );

		Task<List<Message>> SetTrackNumbersFromFileNamesAsync(String directoryPath, HashSet<String> fileExtensionsToLoad, Int32 offset, Int32? discNumber);

		Task<List<Message>> RemoveApeTagsAsync(String directoryPath, HashSet<String> fileExtensionsToLoad);

		Task<List<Message>> SetAlbumArtAsync(String directoryPath, HashSet<String> fileExtensionsToLoad, String imageFileName, AlbumArtSetMode mode);
	}

	public enum AlbumArtSetMode
	{
		Replace,
		AddIfMissing
	}
}
