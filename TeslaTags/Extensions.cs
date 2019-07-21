using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace TeslaTags
{
	public static partial class Extensions
	{
		public static String FormatInvariant(this String format, Object arg0)
		{
			return String.Format( CultureInfo.InvariantCulture, format, arg0 );
		}

		public static String FormatInvariant(this String format, Object arg0, Object arg1)
		{
			return String.Format( CultureInfo.InvariantCulture, format, arg0, arg1 );
		}

		public static String FormatInvariant(this String format, Object arg0, Object arg1, Object arg2)
		{
			return String.Format( CultureInfo.InvariantCulture, format, arg0, arg1, arg2 );
		}

		public static String FormatInvariant(this String format, params Object[] args)
		{
			return String.Format( CultureInfo.InvariantCulture, format, args );
		}

		/// <summary>Performs an ordinal case-insensitive equality check.</summary>
		public static Boolean EqualsCI( this String x, String y )
		{
			return String.Equals( x, y, StringComparison.OrdinalIgnoreCase );
		}

		public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
		{
			if( items != null )
			{
				foreach( T item in items ) collection.Add( item );
			}
		}
	}
}
