using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HugsLib.Quickstart {
	/// <summary>
	/// Allows to change settings related to the custom quickstart functionality.
	/// Strings are not translated, since this is a tool exclusively for modders.
	/// </summary>
	public class Dialog_QuickstartSettings : Window {
		private readonly List<FileEntry> saveFiles = new List<FileEntry>();

		public override Vector2 InitialSize {
			get { return new Vector2(600f, 500f); }
		}

		public Dialog_QuickstartSettings() {
			closeOnCancel = true;
			closeOnAccept = false;
			doCloseButton = false;
			doCloseX = true;
			resizeable = false;
			draggable = true;
		}

		public override void PreOpen() {
			base.PreOpen();
			CacheSavedGameFiles();
			EnsureSettingsHaveValidFiles(QuickstartController.Settings);
		}

		public override void PostClose() {
			QuickstartController.SaveSettings();
		}

		public override void DoWindowContents(Rect inRect) {
			const float categoryPadding = 10f;
			const float categoryInset = 30f;
			const float radioLabelInset = 40f;
			const float mainListingSpacing = 6f;
			const float subListingSpacing = 6f;
			const float subListingLabelWidth = 100f;
			const float subListingRowHeight = 30f;
			const float checkboxListingWidth = 280f;
			const float listingColumnSpacing = 17f;
			const string shiftTip = "Hold Shift while starting up to prevent the quickstart.";
			var settings = QuickstartController.Settings;
			var mainListing = new Listing_Standard();
			mainListing.verticalSpacing = mainListingSpacing;
			mainListing.Begin(inRect);
			Text.Font = GameFont.Medium;
			mainListing.Label("Quickstart settings");
			Text.Font = GameFont.Small;
			mainListing.GapLine();
			mainListing.Gap();
			OperationModeRadioButton(mainListing, radioLabelInset, "Quickstart off", settings, QuickstartSettings.QuickstartMode.Disabled, 
				"Quickstart functionality is disabled.\nThe game starts normally.");
			OperationModeRadioButton(mainListing, radioLabelInset, "Quickstart: load save file", settings, QuickstartSettings.QuickstartMode.LoadMap,
				"Load the selected saved game right after launch.\n"+shiftTip);
			var expectedHeight = categoryPadding * 2 + (subListingRowHeight + subListingSpacing) * 1;
			MakeSubListing(mainListing, 0, expectedHeight, categoryPadding, categoryInset, subListingSpacing, (sub, width) => {
				sub.ColumnWidth = subListingLabelWidth;
				Text.Anchor = TextAnchor.MiddleLeft;
				var rect = sub.GetRect(subListingRowHeight);
				Widgets.Label(rect, "Save file:");
				Text.Anchor = TextAnchor.UpperLeft;
				sub.NewColumn();
				sub.ColumnWidth = width - subListingLabelWidth - listingColumnSpacing;
				MakeSelectSaveButton(sub, settings);
			});
			OperationModeRadioButton(mainListing, radioLabelInset, "Quickstart: generate map", settings, QuickstartSettings.QuickstartMode.GenerateMap,
				"Generate a new map right after launch.\nWorks the same as using the \"quicktest\" command line option.\n" + shiftTip);
			expectedHeight = categoryPadding * 2 + (subListingRowHeight + subListingSpacing) * 2;
			MakeSubListing(mainListing, 0, expectedHeight, categoryPadding, categoryInset, subListingSpacing, (sub, width) => {
				sub.ColumnWidth = subListingLabelWidth;
				Text.Anchor = TextAnchor.MiddleLeft;
				var rect = sub.GetRect(subListingRowHeight);
				Widgets.Label(rect, "Scenario:");
				sub.Gap(subListingSpacing);
				rect = sub.GetRect(subListingRowHeight);
				Widgets.Label(rect, "Map size:");
				Text.Anchor = TextAnchor.UpperLeft;
				sub.NewColumn();
				sub.ColumnWidth = width - subListingLabelWidth - listingColumnSpacing;
				MakeSelectScenarioButton(sub, settings);
				MakeSelectMapSizeButton(sub, settings);
			});
			expectedHeight = categoryPadding * 2 + (subListingRowHeight + subListingSpacing) * 3;
			MakeSubListing(mainListing, checkboxListingWidth, expectedHeight, categoryPadding, 0f, subListingSpacing, (sub, width) => {
				sub.CheckboxLabeled("Abort quickstart on error", ref settings.StopOnErrors, "Prevent quickstart if errors are detected during startup.");
				sub.CheckboxLabeled("Abort quickstart on warning", ref settings.StopOnWarnings, "Prevent quickstart if warnings are detected during startup.");
				sub.CheckboxLabeled("Ignore version & mod config mismatch", ref settings.BypassSafetyDialog, "Skip the mod config mismatch dialog and load all saved games regardless.");
			});
			mainListing.End();
			Text.Anchor = TextAnchor.UpperLeft;

			var btnSize = new Vector2(180f, 40f);
			var buttonYStart = inRect.height - btnSize.y;
			if (Widgets.ButtonText(new Rect(inRect.width - btnSize.x, buttonYStart, btnSize.x, btnSize.y), "Close")) {
				Close();
			}
		}

		private void OperationModeRadioButton(Listing_Standard listing, float labelInset, string label, QuickstartSettings settings, QuickstartSettings.QuickstartMode assignedMode, string tooltip) {
			const float labelTopMargin = -4f;
			const float fontSize = 16f;
			float lineHeight = Text.LineHeight;
			var entryRect = listing.GetRect(lineHeight + listing.verticalSpacing);
			var labelRect = new Rect(entryRect.x + labelInset, entryRect.y + labelTopMargin, entryRect.width - labelInset, entryRect.height - labelTopMargin);
			var rowRect = new Rect(entryRect.x, entryRect.y, entryRect.width, entryRect.height);
			if (tooltip != null) {
				if (Mouse.IsOver(rowRect)) {
					Widgets.DrawHighlight(rowRect);
				}
				TooltipHandler.TipRegion(rowRect, tooltip);
			}
			if (Widgets.ButtonInvisible(rowRect)) {
				if (settings.OperationMode != assignedMode) {
					SoundDefOf.RadioButtonClicked.PlayOneShotOnCamera();
					QuickstartController.Settings.OperationMode = assignedMode;
				}
			}
			Widgets.RadioButton(entryRect.x, entryRect.y, settings.OperationMode == assignedMode);
			
			Text.Font = GameFont.Medium;
			var emphasizedLabel = string.Format("<size={0}>{1}</size>", fontSize, label);
			Widgets.Label(labelRect, emphasizedLabel);
			Text.Font = GameFont.Small;
		}

		private void MakeSubListing(Listing_Standard mainListing, float width, float allocatedHeight, float padding, float extraInset, float verticalSpacing, Action<Listing_Standard, float> drawContents) {
			var subRect = mainListing.GetRect(allocatedHeight);
			width = width > 0 ? width : subRect.width - (padding + extraInset);
			subRect = new Rect(subRect.x+padding+extraInset, subRect.y+padding, width, subRect.height-padding*2f);
			var sub = new Listing_Standard {verticalSpacing = verticalSpacing};
			sub.Begin(subRect);
			drawContents(sub, width);
			sub.End();
		}

		private void MakeSelectSaveButton(Listing_Standard sub, QuickstartSettings settings) {
			const float VersionLabelOffset = 10f;
			const float LoadNowWidth = 120f;
			const float HorizontalSpacing = 6f;
			const float ButtonHeight = 30f;
			var selected = settings.SaveFileToLoad;
			var buttonRect = sub.GetRect(ButtonHeight);
			var leftHalf = new Rect(buttonRect) {xMax = buttonRect.xMax - (LoadNowWidth + HorizontalSpacing)};
			var rightHalf = new Rect(buttonRect) { xMin = buttonRect.xMin + leftHalf.width + HorizontalSpacing };
			if (Widgets.ButtonText(leftHalf, selected ?? "Select a save file")) {
				var menu = new FloatMenu(saveFiles.Select(s => {
					return new FloatMenuOption(s.Label, () => { settings.SaveFileToLoad = s.Name; }, MenuOptionPriority.Default, null, null, Text.CalcSize(s.VersionLabel).x + VersionLabelOffset,
						rect => {
							var prevColor = GUI.color;
							GUI.color = s.FileInfo.VersionColor;
							Text.Anchor = TextAnchor.MiddleLeft;
							Widgets.Label(new Rect(rect.x + VersionLabelOffset, rect.y, 200f, rect.height), s.VersionLabel);
							Text.Anchor = TextAnchor.UpperLeft;
							GUI.color = prevColor;
							return false;
						}
					);
				}).ToList());
				Find.WindowStack.Add(menu);
			}
			if (Widgets.ButtonText(rightHalf, "Load now")) {
				if (!HugsLibUtility.ShiftIsHeld) {
					settings.OperationMode = QuickstartSettings.QuickstartMode.LoadMap;
				}
				QuickstartController.InitateSaveLoading();
				Close();
			}
			sub.Gap(sub.verticalSpacing);
		}

		private void MakeSelectScenarioButton(Listing_Standard sub, QuickstartSettings settings) {
			const float GenerateNowWidth = 120f;
			const float HorizontalSpacing = 6f;
			const float ButtonHeight = 30f;
			var buttonRect = sub.GetRect(ButtonHeight);
			var leftHalf = new Rect(buttonRect) { xMax = buttonRect.xMax - (GenerateNowWidth + HorizontalSpacing) };
			var rightHalf = new Rect(buttonRect) { xMin = buttonRect.xMin + leftHalf.width + HorizontalSpacing };
			var selected = settings.ScenarioToGen;
			if (Widgets.ButtonText(leftHalf, selected ?? "Select a scenario")) {
				var menu = new FloatMenu(ScenarioLister.AllScenarios().Select(s => {
					return new FloatMenuOption(s.name, () => { settings.ScenarioToGen = s.name; });
				}).ToList());
				Find.WindowStack.Add(menu);
			}
			if (Widgets.ButtonText(rightHalf, "Generate now")) {
				if (!HugsLibUtility.ShiftIsHeld) {
					settings.OperationMode = QuickstartSettings.QuickstartMode.GenerateMap;
				}
				QuickstartController.InitateMapGeneration();
				Close();
			}
			sub.Gap(sub.verticalSpacing);
		}

		private void MakeSelectMapSizeButton(Listing_Standard sub, QuickstartSettings settings) {
			var allSizes = QuickstartController.MapSizes;
			var selected = allSizes.Select(s => s.Size == settings.MapSizeToGen ? s.Label : null).FirstOrDefault(s => s != null);
			if (sub.ButtonText(selected ?? "Select a map size")) {
				var menu = new FloatMenu(allSizes.Select(s => {
					return new FloatMenuOption(s.Label, () => { settings.MapSizeToGen = s.Size; });
				}).ToList());
				Find.WindowStack.Add(menu);
			}
		}

		private void CacheSavedGameFiles() {
			saveFiles.Clear();
			foreach (var current in GenFilePaths.AllSavedGameFiles) {
				try {
					saveFiles.Add(new FileEntry(current));
				} catch (Exception) {
					// we don't care. just skip the file
				}
			}
		}

		private void EnsureSettingsHaveValidFiles(QuickstartSettings settings) {
			// make sure our settings are referencing loadable files
			if (saveFiles.Select(s => s.Name).All(s => s != settings.SaveFileToLoad)) {
				settings.SaveFileToLoad = null;
			}
			if (settings.ScenarioToGen != null && ScenarioLister.AllScenarios().All(s => s.name != settings.ScenarioToGen)) {
				settings.ScenarioToGen = null;
			}
			if (settings.ScenarioToGen == null) {
				settings.ScenarioToGen = ScenarioDefOf.Crashlanded.defName;
			}
		}

		private class FileEntry {
			public readonly string Name;
			public readonly string Label;
			public readonly string VersionLabel;
			public readonly SaveFileInfo FileInfo;
			public FileEntry(FileInfo file) {
				FileInfo = new SaveFileInfo(file);
				Name = Path.GetFileNameWithoutExtension(FileInfo.FileInfo.Name);
				Label = Name;
				VersionLabel = string.Format("({0})", FileInfo.GameVersion);
			}
		}
	}
}