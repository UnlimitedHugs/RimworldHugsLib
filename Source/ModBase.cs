using System;
using Harmony;
using HugsLib.Core;
using HugsLib.Settings;
using HugsLib.Utils;
using UnityEngine.SceneManagement;
using Verse;
// ReSharper disable UnusedParameter.Global,UnusedAutoPropertyAccessor.Global

namespace HugsLib {
	/// <summary>
	/// The base class for all mods using HugsLib library. All classes extending ModBase will be instantiated automatically by HugsLibController at game initialization.
	/// </summary>
	public abstract class ModBase {
		public const string HarmonyInstancePrefix = "HugsLib.";

		/// <summary>
		/// This can be used to log messages specific to your mod.
		/// It will prefix everithing with your ModIdentifier.
		/// </summary>
		protected ModLogger Logger { get; private set; }

		/// <summary>
		/// The ModSettingsPack specific to your mod.
		/// Use this to create settings handles.
		/// </summary>
		protected ModSettingsPack Settings { get; private set; }

		/// <summary>
		/// Override this and return false to prevent a HarmonyInstance from being automatically created and scanning your assembly for patches.
		/// </summary>
		protected virtual bool HarmonyAutoPatch {
			get { return true; }
		}
		
		/// <summary>
		/// The reference to HarmonyInstance that applied the patches in your assembly.
		/// </summary>
		protected HarmonyInstance HarmonyInst { get; set; }

		/// <summary>
		/// A unique identifier fo your mod.
		/// Valid characters are A-z, 0-9, -, no spaces.
		/// </summary>
		public abstract string ModIdentifier { get; }

		protected ModContentPack modContentPackInt;
		/// <summary>
		/// The content pack for the mod containing the assembly this class belongs to
		/// </summary>
		public virtual ModContentPack ModContentPack {
			get { return modContentPackInt; }
			internal set {
				if (Settings != null) {
					Settings.EntryName = value != null ? value.Name : null;
				}
				modContentPackInt = value;
			}
		}

		/// <summary>
		/// Return the override version from the Version.xml file if specified, or the assembly version otherwise
		/// </summary>
		/// <returns></returns>
		public virtual VersionShort GetVersion() {
			var file = VersionFile.TryParseVersionFile(ModContentPack);
			if (file != null && file.OverrideVersion != null) return file.OverrideVersion;
			return GetType().Assembly.GetName().Version;
		}

		/// <summary>
		/// Can be false if the mod was enabled at game start and then disabled in the mods menu
		/// </summary>
		public bool ModIsActive { get; internal set; }
		
		protected ModBase() {
			var modId = ModIdentifier;
			if (!PersistentDataManager.IsValidElementName(modId)) throw new FormatException("Invalid mod identifier: " + modId);
			Logger = new ModLogger(modId);
			Settings = HugsLibController.Instance.Settings.GetModSettings(modId);
			if (HarmonyAutoPatch) {
				var harmonyId = HarmonyInstancePrefix + ModIdentifier;
				try {
					HarmonyInst = HarmonyInstance.Create(harmonyId);
					HarmonyInst.PatchAll(GetType().Assembly);
				} catch (Exception e) {
					HugsLibController.Logger.Error("Failed to apply Harmony patches for {0}. Exception was: {1}", harmonyId, e);
				}
			}
		}

		/// <summary>
		/// Called after the static constructors for non-HugsLib mods have execuded. Is not called again on def reload
		/// </summary>
		public virtual void Initialize() {
		}

		/// <summary>
		/// Called on each tick when in Play scene
		/// </summary>
		/// <param name="currentTick">The sequential number of the tick being processed</param>
		public virtual void Tick(int currentTick) {
		}

		/// <summary>
		/// Called each frame
		/// </summary>
		public virtual void Update() {
		}

		/// <summary>
		/// Called each unity physics update
		/// </summary>
		public virtual void FixedUpdate() {
		}

		/// <summary>
		/// Callead on each unity gui event, after UIRoot.UIRootOnGUI.
		/// Resprects UI scaling and screen fading. Will not be called during loading screens.
		/// This is a good place to listen for hotkey events.
		/// </summary>
		public virtual void OnGUI() {
		}


		/// <summary>
		/// Called when GameState.Playing has been entered and the world is fully loaded in the Play scene.
		/// Will not be called during world generation and landing site selection.
		/// </summary>
		public virtual void WorldLoaded() {
		}
		
		/// <summary>
		/// Called right after Map.ConstructComponents() (before MapLoaded)
		/// </summary>
		/// <param name="map">The map being initialized</param>
		public virtual void MapComponentsInitializing(Map map) {
		}

		/// <summary>
		/// Called when the map was fully loaded
		/// </summary>
		/// <param name="map">The map that has finished loading</param>
		public virtual void MapLoaded(Map map) {
		}

		/// <summary>
		/// Called after a map has been abandoned or otherwise made inaccessible.
		/// Works on player bases, enounter maps, destroyed faction bases, etc.
		/// </summary>
		/// <param name="map">The map that has been discarded</param>
		public virtual void MapDiscarded(Map map) {
		}

		/// <summary>
		/// Called after each scene change
		/// </summary>
		/// <param name="scene">The scene that has been loaded</param>
		public virtual void SceneLoaded(Scene scene) {
		}

		/// <summary>
		/// Called after settings menu changes have been confirmed.
		/// This is called for all mods, regardless if their own settings have been modified, or not.
		/// </summary>
		public virtual void SettingsChanged() {
		}

		
		
		/// <summary>
		/// Called after Initialize and when defs have been reloaded. This is a good place to inject defs.
		/// Get your settings handles here, so that the labels will properly update on language change.
		/// If the mod is disabled after being loaded, this method will STILL execute. Use ModIsActive to check.
		/// </summary>
		public virtual void DefsLoaded() {
		}

	}
}