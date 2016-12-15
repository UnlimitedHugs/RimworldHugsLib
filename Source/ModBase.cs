using System;
using HugsLib.Core;
using HugsLib.Settings;
using HugsLib.Utils;
using UnityEngine.SceneManagement;
using Verse;

namespace HugsLib {
	/**
	 * The base class for all mods using this library. All classes extending ModBase will be instantiated automatically by HugsLibController at game initialization.
	 */
	public abstract class ModBase {

		protected ModLogger Logger { get; private set; }

		protected ModSettingsPack Settings { get; private set; }

		public abstract string ModIdentifier { get; }

		// the content pack for the mod this class belongs to
		protected ModContentPack modContentPackInt;
		public virtual ModContentPack ModContentPack {
			get { return modContentPackInt; }
			internal set {
				if (Settings != null) {
					Settings.EntryName = value != null ? value.Name : null;
				}
				modContentPackInt = value;
			}
		}

		// return the override version from the Version.xml file if specified, or the assembly version otherwise
		public virtual VersionShort GetVersion() {
			var file = VersionFile.TryParseVersionFile(ModContentPack);
			if (file != null && file.OverrideVersion != null) return file.OverrideVersion;
			return GetType().Assembly.GetName().Version;
		}

		// can be false if the mod was enabled at game start and then disabled in the mods menu
		public bool ModIsActive { get; internal set; }
		
		protected ModBase() {
			var modId = ModIdentifier;
			if (!PersistentDataManager.IsValidElementName(modId)) throw new FormatException("Invalid mod identifier: " + modId);
			Logger = new ModLogger(modId);
			Settings = HugsLibController.Instance.Settings.GetModSettings(modId);
		}

		// called when the scene object is intialized. Is not called again on def reload
		public virtual void Initialize() {
		}

		// called on each tick when in Play scene
		public virtual void Tick(int currentTick) {
		}

		// called each frame
		public virtual void Update() {
		}

		// called each unity physics update
		public virtual void FixedUpdate() {
		}

		// callead on each unity gui event
		public virtual void OnGUI() {
		}

		// called when the Play scene was entered and initialization has been completed
		public virtual void WorldLoaded() {
		}
		
		// called during Map.ConstructComponents() before MapLoaded
		public virtual void MapComponentsInitializing(Map map) {
		}

		// called when the map was fully loaded
		public virtual void MapLoaded(Map map) {
		}

		// called on each scene change
		public virtual void SceneLoaded(Scene scene) {
		}

		// called after settings menu changes have been confirmed
		public virtual void SettingsChanged() {
		}

		// called after Initialize and when defs have been reloaded. This is a good place to inject defs
		// Get your settings handles here, so that the labels will properly update on language change
		// If the mod is disabled after being loaded, this method will STILL execute. Use ModIsActive to check.
		public virtual void DefsLoaded() {
		}

	}
}