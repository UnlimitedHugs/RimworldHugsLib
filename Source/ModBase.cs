using System;
using HugsLib.Settings;
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
		private ModContentPack modContentPackInt;
		public ModContentPack ModContentPack {
			get { return modContentPackInt; }
			internal set {
				if (Settings != null) {
					Settings.EntryName = value != null ? value.Name : null;
				}
				modContentPackInt = value;
			}
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
		public virtual void Initalize() {
		}

		// called on each tick when a map is loaded
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
		
		// called when the map scene has been entered
		public virtual void MapLoading() {
		}

		// called during Map.ConstructComponents(): after MapLoading and before MapLoaded. Last chance to modify and inject defs.
		public virtual void MapComponentsInitializing() {
		}

		// called when the map was fully loaded
		public virtual void MapLoaded() {
		}

		// called on each scene change
		public virtual void LevelLoaded(int levelId) {
		}

		// called after settings menu changes have been confirmed
		public virtual void SettingsChanged() {
		}

		// called after initalize and when defs have been reloaded. This is a good place to inject defs
		// If the mod is disabled after being loaded, this method will STILL execute
		public virtual void DefsLoaded() {
		}
	}
}