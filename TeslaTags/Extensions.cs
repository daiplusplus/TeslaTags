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

		public static Boolean EqualsCI( this String x, String y )
		{
			return String.Equals( x, y, StringComparison.OrdinalIgnoreCase );
		}

		public static void AddInfoFile( this List<Message> messages, String filePath, String text )
		{
			messages.Add( new Message( MessageSeverity.Info, Path.GetDirectoryName( filePath ), filePath, text ) );
		}

		public static void AddInfoDirectory( this List<Message> messages, String directoryPath, String text )
		{
			messages.Add( new Message( MessageSeverity.Info, directoryPath, directoryPath, text ) );
		}

		/// <summary>Returns false if there were no reasons.</summary>
		public static Boolean AddFileCorruptionErrors( this List<Message> messages, String filePath, IEnumerable<String> corruptionReasons )
		{
			if( corruptionReasons == null ) return false;
			
			Boolean any = false;

			foreach( String reason in corruptionReasons )
			{
				messages.AddFileError( filePath, "File corrupted: " + reason );
				any = true;
			}

			return any;
		}

		public static void AddFileWarning( this List<Message> messages, String filePath, String text )
		{
			messages.Add( new Message( MessageSeverity.Warning, Path.GetDirectoryName( filePath ), filePath, text ) );
		}

		public static void AddFileWarning( this List<Message> messages, String filePath, String format, params Object[] args )
		{
			Extensions.AddFileWarning( messages, filePath, text: String.Format( CultureInfo.InvariantCulture, format, args ) );
		}

		public static void AddFileError( this List<Message> messages, String filePath, String text )
		{
			messages.Add( new Message( MessageSeverity.Error, Path.GetDirectoryName( filePath ), filePath, text ) );
		}

		public static void AddFileError( this List<Message> messages, String filePath, String format, params Object[] args )
		{
			Extensions.AddFileError( messages, filePath, text: String.Format( CultureInfo.InvariantCulture, format, args ) );
		}

		public static void AddFileChange( this List<Message> messages, String filePath, String messageText )
		{
			messages.Add( new Message( MessageSeverity.FileModification, Path.GetDirectoryName( filePath ), filePath, messageText ) );
		}

		public static void AddFileChange( this List<Message> messages, String filePath, String field, String oldValue, String newValue )
		{
			oldValue = ( oldValue == null ) ? "null" : ("\"" + oldValue + "\"");
			newValue = ( newValue == null ) ? "null" : ("\"" + newValue + "\"");

			String messageText = String.Concat( field, ": ", oldValue, " -> ", newValue );

			messages.Add( new Message( MessageSeverity.FileModification, Path.GetDirectoryName( filePath ), filePath, messageText ) );
		}

		////

		public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
		{
			if( items != null )
			{
				foreach( T item in items ) collection.Add( item );
			}
		}
	}
}
