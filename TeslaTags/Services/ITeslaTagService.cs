using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TeslaTags
{
	public interface ITeslaTagsService
	{
		Task StartRetaggingAsync( RetaggingOptions options, IProgress<IReadOnlyList<String>> directories, IProgress<DirectoryResult> directoryProgress, CancellationToken cancellationToken );

		Task<List<Message>> SetTrackNumbersFromFileNamesAsync(String directoryPath, Int32 offset, Int32? discNumber);

		Task<List<Message>> RemoveApeTagsAsync(String directoryPath);

		Task<List<Message>> SetAlbumArtAsync(String directoryPath, String imageFileName, AlbumArtSetMode mode);
	}

	public class RetaggingOptions
	{
		public RetaggingOptions(String musicRootDirectory, Boolean readOnly, Boolean undo, IDirectoryPredicate directoryFilterPredicate, GenreRules genreRules)
		{
			this.MusicRootDirectory          = musicRootDirectory ?? throw new ArgumentNullException( nameof( musicRootDirectory ) );
			this.ReadOnly                    = readOnly;
			this.Undo                        = undo;
			this.DirectoryFilterPredicate    = directoryFilterPredicate ?? new EmptyDirectoryPredicate();
			this.GenreRules                  = genreRules ?? throw new ArgumentNullException( nameof( genreRules ) );
		}

		public String              MusicRootDirectory       { get; }
		public Boolean             ReadOnly                 { get; }
		public Boolean             Undo                     { get; }
		public IDirectoryPredicate DirectoryFilterPredicate { get; }
		public GenreRules          GenreRules               { get; }
	}

	public enum AlbumArtSetMode
	{
		Replace,
		AddIfMissing
	}
	}
