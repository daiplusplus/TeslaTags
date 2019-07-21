using System;
using System.Collections.Generic;

namespace TeslaTags
{
	public static class TagWriter
	{
		public static void SetAlbum( LoadedFile file, List<Message> messages, String newAlbum )
		{
			String oldAlbum = file.Tag.Album;

			if( String.Equals( oldAlbum, newAlbum, StringComparison.Ordinal ) ) return; // NOOP, includes null

			file.Tag.Album = newAlbum;

			messages.AddFileChange( file.FileInfo.FullName, nameof(TagLib.Tag.Album), oldAlbum, newAlbum );
			file.IsModified = true;

			file.RecoveryTag.Album = oldAlbum;
		}

		public static void SetArtist( LoadedFile file, List<Message> messages, String newArtist )
		{
			String oldArtist = file.Tag.GetPerformers();

			if( String.Equals( oldArtist, newArtist, StringComparison.Ordinal ) ) return; // NOOP, includes null

			file.Tag.Performers = ( newArtist == null ) ? null : new String[] { newArtist };

			messages.AddFileChange( file.FileInfo.FullName, nameof(TagLib.Tag.Performers), oldArtist, newArtist );
			file.IsModified = true;

			file.RecoveryTag.Artist = oldArtist;
		}

		public static void SetGenre( LoadedFile file, List<Message> messages, String newGenre )
		{
			if( String.IsNullOrWhiteSpace( newGenre ) ) newGenre = null;

			String oldGenre = file.Tag.JoinedGenres;

			if( newGenre == null && String.IsNullOrWhiteSpace( oldGenre ) ) return; // NOOP

			if( String.Equals( oldGenre, newGenre, StringComparison.Ordinal ) ) return; // NOOP, includes null

			file.Tag.Genres = ( newGenre == null ) ? null : new String[] { newGenre };

			messages.AddFileChange( file.FileInfo.FullName, nameof(TagLib.Tag.Genres), oldGenre, newGenre );
			file.IsModified = true;

			file.RecoveryTag.Genre = oldGenre;
		}

		public static void SetTrackNumber( LoadedFile file, List<Message> messages, UInt32? newTrack )
		{
			UInt32 oldTrack = file.Tag.Track;

			if( oldTrack == newTrack ) return; // NOOP

			UInt32 newTrackValue = newTrack ?? 0;

			file.Tag.Track = newTrackValue; // it's kinda messy to clear tags using TagLib, it's a poorly-designed API (I noticed!): https://stackoverflow.com/questions/21343938/delete-all-pictures-of-an-id3-tag-with-taglib-sharp

			messages.AddFileChange( file.FileInfo.FullName, nameof(TagLib.Tag.Track), oldTrack.ToString(), newTrack.ToString() );
			file.IsModified = true;

			file.RecoveryTag.TrackNumber = (Int32)newTrackValue;
		}

		public static void SetTitle( LoadedFile file, List<Message> messages, String newTitle )
		{
			String oldTitle = file.Tag.Title;

			if( String.Equals( oldTitle, newTitle, StringComparison.Ordinal ) ) return; // NOOP, includes null

			file.Tag.Title = newTitle;

			messages.AddFileChange( file.FileInfo.FullName, nameof(TagLib.Tag.Title), oldTitle, newTitle );
			file.IsModified = true;

			file.RecoveryTag.Title = oldTitle;
		}

		public static void Revert( LoadedFile file, List<Message> messages )
		{
			RecoveryTag rt = file.RecoveryTag;

			if( rt.Artist      != null ) file.Tag.Performers = new String[] { rt.Artist };
			if( rt.Album       != null ) file.Tag.Album      = rt.Album;
			if( rt.TrackNumber != null ) file.Tag.Track      = (UInt32)rt.TrackNumber;
			if( rt.Genre       != null ) file.Tag.Genres     = new String[] { rt.Genre };
			if( rt.Title       != null ) file.Tag.Title      = rt.Title;

			file.RecoveryTag.Clear();
			file.IsModified = true;

			messages.AddInfoFile( file.FileInfo.FullName, "File recovered." );
		}
	}
}
