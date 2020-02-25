using System;
using Verse;
// ReSharper disable UnassignedField.Global

namespace HugsLib {
	/// <summary>
	/// Describes a single update news item.
	/// Must be defined in the News folder that is placed in the root mod directory of the mod.
	/// </summary>
	public class UpdateFeatureDef : Def {
		/// <summary>
		/// Displayed in the title of the news item
		/// </summary>
		public string modNameReadable;
		/// <summary>
		/// Optional complete replacement for the news item title
		/// </summary>
		public string titleOverride;
		// The minimum assembly version of the ModBase extension that will cause the feature to be displayed (format: major.minor.patch)
		/// <summary>
		/// The version number associated with the news item.
		/// Used to sort news items and determine which items have not been displayed yet.
		/// For example, after an item with version 3.2.1 has been displayed, adding an item with version 3.2.0 will not cause the 
		/// New Mod Features dialog to automatically open. However, both items will still appear the next time the dialog is opened.
		/// </summary>
		public string version;
		/// <summary>
		/// The text of the news item. Can contain text and images, supports Unity html markup (only recommended for highlighting).
		/// The text can contain the following formatting markers:
		/// |               -> (pipe) splits the content into segments. A segment can be a paragraph or image sequence
		/// img:name1,name2 -> Displays a horizontal image sequence. Image names are file names without the extension.
		/// caption:text    -> Attaches a text paragraph on the right side of the preceding image
		/// Everything else is treated as plain text and creates a paragraph.
		/// Example:
		/// Paragraph1|Paragraph2|img:singleImage|caption:caption\ntext|img:sequence1,sequence2|More text
		/// </summary>
		public string content;
		/// <summary>
		/// Optional link to a forum post/info page for this update, or the whole mod. Displayed in the news item title.
		/// </summary>
		public string linkUrl;
		
		// Do no remove these. They ensure compatibility with Rimworld 1.0 where UpdateFeatureDef was loaded from the Defs folder.
		[Obsolete("No longer needs to be specified. Use Def.modContentPack.PackageId to identify which mod added this def. Deprecated since Rimworld 1.1")]
		public string modIdentifier;
		[Obsolete("Use UpdateFeatureDef.version to specify the version of the mod a news feature is about. Deprecated since Rimworld 1.1")]
		public string assemblyVersion;

		/// <summary>
		/// If this is true, we are likely dealing with a def "inherited" through the folder versioning system from 1.0.
		/// Discard these defs and skip verification.
		/// </summary>
		internal bool HasDeprecatedFormat {
#pragma warning disable 618
			get { return modIdentifier != null || assemblyVersion != null; }
#pragma warning restore 618
		}

		public Version VersionParsed { get; set; }

		public override void ResolveReferences() {
			if (HasDeprecatedFormat) return;
			base.ResolveReferences();

			if (defName == null) {
				defName = modNameReadable + version;
			}
			if (modNameReadable == null) ReportError("UpdateFeatureDef.modNameReadable must be set");
			Exception versionFailure = null;
			try {
				if (version == null) throw new Exception("UpdateFeatureDef.version must be defined");
				VersionParsed = new Version(version);
			} catch (Exception e) {
				VersionParsed = new Version();
				versionFailure = e;
			}
			if (versionFailure != null) ReportError("UpdateFeatureDef.version parsing failed: " + versionFailure);
			if (content == null) ReportError("UpdateFeatureDef.content must be set");
		}

		private void ReportError(string message) {
			Log.Error($"UpdateFeatureDef (defName: {defName}) contains invalid data: {message}");
		}
	}
}