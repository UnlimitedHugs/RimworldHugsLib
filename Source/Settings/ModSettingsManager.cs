using System;
using System.Collections.Generic;
using System.Xml.Linq;
using HugsLib.Core;
using HugsLib.Utils;

namespace HugsLib.Settings {
	/// <summary>
	/// A central place for mods to store persistent settings. Individual settings are grouped by mod using ModSettingsPack
	/// </summary>
	public class ModSettingsManager : PersistentDataManager {
		protected override string FileName {
			get { return "ModSettings.xml"; }
		}

		/// <summary>
		/// Fires when <see cref="SaveChanges"/> is called and changes are about to be saved.
		/// Use <see cref="ModSettingsPacks"/> and <see cref="ModSettingsPack.HasUnsavedChanges"/> to identify changed packs,
		/// and <see cref="ModSettingsPack.Handles"/> with <see cref="SettingHandle.HasUnsavedChanges"/> to identify changed handles.
		/// </summary>
		public event Action BeforeModSettingsSaved;
		/// <summary>
		/// Fires when <see cref="SaveChanges"/> is called and the settings file has just been written to disk.
		/// </summary>
		public event Action AfterModSettingsSaved;

		private readonly List<ModSettingsPack> packs = new List<ModSettingsPack>();
		
		/// <summary>
		/// Enumerates the <see cref="ModSettingsPack"/>s that have been registered up to this point.
		/// </summary>
		public IEnumerable<ModSettingsPack> ModSettingsPacks {
			get { return packs; }
		}
		/// <summary>
		/// Returns true when there are handles with values that have changed since the last time settings were saved.
		/// </summary>
		public bool HasUnsavedChanges {
			get {
				for (var i = 0; i < packs.Count; i++) {
					if (packs[i].HasUnsavedChanges) return true;
				}
				return false;
			}
		}

		internal ModSettingsManager() {
			LoadData();
		}

		internal ModSettingsManager(string overrideFilePath, IModLogger logger) {
			OverrideFilePath = overrideFilePath;
			DataManagerLogger = logger;
			LoadData();
		}

		/// <summary>
		/// Retrieves the <see cref="ModSettingsPack"/> for a given mod identifier.
		/// </summary>
		/// <param name="modId">The unique identifier of the mod that owns the pack</param>
		/// <param name="displayModName">If not null, assigns the <see cref="ModSettingsPack.EntryName"/> property of the pack.
		/// This will be displayed in the Mod Settings dialog as a header.</param>
		public ModSettingsPack GetModSettings(string modId, string displayModName = null) {
			if(!IsValidElementName(modId)) throw new Exception("Invalid name for mod settings group: "+modId);
			ModSettingsPack pack = null;
			for (int i = 0; i < packs.Count; i++) {
				if (packs[i].ModId == modId) {
					pack = packs[i];
					break;
				}
			}
			if (pack == null) {
				pack = InstantiatePack(modId);
			}
			if (displayModName != null) {
				pack.EntryName = displayModName;
			}
			return pack;
		}

		/// <summary>
		/// Saves all settings to disk and notifies all ModBase mods by calling SettingsChanged() 
		/// </summary>
		public void SaveChanges() {
			if (!HasUnsavedChanges) return;
			try {
				BeforeModSettingsSaved?.Invoke();
			} catch (Exception e) {
				HugsLibController.Logger.ReportException(e);
			}
			SaveData();
			try {
				AfterModSettingsSaved?.Invoke();
			} catch (Exception e) {
				HugsLibController.Logger.ReportException(e);
			}
		}

		public bool HasSettingsForMod(string modId) {
			return packs.Find(p => p.ModId == modId) != null;
		}

		/// <summary>
		/// Removes a settings pack for a mod if it exists. Use SaveChanges to apply the change afterward.
		/// </summary>
		/// <param name="modId">The identifier of the mod owning the pack</param>
		public bool TryRemoveModSettings(string modId) {
			var pack = packs.Find(p => p.ModId == modId);
			if (pack == null) return false;
			if (packs.Remove(GetModSettings(modId))) {
				return true;
			}
			return false;
		}

		protected override void LoadFromXml(XDocument xml) {
			packs.Clear();
			if(xml.Root == null) throw new NullReferenceException("Missing root node");
			foreach (var childNode in xml.Root.Elements()) {
				var pack = InstantiatePack(childNode.Name.ToString());
				pack.LoadFromXml(childNode);
			}
		}

		protected override void WriteXml(XDocument xml) {
			var root = new XElement("settings");
			xml.Add(root);
			foreach (var modSettingPack in packs) {
				modSettingPack.WriteXml(root);
			}
		}

		private ModSettingsPack InstantiatePack(string modId) {
			var pack = new ModSettingsPack(modId) {
				ParentManager = this
			};
			packs.Add(pack);
			return pack;
		}
	}
}