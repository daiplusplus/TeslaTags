using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeslaTags
{
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
}
