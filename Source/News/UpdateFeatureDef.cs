using System;
using HugsLib.Core;
using Verse;
// ReSharper disable UnassignedField.Global CheckNamespace

namespace HugsLib {
	/// <summary>
	/// Describes a single update news item.
	/// Recommended to be placed in the /News folder in the root directory of the mod.
	/// Can be loaded from the /Defs folder, but any <see cref="UpdateFeatureDef"/> placed 
	/// in the /News  folder will unload all <see cref="UpdateFeatureDef"/>s loaded from /Defs.
	/// </summary>
	public class UpdateFeatureDef : Def {
		/// <summary>
		/// An optional unique identifier to reference the mod that adds this news item.
		/// If not set, the PackageId of the mod will be used.<para/>
		/// Must start with a letter and contain any of [A-z, 0-9, -, _, :]
		/// </summary>
		/// <remarks>
		/// Used to preserve compatibility with pre-RW1.1 HugsLib news data, such as already displayed news items and ignored news providers.<para/>
		/// Previously used to reference a BodBase.ModIdentifier which had to be loaded for the defining news def to be displayed.
		/// </remarks>
		public string modIdentifier;
		/// <summary>
		/// Displayed in the title of the news item
		/// </summary>
		public string modNameReadable;
		/// <summary>
		/// Optional complete replacement for the news item title
		/// </summary>
		public string titleOverride;
		/// <summary>
		/// The version number associated with the news item. Format: major.minor.patch<para/>
		/// Used to sort news items and determine which items have not been displayed yet.
		/// </summary>
		/// <remarks>
		/// For example, after an item with version 3.2.1 has been displayed, adding an item with version 3.2.0 will not cause the 
		/// New Mod Features dialog to automatically open. However, both items will still appear the next time the dialog is opened.<para/>
		/// The version of the mod adding the news item is no longer required to be equal or higher for a news item to be displayed.
		/// </remarks>
		public string assemblyVersion;
		/// <summary>
		/// The text of the news item. Can contain text and images, supports Unity html markup (only recommended for highlighting).<para/>
		/// The text can contain the following formatting markers:<para/>
		/// |               -> (pipe) splits the content into segments. A segment can be a paragraph or image sequence<para/>
		/// img:name1,name2 -> Displays a horizontal image sequence. Image names are file names without the extension.<para/>
		/// caption:text    -> Attaches a text paragraph on the right side of the preceding image<para/>
		/// Everything else is treated as plain text and creates a paragraph.<para/>
		/// </summary>
		/// <example>Paragraph1|Paragraph2|img:singleImage|caption:caption\ntext|img:sequence1,sequence2|More text</example>
		public string content;
		/// <summary>
		/// When set to true (true by default), leading and trailing whitespace characters (spaces, tabs, newlines)
		/// are removed from content captions and paragraphs.
		/// This makes it easier lay out your content and not have to cram everything into one line.
		/// </summary>
		public bool trimWhitespace = true;
		/// <summary>
		/// Optional link to a forum post/info page for this update, or the whole mod. Displayed in the news item title.
		/// </summary>
		public string linkUrl;
		
		public Version Version { get; set; }

		/// <summary>
		/// Returns the Id of the owning mod. 
		/// <see cref="modIdentifier"/> is used if defined, and ModContentPack.PackageId otherwise.
		/// </summary>
		public string OwningModId {
			get {
				if(!modIdentifier.NullOrEmpty()) return modIdentifier;
				return modContentPack?.PackageId;
			}
		}

		public override void ResolveReferences() {
			base.ResolveReferences();
			try {
				if (!modIdentifier.NullOrEmpty() && !PersistentDataManager.IsValidElementName(modIdentifier)) {
					throw new Exception($"{nameof(modIdentifier)} value has invalid format. " +
										"Must start with a letter and contain any of [A-z, 0-9, -, _, :].");
				}
				if (defName == null) {
					defName = modIdentifier + assemblyVersion;
				}
				if (modNameReadable == null) {
					throw new Exception($"{nameof(modNameReadable)} value must be set.");
				}
				try {
					if (string.IsNullOrEmpty(assemblyVersion)) {
						throw new Exception("No value specified.");
					}
					Version = new Version(assemblyVersion);
				} catch (Exception e) {
					Version = new Version();
					throw new Exception($"{nameof(assemblyVersion)} value is not valid.", e);
				}
				if (content == null) {
					throw new Exception($"{nameof(content)} value must be set.");
				}
			} catch (Exception e) {
				throw new Exception($"{nameof(UpdateFeatureDef)} contains invalid data.", e);
			}
		}
	}
}