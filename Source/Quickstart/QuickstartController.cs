using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;
using HugsLib.Settings;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace HugsLib.Quickstart {
	/// <summary>
	/// Manages the custom quickstart functionality.
	/// Will trigger map loading and generation when the appropriate settings are present, and draws an additional dev toolbar button.
	/// </summary>
	[StaticConstructorOnStartup]
	public static class QuickstartController {
		public const int DefaultMapSize = 250;

		public static readonly List<MapSizeEntry> MapSizes = new List<MapSizeEntry>();

		public static QuickstartSettings Settings {
			get {
				if(handle == null) throw new NullReferenceException("Setting handle not initialized");
				return handle.Value ?? (handle.Value = new QuickstartSettings());
			}
		}

		private static Texture2D quickstartIconTex;
		private static Type quickStarterType;
		private static FieldInfo quickStartedField;
		private static SettingHandle<QuickstartSettings> handle;
		private static bool mapGenerationPending;

		public static void InitateMapGeneration() {
			PrepareMapGeneration();
			HugsLibController.Logger.Message("Quickstarter generating map with scenario: " + DefDatabase<ScenarioDef>.GetNamed(Settings.ScenarioToGen).label);
			quickStartedField.SetValue(null, true);
			LongEventHandler.QueueLongEvent(() => {
				Current.Game = null;
			}, "Play", "GeneratingMap", true, null);
		}

		internal static void PrepareReflection() {
			quickStarterType = typeof(Root).Assembly.GetType("Verse.QuickStarter");
			if (quickStarterType == null) HugsLibController.Logger.Error("Verse.QuickStarter type not found");
			quickStartedField = AccessTools.Field(quickStarterType, "quickStarted");
			if (quickStartedField == null) HugsLibController.Logger.Error("QuickStarter.quickStarted field not found");
		}

		internal static void Initialize() {
			LongEventHandler.ExecuteWhenFinished(() => quickstartIconTex = ContentFinder<Texture2D>.Get("quickstartIcon"));
			EnumerateMapSizes();
			if (Prefs.DevMode) {
				LongEventHandler.QueueLongEvent(SetupForQuickstart, null, false, null);
			}
		}

		internal static void RegisterSettings(ModSettingsPack settings) {
			handle = settings.GetHandle<QuickstartSettings>("quickstartSettings", null, null);
			handle.VisibilityPredicate = () => false; // invisible, but can be reset by "Reset all settings"
		}

		internal static void DrawDebugToolbarButton(WidgetRow widgets) {
			if (widgets.ButtonIcon(quickstartIconTex, "Open the quickstart settings.\n\nThis lets you automatically generate a map or load an existing save when the game is started.\nShift-click to quick-generate a new map.")) {
				var stack = Find.WindowStack;
				if (HugsLibUtility.ShiftIsHeld) {
					stack.TryRemove(typeof(Dialog_QuickstartSettings));
					InitateMapGeneration();
				} else {
					if (stack.IsOpen<Dialog_QuickstartSettings>()) {
						stack.TryRemove(typeof(Dialog_QuickstartSettings));
					} else {
						stack.Add(new Dialog_QuickstartSettings());
					}
				}
			}
		}

		internal static void SaveSettings() {
			HugsLibController.SettingsManager.SaveChanges();
		}

		internal static Scenario ReplaceQuickstartScenarioIfNeeded(Scenario original) {
			return mapGenerationPending ? DefDatabase<ScenarioDef>.GetNamed(Settings.ScenarioToGen).scenario : original;
		}

		internal static int ReplaceQuickstartMapSizeIfNeeded(int original) {
			return mapGenerationPending ? Settings.MapSizeToGen : original;
		}

		private static void SetupForQuickstart() {
			try {
				if (Settings.OperationMode == QuickstartSettings.QuickstartMode.Disabled) return;
				if (HugsLibUtility.ShiftIsHeld) {
					HugsLibController.Logger.Warning("Quickstart aborted: Shift key was held.");
					return;
				}
				CheckForErrorsAndWarnings();
				if (Settings.OperationMode == QuickstartSettings.QuickstartMode.GenerateMap) {
					if (GenCommandLine.CommandLineArgPassed("quicktest")) {
						// vanilla QuickStarter will change the scene, only set up scenario and map size injection
						PrepareMapGeneration();
					} else {
						InitateMapGeneration();
					}
				} else if(Settings.OperationMode == QuickstartSettings.QuickstartMode.LoadMap) {
					var saveName = Settings.SaveFileToLoad;
					if (saveName == null) {
						throw new WarningException("save filename not set");
					}
					var filePath = GenFilePaths.FilePathForSavedGame(saveName);
					if (!File.Exists(filePath)) {
						throw new WarningException("save file not found: "+Settings.SaveFileToLoad);
					}
					HugsLibController.Logger.Message("Quickstarter is loading saved game: "+saveName);
					Action loadAction = () => {
						LongEventHandler.QueueLongEvent(delegate {
							Current.Game = new Game {InitData = new GameInitData {gameToLoad = saveName}};
						}, "Play", "LoadingLongEvent", true, null);
					};
					if (Settings.BypassSafetyDialog) {
						loadAction();
					} else {
						PreLoadUtility.CheckVersionAndLoad(filePath, ScribeMetaHeaderUtility.ScribeHeaderMode.Map, loadAction);
					}
				}
			} catch (WarningException e) {
				HugsLibController.Logger.Error("Quickstart aborted: "+e.Message);
			}
		}

		private static void CheckForErrorsAndWarnings() {
			if (Settings.StopOnErrors && Log.Messages.Any(m => m.type == LogMessageType.Error)) {
				throw new WarningException("errors detected in log");
			}
			if (Settings.StopOnWarnings && Log.Messages.Any(m => m.type == LogMessageType.Warning)) {
				throw new WarningException("warnings detected in log");
			}
		}

		private static void EnumerateMapSizes() {
			var vanillaSizes = Traverse.Create<Dialog_AdvancedGameConfig>().Field("MapSizes").GetValue<int[]>();
			if (vanillaSizes == null) {
				HugsLibController.Logger.Error("Could not reflect required field: Dialog_AdvancedGameConfig.MapSizes");
				return;
			}
			MapSizes.Clear();
			MapSizes.Add(new MapSizeEntry(75, "75x75 (Encounter)"));
			foreach (var size in vanillaSizes) {
				string desc = null;
				switch (size) {
					case 200: desc = "MapSizeSmall".Translate(); break;
					case 250: desc = "MapSizeMedium".Translate(); break;
					case 300: desc = "MapSizeLarge".Translate(); break;
					case 350: desc = "MapSizeExtreme".Translate(); break;
				}
				var label = String.Format("{0}x{0}", size) + (desc != null ? String.Format(" ({0})", desc) : "");
				MapSizes.Add(new MapSizeEntry(size, label));
			}
			SnapSettingsMapSizeToClosestValue(Settings, MapSizes);
		}

		private static void PrepareMapGeneration() {
			var scenarioDef = DefDatabase<ScenarioDef>.GetNamedSilentFail(Settings.ScenarioToGen);
			if (scenarioDef == null) {
				throw new WarningException("scenario not found: " + Settings.ScenarioToGen);
			}
			mapGenerationPending = true;
		}

		// ensure that the settings have a valid map size
		private static void SnapSettingsMapSizeToClosestValue(QuickstartSettings settings, List<MapSizeEntry> sizes) {
			Settings.MapSizeToGen = sizes.OrderBy(e => Mathf.Abs(e.Size - settings.MapSizeToGen)).First().Size;
		}

		public class MapSizeEntry {
			public readonly int Size;
			public readonly string Label;

			public MapSizeEntry(int size, string label) {
				Size = size;
				Label = label;
			}
		}
	}
}