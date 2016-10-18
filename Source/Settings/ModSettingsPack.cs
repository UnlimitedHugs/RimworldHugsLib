using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Verse;

namespace HugsLib.Settings {
	/**
	 * A group of settings values added by a mod. Each mod has their own ModSettingsPack
	 * Loaded values are stored until they are "claimed" by their mod by requesting a handle for a setting with the same name
	 */
	public class ModSettingsPack {
		public enum ListPriority {
			Higher, Normal, Lower
		}

		public string ModId { get; private set; }
		public string EntryName { get; internal set; }
		public ListPriority DisplayPriority { get; set; }

		private readonly Dictionary<string, string> loadedValues = new Dictionary<string, string>();
		private readonly List<SettingHandle> handles = new List<SettingHandle>();

		public ModSettingsPack(string modId) {
			ModId = modId;
			DisplayPriority = ListPriority.Normal;
		}

		public SettingHandle<T> GetHandle<T>(string settingName, string title, string description, T defaultValue = default(T), SettingHandle.ValueIsValid validator = null, string enumPrefix = null) {
			if (!PersistentDataManager.IsValidElementName(settingName)) throw new Exception("Invalid name for mod setting: " + settingName);
			SettingHandle<T> handle = null;
			for (int i = 0; i < handles.Count; i++) {
				if (handles[i].Name != settingName) continue;
				if (!(handles[i] is SettingHandle<T>)) continue;
				handle = (SettingHandle<T>)handles[i];
				break;
			}
			if (handle == null) {
				handle = new SettingHandle<T>(settingName) {Value = defaultValue};
				handles.Add(handle);
			}
			handle.DefaultValue = defaultValue;
			handle.Title = title;
			handle.Description = description;
			handle.Validator = validator;
			handle.EnumStringPrefix = enumPrefix;
			if (typeof(T).IsEnum && (enumPrefix == null || !(enumPrefix + Enum.GetName(typeof(T), default(T))).CanTranslate())) {
				HugsLibController.Logger.Warning("Missing enum setting labels for enum "+typeof(T));
			}
			if (loadedValues.ContainsKey(settingName)) {
				var loadedValue = loadedValues[settingName];
				loadedValues.Remove(settingName);
				handle.StringValue = loadedValue;
				if (handle.Validator != null && !handle.Validator(loadedValue)) {
					handle.ResetToDefault();
				}
			}
			return handle;
		}

		public bool TryRemoveUnclaimedValue(string name) {
			return loadedValues.Remove(name);
		}

		public IEnumerable<SettingHandle> Handles {
			get { return handles; }
		} 

		internal void LoadFromXml(XElement xml) {
			loadedValues.Clear();
			foreach (var childNode in xml.Elements()) {
				loadedValues.Add(childNode.Name.ToString(), childNode.Value);
			}
		}

		internal void WriteXml(XElement xml) {
			if (loadedValues.Count == 0 && handles.Count(h => !h.Unsaved) == 0) return; // no values, no saving
			var packElem = new XElement(ModId);
			xml.Add(packElem);
			foreach (var loadedValue in loadedValues) { // loaded values may remain uncalimed, so we put em back where they came from
				packElem.Add(new XElement(loadedValue.Key, new XText(loadedValue.Value)));
			}
			foreach (var handle in handles) {
				if(handle.Unsaved) continue;
				packElem.Add(new XElement(handle.Name, new XText(handle.StringValue)));
			}
		}
	}
}