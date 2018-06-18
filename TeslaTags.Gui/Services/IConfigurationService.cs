using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
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
		public Boolean DesignMode { get; set; }
		public String RootDirectory { get; set; }
		public String[] ExcludeList { get; set; }
		public Boolean HideEmptyDirectories { get; set; }

		public GenreRules GenreRules { get; set; }

		public (Int32 X, Int32 Y, Int32 Width, Int32 Height) RestoredWindowPosition { get; set; }
		public Boolean IsMaximized { get; set; }
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

			XDocument doc = OpenAppConfig();
			if( doc != null )
			{
				PopulateConfig( config, doc );
			}

			this.config = config;
		}

		private static void PopulateConfig(Config config, XDocument doc)
		{
			var appSettings = doc
				.Elements( "configuration" ) // `Elements(XName)` is immediate children, unlike `Descendants(XName)` which does a deep search.
				.Elements( "appSettings" )
				.Elements( "add" )
				.Select( el => ToTuple( el.Attribute("key"), el.Attribute("value") ) )
				.Where( t => !String.IsNullOrWhiteSpace( t.key ) );

			
			config.ExcludeList = new String[0];
			config.GenreRules = new GenreRules();

			foreach( (String key, String value) in appSettings )
			{
				switch( key )
				{
				// UI:
				case "DESIGNMODE":
					if( Boolean.TryParse( value, out Boolean designModeValue ) ) config.DesignMode = designModeValue;
					break;
				
				case "HIDEEMPTYDIRECTORIES":
					if( Boolean.TryParse( value, out Boolean hideEmptyDirectoriesValue ) ) config.HideEmptyDirectories = hideEmptyDirectoriesValue;
					break;

				case "RESTOREDWINDOWPOSITION":
					IList<Int32> values = value
						.Split( _directoryListSeparators, StringSplitOptions.RemoveEmptyEntries )
						.Select( s => Int32.TryParse( s, NumberStyles.Integer, CultureInfo.InvariantCulture, out Int32 v ) ? (Int32?)v : null )
						.Where( v => v != null )
						.Select( v => v.Value )
						.ToList();

					if( values.Count == 4 ) config.RestoredWindowPosition = ( X: values[0], Y: values[1], Width: values[2], Height: values[3] );
					break;
				
				case "ISMAXIMIZED":
					if( Boolean.TryParse( value, out Boolean isMaximizedValue ) ) config.IsMaximized = isMaximizedValue;
					break;

				// Processing settings:
				case "EXCLUDE":
					String[] excludeList = value?.Split( _directoryListSeparators, StringSplitOptions.RemoveEmptyEntries ) ?? null;
					if( excludeList != null ) config.ExcludeList = excludeList;
					break;
				case "ROOTDIRECTORY":
					config.RootDirectory = value;
					break;

				// Genre settings:	
				case "GENREDEFAULT":
					if( Enum.TryParse( value, out GenreDefault genreDefaultValue ) ) config.GenreRules.Default = genreDefaultValue;
					break;
				case "GENREASSORTEDFILES":
					if( Enum.TryParse( value, out GenreAssortedFiles genreAssortedFilesValue ) ) config.GenreRules.AssortedFiles = genreAssortedFilesValue;
					break;
				case "GENRECOMPILATION-USEARTISTNAME":
					if( Boolean.TryParse( value, out Boolean genreCompilationUseArtistNameValue ) ) config.GenreRules.CompilationUseArtistName = genreCompilationUseArtistNameValue;
					break;
				case "GENREGUESTARTIST-USEARTISTNAME":
					if( Boolean.TryParse( value, out Boolean genreGuestArtistUseArtistNameValue ) ) config.GenreRules.GuestArtistUseArtistName = genreGuestArtistUseArtistNameValue;
					break;
				}
			}
		}

		private static (String key, String value) ToTuple( XAttribute keyAttrib, XAttribute valueAttrib )
		{
			String   key       = keyAttrib  ?.Value?.ToUpperInvariant();
			String   value     = valueAttrib?.Value;
			return (key, value);
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

			SetAppSetting( appSettingsDict, nameof(Config.DesignMode)            , config.DesignMode.ToString() );
			SetAppSetting( appSettingsDict, nameof(Config.HideEmptyDirectories)  , config.HideEmptyDirectories.ToString() );
			SetAppSetting( appSettingsDict, nameof(Config.RestoredWindowPosition), restoredWindowPositionValue );
			SetAppSetting( appSettingsDict, nameof(Config.IsMaximized)           , config.IsMaximized.ToString() );
			SetAppSetting( appSettingsDict, nameof(Config.ExcludeList)           , String.Join( ";",  config.ExcludeList.Where( s => !String.IsNullOrWhiteSpace(s) ) ) );
			SetAppSetting( appSettingsDict, nameof(Config.RootDirectory)         , config.RootDirectory );
			SetAppSetting( appSettingsDict, "GenreDefault"                       , config.GenreRules.Default.ToString() );
			SetAppSetting( appSettingsDict, "GenreAssortedFiles"                 , config.GenreRules.AssortedFiles.ToString() );
			SetAppSetting( appSettingsDict, "GenreCompilation-UseArtistName"     , config.GenreRules.CompilationUseArtistName.ToString() );
			SetAppSetting( appSettingsDict, "GenreGuestArtist-UseArtistName"     , config.GenreRules.GuestArtistUseArtistName.ToString() );

			////////////////////////

			doc.Save( AppConfigFileName );
		}

		private static void SetAppSetting(Dictionary<String,XElement> appSettingsDict, String key, String value)
		{
			appSettingsDict.GetAppConfigElement( key ).SetAttributeValue( "value", value );
		}
	}

	internal static partial class Extensions
	{
		public static XElement GetAppConfigElement(this Dictionary<String,XElement> dict, String key)
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
				return value;
			}
		}
	}
}
