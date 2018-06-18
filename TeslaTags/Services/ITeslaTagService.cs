using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Linq;

namespace TeslaTags
{
	public interface ITeslaTagsService
	{
		void Start(String directory, Boolean readOnly, Boolean undo, GenreRules genreRules);
		
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
