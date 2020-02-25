// ManifestFile.cs
// Copyright Karel Kroeze, 2020-2020

using System;
using System.IO;
using System.Xml.Linq;
using Verse;

namespace HugsLib.Utils
{
    public class ManifestFile
    {

        public const string ManifestFileDir  = "About";
        public const string ManifestFileName = "Manifest.xml";

        public static ManifestFile TryParse( ModContentPack pack )
        {
            var filePath = Path.Combine( pack.RootDir, Path.Combine( ManifestFileDir, ManifestFileName ) );
            if ( !File.Exists( filePath ) ) return null;
            try
            {
                var doc = XDocument.Load( filePath );
                return new ManifestFile( doc );
            }
            catch ( Exception e )
            {
                HugsLibController.Logger.Error( "Exception while parsing manifest file at path: " + filePath +
                                                " Exception was: "                                + e );
            }

            return null;
        }

        public Version Version { get; private set; }

        private ManifestFile( XDocument doc )
        {
            ParseXmlDocument( doc );
        }

        private void ParseXmlDocument( XDocument doc )
        {
            if ( doc.Root == null ) throw new Exception( "Missing root node" );
            var versionElement = doc.Root.Element( "Version" );
            if ( versionElement != null )
            {
                Version = new Version( versionElement.Value );
            }
        }
    }
}
