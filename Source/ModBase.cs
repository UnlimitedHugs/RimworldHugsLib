using System;
using HarmonyLib;
using HugsLib.Core;
using HugsLib.Settings;
using HugsLib.Utils;
using UnityEngine.SceneManagement;
using Verse;
// ReSharper disable UnusedParameter.Global,UnusedAutoPropertyAccessor.Global

namespace HugsLib {
	/// <summary>
	/// The base class for all mods using HugsLib library. All classes extending ModBase will be instantiated 
	/// automatically by <see cref="HugsLibController"/> at game initialization.
	/// Can be annotated with <see cref="EarlyInitAttribute"/> to initialize the mod at <see cref="Verse.Mod"/>
	/// initialization time and have <see cref="EarlyInitialize"/> be called.
	/// </summary>
	public abstract class ModBase {
		public const string HarmonyInstancePrefix = "HugsLib.";

		/// <summary>
		/// This can be used to log messages specific to your mod.
		/// It will prefix everything with your ModIdentifier.
		/// </summary>
		protected ModLogger Logger { get; private set; }

		/// <summary>
		/// The ModSettingsPack specific to your mod.
		/// Use this to create settings handles that represent the values of saved settings.
		/// </summary>
		protected ModSettingsPack Settings { get; private set; }

		/// <summary>
		/// Override this and return false to prevent a Harmony instance from being automatically created and scanning your assembly for patches.
		/// </summary>
		protected virtual bool HarmonyAutoPatch {
			get { return true; }
		}
		
		/// <summary>
		/// The reference to Harmony instance that applied the patches in your assembly.
		/// </summary>
		protected Harmony HarmonyInst { get; set; }

		/// <summary>
		/// A unique identifier used both as <see cref="SettingsIdentifier"/> and <see cref="LogIdentifier"/>.
		/// Override them separately if different identifiers are needed or no <see cref="ModSettingsPack"/> should be assigned to <see cref="Settings"/>.
		/// Must start with a letter and contain any of [A-z, 0-9, -, _, :] (identifier must be valid as an XML tag name).
		/// </summary>
		/// <remarks>
		/// This is no longer used to identify mods since 7.0 (Rimworld 1.1). Use ModBase.ModContentPack.PackageId to that end instead.
		/// </remarks>
		public virtual string ModIdentifier {
			get { return null; } 
		}
		
		/// <summary>
		/// A unique identifier to use as a key when settings are stored for this mod by <see cref="ModSettingsManager"/>.
		/// Must start with a letter and contain any of [A-z, 0-9, -, _, :] (identifier must be valid as an XML tag name).
		/// By default uses the PackageId of the implementing mod.
		/// Returning null will prevent the <see cref="Settings"/> property from being assigned.
		/// </summary>
		public virtual string SettingsIdentifier {
			get { return ModIdentifier ?? ModContentPack?.PackageId; } 
		}
		
		/// <summary>
		/// A readable identifier for the mod, used as a prefix by <see cref="Logger"/> and in various error messages.
		/// Appear as "[LogIdentifier] message" when using <see cref="Logger"/>.
		/// By default uses the non-lowercase PackageId of the implementing mod or the type name if that is not set.
		/// </summary>
		public virtual string LogIdentifier {
			get { return ModIdentifier ?? ModContentPack?.PackageIdPlayerFacing ?? GetType().FullName; } 
		}

		/// <summary>
		/// The null-checked version of <see cref="LogIdentifier"/>. 
		/// Returns the type name if <see cref="LogIdentifier"/> is null.
		/// </summary>
		public string LogIdentifierSafe {
			get { return LogIdentifier ?? GetType().FullName; } 
		}

		protected ModContentPack modContentPackInt;

		/// <summary>
		/// The content pack for the mod containing the assembly this class belongs to
		/// </summary>
		public virtual ModContentPack ModContentPack {
			get { return modContentPackInt; }
			internal set { modContentPackInt = value; }
		}
		
		/// <summary>
		/// Can be false if the mod was enabled at game start and then disabled in the mods menu.
		/// Always true, unless the <see cref="Verse.ModContentPack"/> of the declaring mod can't be 
		/// identified for some unexpected reason.
		/// </summary>
		public bool ModIsActive { get; internal set; }
		
		/// <summary>
		/// Contains the AssemblyVersion and AssemblyFileVersion of the mod. Used by <see cref="GetVersion"/>.
		/// </summary>
		public AssemblyVersionInfo VersionInfo { get; internal set; }

