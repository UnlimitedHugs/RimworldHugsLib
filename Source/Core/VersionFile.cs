using System;
using System.IO;
using System.Xml.Linq;
using Verse;

namespace HugsLib.Core {
	/// <summary>
	/// Represents the information stored in the About/Version.xml file. 
	/// Since we cannot update the version of the library assembly, we have to store the version externally.
	/// </summary>
	public class VersionFile {
		public const string VersionFileDir = "About";
		public const string VersionFileName = "Version.xml";

		public static VersionFile TryParse(ModContentPack pack) {
			var filePath = Path.Combine(pack.RootDir, Path.Combine(VersionFileDir, VersionFileName));
			if (!File.Exists(filePath)) return null;
			try {
				var doc = XDocument.Load(filePath);
				return new VersionFile(doc);
			} catch (Exception e) {
				HugsLibController.Logger.Error("Exception while parsing version file at path: " + filePath + " Exception was: " + e);
			}
			return null;
		}

		public Version OverrideVersion { get; private set; }
		public Version RequiredLibraryVersion { get; private set; }

		private VersionFile(XDocument doc) {
			ParseXmlDocument(doc);
		}

		private void ParseXmlDocument(XDocument doc) {
			if (doc.Root == null) throw new Exception("Missing root node");
			var overrideVersionElement = doc.Root.Element("overrideVersion");
			if (overrideVersionElement != null) {
				OverrideVersion = new Version(overrideVersionElement.Value);
			}
			var requiredLibraryVersionElement = doc.Root.Element("requiredLibraryVersion");
			if (requiredLibraryVersionElement != null) {
				RequiredLibraryVersion = new Version(requiredLibraryVersionElement.Value);
			}
		}
	}
}