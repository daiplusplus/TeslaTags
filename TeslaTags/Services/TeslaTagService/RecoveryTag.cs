using System;

namespace TeslaTags
{
	public class RecoveryTag
	{
		public String Album       { get; set; }
		public String Artist      { get; set; }
		public String Genre       { get; set; }
		public Int32? TrackNumber { get; set; }
		public String Title       { get; set; }

		public Boolean IsEmpty =>
			this.Album       == null &&
			this.Artist      == null &&
			this.Genre       == null &&
			this.TrackNumber == null &&
			this.Title       == null;

		public Boolean IsSet => !this.IsEmpty;

		public void Clear()
		{
			this.Album       = null;
			this.Artist      = null;
			this.Genre       = null;
			this.TrackNumber = null;
			this.Title       = null;
		}
	}
}