		/// <summary>
		/// Added to avoid breaking mod compatibility during the 7.0 update.
		/// TODO: kill this during the next major update
		/// </summary>
		internal static ModContentPack CurrentlyProcessedContentPack { get; set; }

		protected ModBase() {
			modContentPackInt = CurrentlyProcessedContentPack;
			Logger = new ModLogger(LogIdentifierSafe);
			var settingsPackId = SettingsIdentifier;
			if (!string.IsNullOrEmpty(settingsPackId)) {
				if (PersistentDataManager.IsValidElementName(settingsPackId)) {
					Settings = HugsLibController.Instance.Settings.GetModSettings(settingsPackId, modContentPackInt?.Name);
				} else {
					Logger.Error($"string \"{settingsPackId}\" cannot be used as a settings identifier. " +
								$"Override {nameof(ModBase)}.{nameof(SettingsIdentifier)} to manually specify one. " +
								$"See {nameof(SettingsIdentifier)} autocomplete documentation for expected format.");
				}
			}
		}

		internal void ApplyHarmonyPatches() {
			if (HarmonyAutoPatch) {
				var harmonyId = ModContentPack?.PackageIdPlayerFacing;
				if (harmonyId == null) {
					harmonyId = $"HugsLib.{LogIdentifierSafe}";
					HugsLibController.Logger.Warning($"Failed to identify PackageId for \"{LogIdentifierSafe}\" " +
													$"using \"{harmonyId}\" as Harmony id instead.");
				}
				try {
					if (HugsLibController.Instance.ShouldHarmonyAutoPatch(GetType().Assembly, harmonyId)) {
						HarmonyInst = new Harmony(harmonyId);
						HarmonyInst.PatchAll(GetType().Assembly);
					}
				} catch (Exception e) {
					HugsLibController.Logger.Error("Failed to apply Harmony patches for {0}. Exception was: {1}", harmonyId, e);
				}
			}
		}

		internal ModSettingsPack SettingsPackInternalAccess {
			get { return Settings; }
		}

		/// <summary>
		/// Return the override version from the Version.xml file if specified, 
		/// or the higher one between AssemblyVersion and AssemblyFileVersion
		/// </summary>
		public virtual Version GetVersion() {
			var file = VersionFile.TryParseVersionFile(ModContentPack);
			if (file != null && file.OverrideVersion != null) return file.OverrideVersion;
			return VersionInfo.HighestVersion;
		}

		/// <summary>
		/// Called during HugsLib <see cref="Mod"/> instantiation, accounting for mod load order. 
		/// Load order among mods implementing <see cref="ModBase"/> is respected.
		/// and only if the implementing class is annotated with <see cref="EarlyInitAttribute"/>.
		/// </summary>
		public virtual void EarlyInitialize() {
		}

		[Obsolete("Override EarlyInitialize instead (typo).")]
		public virtual void EarlyInitalize() {
		}

		/// <summary>
		/// Called when HugsLib receives the <see cref="StaticConstructorOnStartup"/> call.
		/// Load order among mods implementing <see cref="ModBase"/> is respected.
		/// Called after the static constructors for non-HugsLib mods have executed. Is not called again on def reload
		/// </summary>
		public virtual void StaticInitialize() {
		}

		[Obsolete("Override StaticInitialize instead (more descriptive name).")]
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
		/// Called on each unity gui event, after UIRoot.UIRootOnGUI.
		/// Respects UI scaling and screen fading. Will not be called during loading screens.
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
		/// Called right after a new map has been generated.
		/// This is the equivalent of MapComponent.MapGenerated().
		/// </summary>
		/// <param name="map">The new map that has just finished generating</param>
		public virtual void MapGenerated(Map map) {
		}

		/// <summary>
		/// Called when the map was fully loaded
		/// </summary>
		/// <param name="map">The map that has finished loading</param>
		public virtual void MapLoaded(Map map) {
		}

		/// <summary>
		/// Called after a map has been abandoned or otherwise made inaccessible.
		/// Works on player bases, encounter maps, destroyed faction bases, etc.
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
		/// Called after StaticInitialize and when defs have been reloaded. This is a good place to inject defs.
		/// Get your settings handles here, so that the labels will properly update on language change.
		/// If the mod is disabled after being loaded, this method will STILL execute. Use ModIsActive to check.
		/// </summary>
		/// <remarks>
		/// There is no scenario in which defs are reloaded without the game restarting, save for a mod manually initiating a reload. 
		/// When def reloading is not an issue, anything done by this method can be safely done in StaticInitialize.
		/// </remarks>
		public virtual void DefsLoaded() {
		}
	}
}