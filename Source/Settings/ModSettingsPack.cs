using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using HugsLib.Core;
using HugsLib.Utils;
using Verse;

namespace HugsLib.Settings {
	/// <summary>
	/// A group of settings values added by a mod. Each mod has their own ModSettingsPack.
	/// Loaded values are stored until they are "claimed" by their mod by requesting a handle for a setting with the same name.
	/// </summary>
	public class ModSettingsPack {
		public enum ListPriority {
			Higher, Normal, Lower, Lowest
		}

		/// <summary>
		/// Identifier of the mod that owns this pack
		/// </summary>
		public string ModId { get; private set; }

		/// <summary>
		/// The name of the owning mod that will display is the Mod Settings dialog
		/// </summary>
		public string EntryName { get; set; } = null!;
		/// <summary>
		/// Special display order for the mod in the Mod Settings dialog.
		/// Mods are generally ordered by name. Please leave this at Normal unless you have a good reason to change it.
		/// </summary>
		public ListPriority DisplayPriority { get; set; }
		/// <summary>
		/// Additional context menu options for this entry in the mod settings dialog.
		/// Will be shown when the hovering menu button for this entry is clicked.
		/// </summary>
		public IEnumerable<ContextMenuEntry>? ContextMenuEntries { get; set; }
		/// <summary>
		/// Returns true if any handles retrieved from this pack have had their values changed.
		/// Resets to false after the changes are saved.
		/// </summary>
		public bool HasUnsavedChanges {
			get {
				for (var i = 0; i < handles.Count; i++) {
					if (handles[i].HasUnsavedChanges) return true;
				}
				return false;
			} 
		}
		/// <summary>
		/// Enumerates the handles that have been registered with this pack up to this point.
		/// </summary>
		public IEnumerable<SettingHandle> Handles => handles;

		/// <summary>
		/// Set to true to disable the collapsing of setting handles in the Mod Settings dialog.
		/// </summary>
		internal bool AlwaysExpandEntry;

		internal ModSettingsManager ParentManager { get; set; } = null!;

		private readonly Dictionary<string, string> loadedValues = new Dictionary<string, string>();
		private readonly List<SettingHandle> handles = new List<SettingHandle>();

		internal ModSettingsPack(string modId) {
			ModId = modId;
			DisplayPriority = ListPriority.Normal;
		}

