using System;
using System.Collections.Generic;
using System.Xml.Linq;
using HugsLib.Core;

namespace HugsLib.Settings {
	/// <summary>
	/// A central place for mods to store persistent settings. Individual settings are grouped by mod using ModSettingsPack
	/// </summary>
	public class ModSettingsManager : PersistentDataManager {
		protected override string FileName {
			get { return "ModSettings.xml"; }
		}

		private readonly List<ModSettingsPack> packs = new List<ModSettingsPack>();
		private readonly Action SettingsChangedCallback;
		
		internal ModSettingsManager(Action settingsChangedCallback) {
			SettingsChangedCallback = settingsChangedCallback;
			LoadData();
		}

		/// <summary>
		/// Retrieves the ModSettingsPack for a given mod identifier.
		/// </summary>
		/// <param name="modId">The unique identifier of the mod that owns the pack</param>
		/// <param name="displayModName">A display name of the mod owning the pack. This will be displayed in the Mod Settings dialog.</param>
		public ModSettingsPack GetModSettings(string modId, string displayModName = null) {
			if(!IsValidElementName(modId)) throw new Exception("Invalid name for mod settings group: "+modId);
			for (int i = 0; i < packs.Count; i++) {
				if (packs[i].ModId == modId) return packs[i];
			}
			var pack = new ModSettingsPack(modId) {EntryName = displayModName};
			packs.Add(pack);
			return pack;
		}

		/// <summary>
		/// Saves all settings to disk and notifies all ModBase mods by calling SettingsChanged() 
		/// </summary>
		public void SaveChanges() {
			SaveData();
			if (SettingsChangedCallback != null) SettingsChangedCallback();
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
			return packs.Remove(GetModSettings(modId));
		}

		public IEnumerable<ModSettingsPack> ModSettingsPacks {
			get { return packs; }
		} 

		protected override void LoadFromXml(XDocument xml) {
			packs.Clear();
			if(xml.Root == null) throw new NullReferenceException("Missing root node");
			foreach (var childNode in xml.Root.Elements()) {
				var pack = new ModSettingsPack(childNode.Name.ToString());
				packs.Add(pack);
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
	}
}