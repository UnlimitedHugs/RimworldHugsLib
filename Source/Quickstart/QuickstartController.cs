using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using HarmonyLib;
using HugsLib.Core;
using HugsLib.Settings;
using HugsLib.Utils;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Profile;

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
				if (handle == null) throw new NullReferenceException("Setting handle not initialized");
				return handle.Value ?? (handle.Value = new QuickstartSettings());
			}
		}

		private static SettingHandle<QuickstartSettings> handle;
		private static QuickstartStatusBox statusBox;
		private static bool quickstartPending;

		public static void InitiateMapGeneration() {
			HugsLibController.Logger.Message(
				"Quickstarter generating map with scenario: " + GetMapGenerationScenario().name);
			LongEventHandler.QueueLongEvent(() => {
				MemoryUtility.ClearAllMapsAndWorld();
				ApplyQuickstartConfiguration();
				PageUtility.InitGameStart();
			}, "GeneratingMap", true, GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap);
		}

		public static void InitiateSaveLoading() {
			var saveName = GetSaveNameToLoad()
				?? throw new WarningException("save filename not set");
			var filePath = GenFilePaths.FilePathForSavedGame(saveName);
			if (!File.Exists(filePath)) {
				throw new WarningException("save file not found: " + saveName);
			}
			HugsLibController.Logger.Message("Quickstarter is loading saved game: " + saveName);
			var loadAction = () => GameDataSaveLoader.LoadGame(saveName);
			if (Settings.BypassSafetyDialog) {
				loadAction();
			} else {
				PreLoadUtility.CheckVersionAndLoad(filePath, ScribeMetaHeaderUtility.ScribeHeaderMode.Map, loadAction);
			}
		}

		internal static void OnEarlyInitialize(ModSettingsPack librarySettings) {
			PrepareSettings(librarySettings);
			PrepareQuickstart();
		}

		internal static void OnLateInitialize() {
			RetrofitSettingWithLabel();
			EnumerateMapSizes();
			if (Prefs.DevMode) {
				LongEventHandler.QueueLongEvent(InitiateQuickstart, null, false, null);
			}
		}

		internal static void OnGUIUnfiltered() {
			if (!quickstartPending) return;
			statusBox.OnGUI();
		}

		internal static void DrawDebugToolbarButton(WidgetRow widgets) {
			const string quickstartButtonTooltip = "Open the quickstart settings.\n\n"
				+ "This lets you automatically generate a map or load an existing save when the game is started.\n"
				+ "Shift-click to quick-generate a new map.";
			if (widgets.ButtonIcon(HugsLibTextures.quickstartIcon, quickstartButtonTooltip)) {
				var stack = Find.WindowStack;
				if (HugsLibUtility.ShiftIsHeld) {
					stack.TryRemove(typeof(Dialog_QuickstartSettings));
					InitiateMapGeneration();
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
			handle.ForceSaveChanges();
		}

		internal static void AddReplacementQuickstartButton(List<ListableOption> buttons) {
			buttons.Add(new ListableOption("DevQuickTest".Translate(), InitiateMapGeneration));
		}
		
		private static void PrepareSettings(ModSettingsPack librarySettings) {
			handle = librarySettings.GetHandle<QuickstartSettings>("quickstartSettings", null, null);
			handle.NeverVisible = true;
		}

		private static void RetrofitSettingWithLabel() {
			// language data is not yet loaded when creating the handle, so we have to postpone adding the label
			handle.Title = "HugsLib_setting_quickstartSettings_label".Translate();
		}

		private static void PrepareQuickstart() {
			if (Settings.OperationMode != QuickstartSettings.QuickstartMode.Disabled) {
				quickstartPending = true;
				statusBox = new QuickstartStatusBox(GetStatusBoxOperation(Settings));
				statusBox.AbortRequested += StatusBoxAbortRequestedHandler;
			}

			QuickstartStatusBox.IOperationMessageProvider GetStatusBoxOperation(QuickstartSettings settings) {
				return settings.OperationMode switch {
					QuickstartSettings.QuickstartMode.LoadMap => new QuickstartStatusBox.LoadSaveOperation(
						GetSaveNameToLoad() ?? string.Empty),
					QuickstartSettings.QuickstartMode.GenerateMap => new QuickstartStatusBox.GenerateMapOperation(
						settings.ScenarioToGen, settings.MapSizeToGen),
					_ => throw new ArgumentOutOfRangeException("Unhandled operation mode: " + settings.OperationMode)
				};
			}
		}

		private static void StatusBoxAbortRequestedHandler(bool abortAndDisable) {
			quickstartPending = false;
			HugsLibController.Logger.Warning("Quickstart aborted: Space key was pressed.");
			if (abortAndDisable) {
				Settings.OperationMode = QuickstartSettings.QuickstartMode.Disabled;
				LongEventHandler.ExecuteWhenFinished(SaveSettings);
			}
		}
		
		private static void InitiateQuickstart() {
			if (!quickstartPending) return;
			quickstartPending = false;
			statusBox = null;
			try {
				if (Settings.OperationMode == QuickstartSettings.QuickstartMode.Disabled) return;
				CheckForErrorsAndWarnings();
				if (Settings.OperationMode == QuickstartSettings.QuickstartMode.GenerateMap) {
					InitiateMapGeneration();
				} else if (Settings.OperationMode == QuickstartSettings.QuickstartMode.LoadMap) {
					InitiateSaveLoading();
				}
			} catch (WarningException e) {
				HugsLibController.Logger.Error("Quickstart aborted: " + e.Message);
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
					case 200:
						desc = "MapSizeSmall".Translate();
						break;
					case 250:
						desc = "MapSizeMedium".Translate();
						break;
					case 300:
						desc = "MapSizeLarge".Translate();
						break;
					case 350:
						desc = "MapSizeExtreme".Translate();
						break;
				}
				var label = string.Format("{0}x{0}", size) + (desc != null ? $" ({desc})" : "");
				MapSizes.Add(new MapSizeEntry(size, label));
			}
			SnapSettingsMapSizeToClosestValue(Settings, MapSizes);
		}

		private static Scenario GetMapGenerationScenario() {
			return TryGetScenarioByName(Settings.ScenarioToGen) ?? ScenarioDefOf.Crashlanded.scenario;
		}

		private static void ApplyQuickstartConfiguration() {
			// adapted from Root_Play.SetupForQuickTestPlay
			Current.ProgramState = ProgramState.Entry;
			Current.Game = new Game {
				InitData = new GameInitData(),
				Scenario = GetMapGenerationScenario(),
			};
			Find.Scenario.PreConfigure();
			Current.Game.storyteller = new Storyteller(StorytellerDefOf.Cassandra, DifficultyDefOf.Rough);
			Current.Game.World = WorldGenerator.GenerateWorld(0.05f, GenText.RandomSeedString(),
				OverallRainfall.Normal, OverallTemperature.Normal, OverallPopulation.Normal, LandmarkDensity.Normal);
			Find.GameInitData.ChooseRandomStartingTile();
			Find.GameInitData.mapSize = Settings.MapSizeToGen;
			Find.Scenario.PostIdeoChosen();
		}

		private static Scenario TryGetScenarioByName(string name) {
			return ScenarioLister.AllScenarios().FirstOrDefault(s => s.name == name);
		}

		// ensure that the settings have a valid map size
		private static void SnapSettingsMapSizeToClosestValue(QuickstartSettings settings, List<MapSizeEntry> sizes) {
			Settings.MapSizeToGen = sizes.OrderBy(e => Mathf.Abs(e.Size - settings.MapSizeToGen)).First().Size;
		}

		private static string GetSaveNameToLoad() {
			return Settings.SaveFileToLoad ?? TryGetMostRecentSaveFileName();
		}

		private static string TryGetMostRecentSaveFileName() {
			var mostRecentFilePath = GenFilePaths.AllSavedGameFiles.FirstOrDefault()?.Name;
			return Path.GetFileNameWithoutExtension(mostRecentFilePath);
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