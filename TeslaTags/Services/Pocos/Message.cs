using System;

namespace TeslaTags
{
	public class Message
	{
		public Message(MessageSeverity severity, String directory, String path, String text)
		{
			this.Severity     = severity;
			this.FullPath     = path ?? throw new ArgumentNullException(nameof(path));
			this.IsDirectory  = String.Equals( directory, path, StringComparison.OrdinalIgnoreCase );
			this.RelativePath = path.StartsWith( directory, StringComparison.OrdinalIgnoreCase ) ? path.Substring( directory.Length ) : path;
			this.Text         = text ?? throw new ArgumentNullException(nameof(text));
		}

		public MessageSeverity Severity     { get; }
		public String          FullPath     { get; }
		public String          RelativePath { get; }
		public Boolean         IsDirectory  { get; }
		public String          Text         { get; }

		private String toString;

		public override String ToString()
		{
			return this.toString ?? ( this.toString = String.Concat( this.FullPath, "\t", this.Severity.ToString(), "\t", this.Text ) );
		}
	}

	public enum MessageSeverity
	{
		Info,
		FileModification,
		Warning,
		Error
	}
}
