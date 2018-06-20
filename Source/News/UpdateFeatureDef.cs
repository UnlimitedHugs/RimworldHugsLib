using System;
using HugsLib.Core;
using Verse;
// ReSharper disable UnassignedField.Global

namespace HugsLib {
	/// <summary>
	/// Describes a single update news item. A mod must have a class extending ModBase and keep its assembly version up to date to make use of this.
	/// </summary>
	public class UpdateFeatureDef : Def {
		// The ModBase.ModIdentifier of the mod that this def belongs to
		public string modIdentifier;
		// Displayed in the title of the news item
		public string modNameReadable;
		// Optional complete replacement for the news item title
		public string titleOverride;
		// The minimum assembly version of the ModBase extension that will cause the feature to be displayed (format: major.minor.patch)
		public string assemblyVersion;
		/**
		 * The text of the news item. Can contain text and images, supports Unity html markup (only recommended for highlighting).
		 * The text can contain the following formatting markers:
		 * |               -> (pipe) splits the content into segments. A segment can be a paragraph or image sequence
		 * img:name1,name2 -> Displays a horizontal image sequence. Image names are file names without the extension.
		 * caption:text    -> Attaches a text paragraph on the right side of the preceding image
		 * Everything else is treated as plain text and creates a paragraph.
		 * Example:
		 * Paragraph1|Paragraph2|img:singleImage|caption:caption\ntext|img:sequence1,sequence2|More text
		 */
		public string content;
		// optional link to a forum post/info page for this update, or the whole mod. Displayed in the news item title.
		public string linkUrl;

		public Version Version { get; set; }

		public override void ResolveReferences() {
			base.ResolveReferences();
			if (defName == null) {
				defName = modIdentifier + assemblyVersion;
			}
			if (modIdentifier == null) ReportError("UpdateFeatureDef.modIdentifier must be set");
			if (modNameReadable == null) ReportError("UpdateFeatureDef.modNameReadable must be set");
			Exception versionFailure = null;
			try {
				if (assemblyVersion == null) throw new Exception("UpdateFeatureDef.assemblyVersion must be defined");
				Version = new Version(assemblyVersion);
			} catch (Exception e) {
				Version = new Version();
				versionFailure = e;
			}
			if (versionFailure != null) ReportError("UpdateFeatureDef.version parsing failed: " + versionFailure);
			if (content == null) ReportError("UpdateFeatureDef.content must be set");
		}

		private void ReportError(string message) {
			Log.Error(string.Format("UpdateFeatureDef (defName: {0}) contains invalid data: {1}", defName, message));
		}
	}
}