		/// <summary>
		/// Retrieves an existing SettingHandle from the pack, or creates a new one.
		/// Loaded settings will only display in the Mod Settings dialog after they have been claimed using this method.
		/// </summary>
		/// <typeparam name="T">The type of setting value you are creating.</typeparam>
		/// <param name="settingName">Unique identifier for the setting. Must be unique for this specific pack only.</param>
		/// <param name="title">A display name for the setting that will show up next to it in the Mod Settings dialog. Recommended to keep this short.</param>
		/// <param name="description">A description for the setting that will appear in a tooltip when the player hovers over the setting in the Mod Settings dialog.</param>
		/// <param name="defaultValue">The value the setting will assume when newly created and when the player resets the setting to its default.</param>
		/// <param name="validator">An optional delegate that will be called when a new value is about to be assigned to the handle. Receives a string argument and must return a bool to indicate if the passed value is valid for the setting.</param>
		/// <param name="enumPrefix">Used only for Enum settings. Enum values are displayed in a readable format by the following method: Translate(prefix+EnumValueName)</param>
		public SettingHandle<T> GetHandle<T>(string settingName, string? title, string? description, T? defaultValue = default, SettingHandle.ValueIsValid? validator = null, string? enumPrefix = null) {
			if (!PersistentDataManager.IsValidElementName(settingName)) throw new Exception("Invalid name for mod setting: " + settingName);
			SettingHandle<T>? handle = null;
			for (int i = 0; i < handles.Count; i++) {
				if (handles[i].Name != settingName) continue;
				if (handles[i] is not SettingHandle<T> settingHandle) continue;
				handle = settingHandle;
				break;
			}
			if (handle == null) {
				handle = new SettingHandle<T>(settingName)
				{
					Value = defaultValue,
					ParentPack = this
				};
				handles.Add(handle);
			}
			handle.DefaultValue = defaultValue;
			handle.Title = title;
			handle.Description = description;
			handle.Validator = validator;
			handle.EnumStringPrefix = enumPrefix;
			if (typeof(T).IsEnum && (enumPrefix == null || !(enumPrefix + Enum.GetName(typeof(T), defaultValue!)).CanTranslate())) {
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
			handle.HasUnsavedChanges = false;
			return handle;
		}

		/// <summary>
		/// Returns a handle that was already created.
		/// Will return null if the handle does not exist yet.
		/// </summary>
		/// <exception cref="InvalidCastException">Throws an exception if the referenced handle does not match the provided type</exception>
		/// <param name="settingName">The name of the handle to retrieve</param>
		public SettingHandle<T>? GetHandle<T>(string settingName) {
			for (int i = 0; i < handles.Count; i++) {
				var handle = handles[i];
				if (handle.Name == settingName) {
					if (handle is not SettingHandle<T> settingHandle) throw new InvalidCastException(string.Format("Handle {0} does not match the specified type of {1}", settingName, typeof(SettingHandle<T>)));
					return settingHandle;
				}
			}
			return null;
		}

		/// <summary>
		/// Attempts to retrieve a setting value by name.
		/// If a handle for that value has already been created, returns that handle's StringValue.
		/// Otherwise will return the unclaimed value that was loaded from the XML file.
		/// Will return null if the value does not exist.
		/// </summary>
		/// <param name="settingName">The name of the setting the value of which should be retrieved</param>
		public string? PeekValue(string settingName) {
			var handle = handles.Find(h => h.Name == settingName);
			if (handle != null) {
				return handle.StringValue;
			}
			if (loadedValues.TryGetValue(settingName, out var loadedValue)) {
				return loadedValue;
			}
			return null;
		}

		/// <summary>
		/// Returns true, if there is a setting value that can be retrieved with PeekValue.
		/// This includes already created handles and unclaimed values.
		/// </summary>
		/// <param name="settingName">The name of the setting to check</param>
		public bool ValueExists(string settingName) {
			return handles.Find(h => h.Name == settingName) != null || loadedValues.ContainsKey(settingName);
		}

		/// <summary>
		/// Deletes a setting loaded from the xml file before it is claimed using GetHandle.
		/// Useful for cleaning up settings that are no longer in use.
		/// </summary>
		/// <param name="name">The identifier of the setting (handle identifier)</param>
		public bool TryRemoveUnclaimedValue(string name) {
			return loadedValues.Remove(name);
		}

		/// <summary>
		/// Prompts the <see cref="ModSettingsManager"/> to save changes if any or the registered 
		/// <see cref="ModSettingsPack"/>s have handles with unsaved changes
		/// </summary>
		public void SaveChanges() {
			ParentManager.SaveChanges();
		}

		internal bool CanBeReset {
			get { return handles.Any(h => h.CanBeReset); }
		}

		internal void LoadFromXml(XElement xml) {
			loadedValues.Clear();
			foreach (var childNode in xml.Elements()) {
				loadedValues.Add(childNode.Name.ToString(), childNode.Value);
			}
		}

		internal void WriteXml(XElement xml) {
			if (loadedValues.Count == 0 && handles.Count(h => !h.Unsaved && !h.HasDefaultValue()) == 0) return; // no values, no saving
			var packElem = new XElement(ModId);
			xml.Add(packElem);
			foreach (var loadedValue in loadedValues) { // loaded values may remain unclaimed, so we put em back where they came from
				packElem.Add(new XElement(loadedValue.Key, new XText(loadedValue.Value)));
			}
			foreach (var handle in handles) {
				handle.HasUnsavedChanges = false;
				if (handle.ShouldBeSaved) {
					packElem.Add(new XElement(handle.Name, new XText(handle.StringValue)));
				}
			}
		}

		public override string ToString() {
			return $"[{nameof(ModSettingsPack)} {nameof(ModId)}:{ModId} " +
				$"{nameof(Handles)}:{Handles.Select(h => h.Name).Join(",")}]";
		}
	}
}