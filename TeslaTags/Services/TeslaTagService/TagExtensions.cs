using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TagLib;

namespace TeslaTags
{
	public static class TagExtensions
	{
		// Workaround for the fact TagLib and ID3v2 split on '/' when that may be undesirable, e.g. "AC/DC".

		public static String GetPerformers( this Tag tag )
		{
			if( tag == null ) throw new ArgumentNullException(nameof(tag));

			return GetOriginalTagValue( tag, tag.Performers );
		}

		public static String GetAlbumArtist( this Tag tag )
		{
			if( tag == null ) throw new ArgumentNullException(nameof(tag));

			return GetOriginalTagValue( tag, tag.AlbumArtists );
		}

		private static String GetOriginalTagValue( Tag tag, String[] tagValue )
		{
			// Note the property accessors always allocates a new array on every call, wow.

			if( tagValue.Length == 1 ) return tagValue[0];

			if( tag is global::TagLib.Id3v2.Tag  )
			{
				// The ID3Tagv2 in TagLibSharp always splits on '/' for Performers, AlbumArtist, Conductor and Composer.
				// But does NOT split on Genre.
				// See `TagLib.Id3v2.TextInformationFrame.ParseRawData()`

				return String.Join( "/", tagValue );
			}
			else if( tag is global::TagLib.Id3v1.Tag )
			{
				return String.Join( ";", tagValue );
			}
			else
			{
				// Uhhhh... what should the default fallback be?
				// I guess semi-colon separated too until we know better.
				return String.Join( ";", tagValue );
			}
		}
	}
}
