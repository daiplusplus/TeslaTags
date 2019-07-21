﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeslaTags
{
	public interface IDirectoryPredicate
	{
		/// <summary>Indicates if the specified <paramref name="directory"/> matches some condition.</summary>
		/// <param name="root">The root directory of a directory search is provided so that if any exclusion criteria match a full path, but only because it's in the root, then the rule won't apply (otherwise every directory would be excluded). E.g. If the root is <c>C:\Users\Dave\Music</c> and you want to exclude Dave Matthews Band, then excluding "dave" would exclude all directories.</param>
		/// <param name="directory">This directory is guaranteed to be a subdirectory of <paramref name="root"/>.</param>
		Boolean Matches( DirectoryInfo root, DirectoryInfo directory );
	}

	public class EmptyDirectoryPredicate : IDirectoryPredicate
	{
		public Boolean Matches( DirectoryInfo root, DirectoryInfo directory )
		{
			return false;
		}
	}

	public class SubstringDirectoryPredicate : IDirectoryPredicate
	{
		private readonly IReadOnlyList<String> substrings;
		private readonly StringComparison      comparison;

		// Accepts an `IEnumerable` and makes a private copy because a mutable list might be passed-in. Can't accept `IReadOnlyList` because that might still be mutable. `IImmutableList<T>` is not in the BCL but is in an extension: System.Collections.Immutable. Boo!
		public SubstringDirectoryPredicate( IEnumerable<String> substrings, StringComparison comparison = StringComparison.OrdinalIgnoreCase )
		{
			this.substrings = substrings == null ? (IReadOnlyList<String>)Array.Empty<String>() : substrings.ToList();
			this.comparison = comparison;
		}

		public Boolean Matches( DirectoryInfo root, DirectoryInfo directory )
		{
			if( root      == null ) throw new ArgumentNullException(nameof(root));
			if( directory == null ) throw new ArgumentNullException(nameof(directory));

			//

			String rootRelativePath = directory.FullName.Substring( startIndex: root.FullName.Length );
			
			return this.substrings.Any( ss => rootRelativePath.IndexOf( ss, this.comparison ) > -1 );
		}
	}

	public class ExactPathComponentMatchPredicate : IDirectoryPredicate
	{
		private readonly HashSet<String> pathComponents;

		public ExactPathComponentMatchPredicate( IEnumerable<String> pathComponents )
		{
			if( pathComponents == null ) pathComponents = Array.Empty<String>();
			this.pathComponents = new HashSet<String>( pathComponents, StringComparer.OrdinalIgnoreCase ); // Case-insensitive on Windows.
		}

		public Boolean Matches( DirectoryInfo root, DirectoryInfo directory )
		{
			if( root      == null ) throw new ArgumentNullException(nameof(root));
			if( directory == null ) throw new ArgumentNullException(nameof(directory));

			//

			DirectoryInfo d = directory;
			while( d != null && d.FullName.Length > root.FullName.Length )
			{
				if( this.pathComponents.Contains( d.Name ) )
				{
					return true;
				}

				d = d.Parent;
			}

			return false;
		}
	}
}