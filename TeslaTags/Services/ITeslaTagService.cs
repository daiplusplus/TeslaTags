using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TeslaTags
{
	public interface ITeslaTagsService
	{
		Task StartRetaggingAsync( RetaggingOptions options, IProgress<IReadOnlyList<String>> directories, IProgress<DirectoryResult> directoryProgress, CancellationToken ct );

		Task<List<Message>> SetTrackNumbersFromFileNamesAsync(String directoryPath, Int32 offset, Int32? discNumber);

		Task<List<Message>> RemoveApeTagsAsync(String directoryPath);

		Task<List<Message>> SetAlbumArtAsync(String directoryPath, String imageFileName, AlbumArtSetMode mode);
	}

	public class RetaggingOptions
	{
		public RetaggingOptions(String musicRootDirectory, Boolean readOnly, Boolean undo, Func<String,Boolean> directoryFilter, GenreRules genreRules)
		{
			this.MusicRootDirectory = musicRootDirectory ?? throw new ArgumentNullException( nameof( musicRootDirectory ) );
			this.ReadOnly           = readOnly;
			this.Undo               = undo;
			this.DirectoryFilter    = directoryFilter;
			this.GenreRules         = genreRules ?? throw new ArgumentNullException( nameof( genreRules ) );
		}

		public String               MusicRootDirectory { get; }
		public Boolean              ReadOnly           { get; }
		public Boolean              Undo               { get; }
		public Func<String,Boolean> DirectoryFilter    { get; }
		public GenreRules           GenreRules         { get; }
	}

	public enum AlbumArtSetMode
	{
		Replace,
		AddIfMissing
	}

	public class GenreRules
	{
		public GenreDefault Default { get; set; }

		public GenreAssortedFiles AssortedFiles { get; set; }

		public Boolean CompilationUseDefault => !this.CompilationUseArtistName;
		public Boolean CompilationUseArtistName { get; set; }

		public Boolean GuestArtistUseDefault => !this.GuestArtistUseArtistName;
		public Boolean GuestArtistUseArtistName { get; set; }

		public Boolean AlwaysNoop =>
			this.Default == GenreDefault.Preserve &&
			this.AssortedFiles == GenreAssortedFiles.UseDefault &&
			this.CompilationUseDefault &&
			this.GuestArtistUseDefault;
	}

	public enum GenreDefault
	{
		Preserve,
		Clear,
		UseArtist
	}

	public enum GenreAssortedFiles
	{
		UseDefault,
		UseFolderName,
		UseArtistName
	}
}
