using System;
using System.IO;
using System.Xml.Linq;
using Verse;

namespace HugsLib.Core {
	/// <summary>
	/// Provides support for reading version information from Manifest.xml files.
	/// These files are used in mods by Fluffy and a a few other authors.
	/// </summary>
	public class ManifestFile {
		public const string ManifestFileDir = "About";
		public const string ManifestFileName = "Manifest.xml";

		/// <summary>
		/// Attempts to read and parse the manifest file for a mod.
		/// </summary>
		/// <returns>
		/// Returns null if reading or parsing fails for any reason.
		/// </returns>
		public static ManifestFile? TryParse(ModContentPack? pack, bool logError = true) {
			if (pack != null) {
				try {
					return Parse(pack);
				} catch (Exception e) {
					if (logError) {
						HugsLibController.Logger.Error("Exception while parsing manifest file:\n" +
														$"packageId:{pack.PackageIdPlayerFacing}, " +
														$"path:{GetManifestFilePath(pack)}, exception:{e}");
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Reads and parses the manifest file for a mod.
		/// </summary>
		/// <returns>
		/// Returns null if the file does not exist.
		/// </returns>
		public static ManifestFile? Parse(ModContentPack pack) {
			if (pack == null) throw new ArgumentNullException(nameof(pack));
			var filePath = GetManifestFilePath(pack);
			if (File.Exists(filePath)) {
				var doc = XDocument.Load(filePath);
				return new ManifestFile(doc);
			}
			return null;
		}

		private static string GetManifestFilePath(ModContentPack pack) {
			return Path.Combine(pack.RootDir, Path.Combine(ManifestFileDir, ManifestFileName));
		}

		public Version? Version { get; private set; }

		private ManifestFile(XDocument doc) {
			ParseXmlDocument(doc);
		}

		private void ParseXmlDocument(XDocument doc) {
			if (doc.Root == null) {
				throw new Exception("Missing root node.");
			}
			var versionElement = doc.Root.Element("version") ?? doc.Root.Element("Version");
			if (versionElement != null) {
				try {
					Version = Version.Parse(versionElement.Value);
				} catch (Exception e) {
					throw new Exception("Failed to parse version tag.", e);
				}
			}
		}
	}
}