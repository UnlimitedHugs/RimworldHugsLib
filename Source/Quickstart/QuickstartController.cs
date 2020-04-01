using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
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
		private static bool quickstartPending;
		private static bool mapGenerationPending;

		public static void InitateMapGeneration() {
			PrepareMapGeneration();
			HugsLibController.Logger.Message("Quickstarter generating map with scenario: " + TryGetScenarioByName(Settings.ScenarioToGen).name);
			quickStartedField.SetValue(null, true);
			LongEventHandler.QueueLongEvent(() => {
				Current.Game = null;
			}, "Play", "GeneratingMap", true, null);
		}

		public static void InitateSaveLoading() {
			var saveName = Settings.SaveFileToLoad;
			if (saveName == null) {
				throw new WarningException("save filename not set");
			}
			var filePath = GenFilePaths.FilePathForSavedGame(saveName);
			if (!File.Exists(filePath)) {
				throw new WarningException("save file not found: " + Settings.SaveFileToLoad);
			}
			HugsLibController.Logger.Message("Quickstarter is loading saved game: " + saveName);
			Action loadAction = () => {
				LongEventHandler.QueueLongEvent(delegate {
					Current.Game = new Game { InitData = new GameInitData { gameToLoad = saveName } };
				}, "Play", "LoadingLongEvent", true, null);
			};
			if (Settings.BypassSafetyDialog) {
				loadAction();
			} else {
				PreLoadUtility.CheckVersionAndLoad(filePath, ScribeMetaHeaderUtility.ScribeHeaderMode.Map, loadAction);
			}
		}

		internal static void PrepareReflection() {
			quickStarterType = typeof(Root).Assembly.GetType("Verse.QuickStarter");
			if (quickStarterType == null) HugsLibController.Logger.Error("Verse.QuickStarter type not found");
			quickStartedField = AccessTools.Field(quickStarterType, "quickStarted");
			if (quickStartedField == null) HugsLibController.Logger.Error("QuickStarter.quickStarted field not found");
		}

		internal static void OnEarlyInitialize(ModSettingsPack librarySettings) {
			PrepareSettings(librarySettings);
			if (Settings.OperationMode != QuickstartSettings.QuickstartMode.Disabled) {
				quickstartPending = true;
			}
		}

		internal static void OnLateInitialize() {
			LongEventHandler.ExecuteWhenFinished(() => quickstartIconTex = ContentFinder<Texture2D>.Get("quickstartIcon"));
			EnumerateMapSizes();
			if (Prefs.DevMode) {
				LongEventHandler.QueueLongEvent(InitiateQuickstart, null, false, null);
			}
		}

		internal static void OnGUIUnfiltered() {
			if (!quickstartPending) return;
			StatusBox.OnGUI(Settings);
			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space) {
				quickstartPending = false;
				HugsLibController.Logger.Warning("Quickstart aborted: Space key was pressed.");
				if (HugsLibUtility.ShiftIsHeld) {
					Settings.OperationMode = QuickstartSettings.QuickstartMode.Disabled;
					LongEventHandler.ExecuteWhenFinished(SaveSettings);
				}
				Event.current.Use();
			}
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
			handle.ForceSaveChanges();
		}

		internal static Scenario ReplaceQuickstartScenarioIfNeeded(Scenario original) {
			return mapGenerationPending ? TryGetScenarioByName(Settings.ScenarioToGen) : original;
		}

		internal static int ReplaceQuickstartMapSizeIfNeeded(int original) {
			return mapGenerationPending ? Settings.MapSizeToGen : original;
		}

		private static void PrepareSettings(ModSettingsPack librarySettings) {
			handle = librarySettings.GetHandle<QuickstartSettings>("quickstartSettings", null, null);
			handle.NeverVisible = true;
		}

		private static void InitiateQuickstart() {
			if(!quickstartPending) return;
			quickstartPending = false;
			try {
				if (Settings.OperationMode == QuickstartSettings.QuickstartMode.Disabled) return;
				CheckForErrorsAndWarnings();
				if (Settings.OperationMode == QuickstartSettings.QuickstartMode.GenerateMap) {
					if (GenCommandLine.CommandLineArgPassed("quicktest")) {
						// vanilla QuickStarter will change the scene, only set up scenario and map size injection
						PrepareMapGeneration();
					} else {
						InitateMapGeneration();
					}
				} else if(Settings.OperationMode == QuickstartSettings.QuickstartMode.LoadMap) {
					InitateSaveLoading();
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
				var label = string.Format("{0}x{0}", size) + (desc != null ? $" ({desc})" : "");
				MapSizes.Add(new MapSizeEntry(size, label));
			}
			SnapSettingsMapSizeToClosestValue(Settings, MapSizes);
		}

		private static void PrepareMapGeneration() {
			var scenario = TryGetScenarioByName(Settings.ScenarioToGen);
			if (scenario == null) {
				throw new WarningException("scenario not found: " + Settings.ScenarioToGen);
			}
			mapGenerationPending = true;
		}

		private static Scenario TryGetScenarioByName(string name) {
			return ScenarioLister.AllScenarios().FirstOrDefault(s => s.name == name);
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

		private static class StatusBox {
			private static readonly Vector2 StatusRectSize = new Vector2(240f, 75f);
			private static readonly Vector2 StatusRectPadding = new Vector2(26f, 18f);

			public static void OnGUI(QuickstartSettings settings) {
				var statusText = GetStatusBoxText(settings);
				var boxRect = GetStatusBoxRect(statusText);
				DrawStatusBox(boxRect, statusText);
			}

			private static string GetStatusBoxText(QuickstartSettings settings) {
				var sb = new StringBuilder("HugsLib quickstarter preparing to\n");
				switch (settings.OperationMode) {
					case QuickstartSettings.QuickstartMode.LoadMap:
						sb.AppendFormat("load save file: {0}\n", settings.SaveFileToLoad);
						break;
					case QuickstartSettings.QuickstartMode.GenerateMap:
						sb.AppendFormat("generate map: {0} ({1}x{1})\n", 
							settings.ScenarioToGen, settings.MapSizeToGen
						);
						break;
					default:
						return $"Unhandled {nameof(QuickstartSettings.QuickstartMode)}: {settings.OperationMode}";
				}
				sb.AppendLine();
				sb.Append("<color=#777777>");
				sb.AppendLine("Press Space to abort");
				sb.Append("Press Shift+Space to disable");
				sb.Append("</color>");
				return sb.ToString();
			}

			private static Rect GetStatusBoxRect(string statusText) {
				var statusTextSize = Text.CalcSize(statusText);
				var boxWidth = Mathf.Max(StatusRectSize.x, statusTextSize.x + StatusRectPadding.x * 2f);
				var boxHeight = Mathf.Max(StatusRectSize.y, statusTextSize.y + StatusRectPadding.y * 2f);
				var boxRect = new Rect(
					(UI.screenWidth - boxWidth) / 2f,
					(UI.screenHeight / 2f - boxHeight) / 2f,
					boxWidth, boxHeight
				);
				boxRect = boxRect.Rounded();
				return boxRect;
			}

			private static void DrawStatusBox(Rect rect, string statusText) {
				Widgets.DrawShadowAround(rect);
				Widgets.DrawWindowBackground(rect);
				var prevAnchor = Text.Anchor;
				Text.Anchor = TextAnchor.MiddleCenter;
				Widgets.Label(rect, statusText);
				Text.Anchor = prevAnchor;
			}
		}
	}
}