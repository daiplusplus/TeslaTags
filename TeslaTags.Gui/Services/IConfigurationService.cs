using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace TeslaTags.Gui
{
	public interface IConfigurationService
	{
		//Task LoadConfigAsync();
		void LoadConfig();

		Config Config { get; }

		//Task SaveConfigAsync(Config config);
		void SaveConfig(Config config);
	}

	public class Config
	{
		public Boolean    DesignMode           { get; set; }

		public String     RootDirectory        { get; set; }

		public String[]   ExcludeList          { get; set; }
		public String[]   FileExtensions       { get; set; }

		public Boolean    HideEmptyDirectories { get; set; }

		public GenreRules GenreRules           { get; set; }

		public (Int32 X, Int32 Y, Int32 Width, Int32 Height) RestoredWindowPosition { get; set; }

		public Boolean    IsMaximized          { get; set; }
	}

	public class XmlDocumentConfigurationService : IConfigurationService
	{
		private XmlDocumentConfigurationService()
		{
		}

		public static XmlDocumentConfigurationService Instance { get; } = new XmlDocumentConfigurationService();

		private static readonly Char[] _directoryListSeparators = new Char[] { ';' };

		private Config config;
		public Config Config
		{
			get
			{
				if( this.config == null ) throw new InvalidOperationException( "Configuration hasn't been loaded." );
				return this.config;
			}
		}

		public void LoadConfig()
		{
			Config config = new Config();

			// Set defaults:
			config.ExcludeList    = FileSystemPredicate.DefaultExcludeFolders.ToArray();
			config.FileExtensions = FileSystemPredicate.DefaultAudioFileExtensions.ToArray();

			XDocument doc = OpenAppConfig();
			if( doc != null )
			{
				PopulateConfig( config, doc );
			}

			this.config = config;
		}

		private static void PopulateConfig(Config config, XDocument doc)
		{
			Boolean E(String key, String name) => String.Equals( key, name, StringComparison.OrdinalIgnoreCase );

			var appSettings = doc
				.Elements( "configuration" ) // `Elements(XName)` is immediate children, unlike `Descendants(XName)` which does a deep search.
				.Elements( "appSettings" )
				.Elements( "add" )
				.Select( el => (key: el.Attribute("key")?.Value, value: el.Attribute("value")?.Value ) )
				.Where( t => !String.IsNullOrWhiteSpace( t.key ) );
			
			config.GenreRules = new GenreRules();

			foreach( (String key, String value) in appSettings )
			{
				if( E( key, nameof(TeslaTags.Gui.Config.DesignMode) ) )
				{
					if( Boolean.TryParse( value, out Boolean designModeValue ) ) config.DesignMode = designModeValue;
				}
				else if( E( key, nameof(TeslaTags.Gui.Config.HideEmptyDirectories) ) )
				{
					if( Boolean.TryParse( value, out Boolean hideEmptyDirectoriesValue ) ) config.HideEmptyDirectories = hideEmptyDirectoriesValue;
				}
				else if( E( key, nameof(TeslaTags.Gui.Config.RestoredWindowPosition) ) )
				{
					IList<Int32> values = value
						.Split( _directoryListSeparators, StringSplitOptions.RemoveEmptyEntries )
						.Select( s => Int32.TryParse( s, NumberStyles.Integer, CultureInfo.InvariantCulture, out Int32 v ) ? (Int32?)v : null )
						.Where( v => v != null )
						.Select( v => v.Value )
						.ToList();

					if( values.Count == 4 ) config.RestoredWindowPosition = ( X: values[0], Y: values[1], Width: values[2], Height: values[3] );
				}
				else if( E( key, nameof(TeslaTags.Gui.Config.IsMaximized) ) )
				{
					if( Boolean.TryParse( value, out Boolean isMaximizedValue ) ) config.IsMaximized = isMaximizedValue;
				}
				else if( E( key, nameof(TeslaTags.Gui.Config.ExcludeList) ) )
				{
					String[] excludeList = value?.Split( _directoryListSeparators, StringSplitOptions.RemoveEmptyEntries ) ?? null;
					if( excludeList != null && excludeList.Length > 0 ) config.ExcludeList = excludeList;
				}
				else if( E( key, nameof(TeslaTags.Gui.Config.FileExtensions) ) )
				{
					String[] extensionList = value?.Split( _directoryListSeparators, StringSplitOptions.RemoveEmptyEntries ) ?? null;
					if( extensionList != null && extensionList.Length > 0 ) config.FileExtensions = extensionList;
				}
				else if( E( key, nameof(TeslaTags.Gui.Config.RootDirectory) ) )
				{
					config.RootDirectory = value;
				}

				// Genre rules:

				else if( E( key, nameof(TeslaTags.Gui.Config.GenreRules) + "_2" + nameof(TeslaTags.Gui.Config.GenreRules.AssortedFilesAction) ) ) // "_2" because the config schema is different than how it was prior to Release 7.
				{
					if( Enum.TryParse( value, out AssortedFilesGenreAction genreActionValue ) ) config.GenreRules.AssortedFilesAction = genreActionValue;
				}
				else if( E( key, nameof(TeslaTags.Gui.Config.GenreRules) + "_2" + nameof(TeslaTags.Gui.Config.GenreRules.ArtistAlbumWithGuestArtistsAction) ) )
				{
					if( Enum.TryParse( value, out GenreAction genreActionValue ) ) config.GenreRules.ArtistAlbumWithGuestArtistsAction = genreActionValue;
				}
				else if( E( key, nameof(TeslaTags.Gui.Config.GenreRules) + "_2" + nameof(TeslaTags.Gui.Config.GenreRules.ArtistAssortedAction) ) )
				{
					if( Enum.TryParse( value, out GenreAction genreActionValue ) ) config.GenreRules.ArtistAssortedAction = genreActionValue;
				}
				else if( E( key, nameof(TeslaTags.Gui.Config.GenreRules) + "_2" + nameof(TeslaTags.Gui.Config.GenreRules.ArtistAlbumAction) ) )
				{
					if( Enum.TryParse( value, out GenreAction genreActionValue ) ) config.GenreRules.ArtistAlbumAction = genreActionValue;
				}
				else if( E( key, nameof(TeslaTags.Gui.Config.GenreRules) + "_2" + nameof(TeslaTags.Gui.Config.GenreRules.CompilationAlbumAction) ) )
				{
					if( Enum.TryParse( value, out GenreAction genreActionValue ) ) config.GenreRules.CompilationAlbumAction = genreActionValue;
				}
			}

			if( config.ExcludeList == null ) config.ExcludeList = new String[0];
		}

		private static String AppConfigFileName => AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;

		private static XDocument OpenAppConfig()
		{
			if( !File.Exists( AppConfigFileName ) ) return null;

			XDocument doc = XDocument.Load( AppConfigFileName, LoadOptions.PreserveWhitespace );
			return doc;
		}

		public void SaveConfig(Config config)
		{
			XDocument doc = OpenAppConfig();
			if( doc == null ) doc = new XDocument();

			XElement appSettingsElement = doc
				.Elements( "configuration" ) // `Elements(XName)` is immediate children, unlike `Descendants(XName)` which does a deep search.
				.Elements( "appSettings" )
				.SingleOrDefault();

			if( appSettingsElement == null )
			{
				XElement configurationElement = doc.Elements("configuration").SingleOrDefault();
				if( configurationElement == null )
				{
					configurationElement = new XElement("configuration");
					doc.AddFirst( configurationElement );
				}

				appSettingsElement = new XElement( "appSettings" );
				configurationElement.Add( appSettingsElement );
			}

			Dictionary<String,XElement> appSettingsDict = appSettingsElement
				.Elements( "add" )
				.Select( el => ( key: el.Attribute("key")?.Value, element: el ) )
				.Where( t => !String.IsNullOrWhiteSpace( t.key ) )
				.ToDictionary( t => t.key, t => t.element, StringComparer.OrdinalIgnoreCase );

			////////////////////////

			String restoredWindowPositionValue = String.Format(
				CultureInfo.InvariantCulture,
				"{0};{1};{2};{3}",
				config.RestoredWindowPosition.X,
				config.RestoredWindowPosition.Y,
				config.RestoredWindowPosition.Width,
				config.RestoredWindowPosition.Height
			);

			SetAppSetting( appSettingsElement, appSettingsDict, nameof(TeslaTags.Gui.Config.DesignMode)            , config.DesignMode.ToString() );
			SetAppSetting( appSettingsElement, appSettingsDict, nameof(TeslaTags.Gui.Config.HideEmptyDirectories)  , config.HideEmptyDirectories.ToString() );
			SetAppSetting( appSettingsElement, appSettingsDict, nameof(TeslaTags.Gui.Config.RestoredWindowPosition), restoredWindowPositionValue );
			SetAppSetting( appSettingsElement, appSettingsDict, nameof(TeslaTags.Gui.Config.IsMaximized)           , config.IsMaximized.ToString() );
			SetAppSetting( appSettingsElement, appSettingsDict, nameof(TeslaTags.Gui.Config.ExcludeList)           , String.Join( ";",  config.ExcludeList   .Where( s => !String.IsNullOrWhiteSpace(s) ).OrderBy( s => s ) ) );
			SetAppSetting( appSettingsElement, appSettingsDict, nameof(TeslaTags.Gui.Config.FileExtensions)        , String.Join( ";",  config.FileExtensions.Where( s => !String.IsNullOrWhiteSpace(s) ).OrderBy( s => s ) ) );
			SetAppSetting( appSettingsElement, appSettingsDict, nameof(TeslaTags.Gui.Config.RootDirectory)         , config.RootDirectory );

			// Genre rules, version 2 (Release 7+)
			SetAppSetting( appSettingsElement, appSettingsDict, nameof(TeslaTags.Gui.Config.GenreRules) + "_2" + nameof(TeslaTags.Gui.Config.GenreRules.AssortedFilesAction              ), config.GenreRules.AssortedFilesAction              .ToString() );
			SetAppSetting( appSettingsElement, appSettingsDict, nameof(TeslaTags.Gui.Config.GenreRules) + "_2" + nameof(TeslaTags.Gui.Config.GenreRules.ArtistAlbumWithGuestArtistsAction), config.GenreRules.ArtistAlbumWithGuestArtistsAction.ToString() );
			SetAppSetting( appSettingsElement, appSettingsDict, nameof(TeslaTags.Gui.Config.GenreRules) + "_2" + nameof(TeslaTags.Gui.Config.GenreRules.ArtistAssortedAction             ), config.GenreRules.ArtistAssortedAction             .ToString() );
			SetAppSetting( appSettingsElement, appSettingsDict, nameof(TeslaTags.Gui.Config.GenreRules) + "_2" + nameof(TeslaTags.Gui.Config.GenreRules.ArtistAlbumAction                ), config.GenreRules.ArtistAlbumAction                .ToString() );
			SetAppSetting( appSettingsElement, appSettingsDict, nameof(TeslaTags.Gui.Config.GenreRules) + "_2" + nameof(TeslaTags.Gui.Config.GenreRules.CompilationAlbumAction           ), config.GenreRules.CompilationAlbumAction           .ToString() );

			////////////////////////

			doc.Save( AppConfigFileName );
			
			// https://stackoverflow.com/questions/18500419/how-to-change-number-of-characters-used-for-indentation-when-writing-xml-with-xd
			// - this doesn't seem to work well?
			/*XmlWriterSettings settings = new XmlWriterSettings();
			settings.Indent = true;
			settings.IndentChars = "\t";

			using( XmlWriter writer = XmlWriter.Create( AppConfigFileName + ".xml", settings ) )
			{
				doc.Save( writer );
			}*/
		}

		private static void SetAppSetting(XElement appSettingsElement, Dictionary<String,XElement> appSettingsDict, String key, String value)
		{
			XElement appConfigElement = GetAppConfigKeyValueElement( appSettingsDict, key, appSettingsElement );

			appConfigElement.SetAttributeValue( "value", value );
		}

		private static XElement GetAppConfigKeyValueElement(Dictionary<String,XElement> dict, String key, XElement appSettingsElement)
		{
			if( dict.TryGetValue( key, out XElement value ) )
			{
				return value;
			}
			else
			{
				value = new XElement("add");
				value.SetAttributeValue( "key", key );
				dict.Add( key, value );

				appSettingsElement.Add( new XText( "\r\n\t\t" ) );
				appSettingsElement.Add( value );

				return value;
			}
		}
	}
}
