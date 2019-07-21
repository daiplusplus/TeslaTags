using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace TeslaTags
{
	public static partial class MessageExtensions
	{
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
			MessageExtensions.AddFileWarning( messages, filePath, text: String.Format( CultureInfo.InvariantCulture, format, args ) );
		}

		public static void AddFileError( this List<Message> messages, String filePath, String text )
		{
			messages.Add( new Message( MessageSeverity.Error, Path.GetDirectoryName( filePath ), filePath, text ) );
		}

		public static void AddFileError( this List<Message> messages, String filePath, String format, params Object[] args )
		{
			MessageExtensions.AddFileError( messages, filePath, text: String.Format( CultureInfo.InvariantCulture, format, args ) );
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

		public static Int32 GetModifiedFileCount( this IEnumerable<Message> messages )
		{
			if( messages == null ) throw new ArgumentNullException(nameof(messages));

			Int32 modifiedFileCount = messages
				.GroupBy( m => m.FullPath )
				.Where( grp => grp.Any( m => m.Severity == MessageSeverity.FileModification ) )
				.Count();

			return modifiedFileCount;
		}
	}
}
