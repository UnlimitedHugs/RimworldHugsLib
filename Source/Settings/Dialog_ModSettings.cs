using System;
using System.Collections.Generic;
using System.Linq;
using HugsLib.Utils;
using UnityEngine;
using Verse;

namespace HugsLib.Settings {
	/// <summary>
	/// An options window for all configurable settings exposed by mods using the library
	/// </summary>
	public class Dialog_ModSettings : Window {
		private delegate bool SettingsHandleDrawer(SettingHandle handle, Rect inRect, HandleControlInfo info);

		private const float TitleLabelHeight = 40f;
		private const float ModEntryLabelHeight = 40f;
		private const float ModEntryLabelPadding = 4f;
		private const float ModEntryShowSettingsButtonHeight = 34f;
		private const float HandleEntryPadding = 3f;
		private const float HandleEntryHeight = 34f;
		private const float ScrollBarWidthMargin = 18f;

		// made static to preserve values between dialog openings
		private static readonly List<ModEntry> listedMods = new List<ModEntry>();
		private static readonly HashSet<ModEntry> expandedModEntries = new HashSet<ModEntry>();
		private static Vector2 scrollPosition;
		
		private readonly Color ModEntryLineColor = new Color(0.3f, 0.3f, 0.3f);
		private readonly Color BadValueOutlineColor = new Color(.9f, .1f, .1f, 1f);
		private readonly Dictionary<SettingHandle, HandleControlInfo> handleControlInfo = new Dictionary<SettingHandle, HandleControlInfo>();
		private readonly SettingsHandleDrawer defaultHandleDrawer;
		private readonly Dictionary<Type, SettingsHandleDrawer> handleDrawers;
		private readonly ModSettingsHoverMenu hoverMenu;

		private float totalContentHeight;
		// TodoMajor: remove this field, leverage the change detection system built into SettingHandle
		private bool settingsHaveChanged;
		private string currentlyDrawnEntry;
		private bool closingScheduled;

		public override Vector2 InitialSize {
			get { return new Vector2(650f, 700f); }
		}

		public Dialog_ModSettings() {
			closeOnCancel = true;
			closeOnAccept = false;
			doCloseButton = false;
			doCloseX = true;
			forcePause = true;
			absorbInputAroundWindow = true;
			resizeable = false;
			defaultHandleDrawer = DrawHandleInputText;
			hoverMenu = new ModSettingsHoverMenu();
			hoverMenu.HandleReset += OnHoverMenuHandleReset;
			// these pairs specify which type of input field will be drawn for handles of this type. defaults to the string input
			handleDrawers = new Dictionary<Type, SettingsHandleDrawer> {
				{typeof(int), DrawHandleInputSpinner},
				{typeof(bool), DrawHandleInputCheckbox},
				{typeof(Enum), DrawHandleInputEnum}
			};
		}

		public override void PreOpen() {
			base.PreOpen();
			settingsHaveChanged = false;
			if (listedMods.Count == 0) {
				EnumerateModsWithSettings();
			}
			RefreshSettingsHandles();
			PopulateControlInfo();
		}

		public override void PostClose() {
			base.PostClose();
			if (settingsHaveChanged) HugsLibController.Instance.Settings.SaveChanges();
		}

		public override void DoWindowContents(Rect inRect) {
			var windowButtonSize = CloseButSize;
			var contentRect = new Rect(0, 0, inRect.width, inRect.height - (windowButtonSize.y + 10f)).ContractedBy(10f);
			GUI.BeginGroup(contentRect);
			var titleRect = new Rect(0f, 0f, contentRect.width, TitleLabelHeight);
			Text.Font = GameFont.Medium;
			GenUI.SetLabelAlign(TextAnchor.MiddleCenter);
			Widgets.Label(titleRect, "HugsLib_settings_windowTitle".Translate());
			GenUI.ResetLabelAlign();
			Text.Font = GameFont.Small;
			if (listedMods.Count > 0) {
				var scrollViewVisible = new Rect(0f, titleRect.height, contentRect.width, contentRect.height - titleRect.height);
				var scrollBarVisible = totalContentHeight > scrollViewVisible.height;
				var scrollViewTotal = new Rect(0f, 0f, scrollViewVisible.width - (scrollBarVisible ? ScrollBarWidthMargin : 0), totalContentHeight);
				Widgets.BeginScrollView(scrollViewVisible, ref scrollPosition, scrollViewTotal);
				var curY = 0f;
				for (int i = 0; i < listedMods.Count; i++) {
					var entry = listedMods[i];
					if (!entry.Visible) continue;
					currentlyDrawnEntry = entry.ModName;
					DrawModEntryHeader(entry, scrollViewTotal.width, ref curY);
					if ((entry.SettingsPack != null && entry.SettingsPack.AlwaysExpandEntry) || expandedModEntries.Contains(entry)) {
						for (int j = 0; j < entry.Handles.Count; j++) {
							var handle = entry.Handles[j];
							if (handle.VisibilityPredicate != null) {
								try {
									if (!handle.VisibilityPredicate()) continue;
								} catch (Exception e) {
									HugsLibController.Logger.ReportException(e, currentlyDrawnEntry, true, "SettingsHandle.VisibilityPredicate");
								}
							}
							DrawHandleEntry(handle, scrollViewTotal, ref curY, scrollViewVisible.height);
						}
					}
					currentlyDrawnEntry = null;
				}
				Widgets.EndScrollView();
				totalContentHeight = curY;
			} else {
				Widgets.Label(new Rect(0, titleRect.height, contentRect.width, titleRect.height), "HugsLib_settings_noSettings".Translate());
			}
			GUI.EndGroup();
			Text.Font = GameFont.Small;
			var resetButtonRect = new Rect(0, inRect.height - windowButtonSize.y, windowButtonSize.x, windowButtonSize.y);
			if (Widgets.ButtonText(resetButtonRect, "HugsLib_settings_resetAll".Translate())) {
				Find.WindowStack.Add(new Dialog_Confirm("HugsLib_settings_resetAll_prompt".Translate(), ResetHugsLibSettingsForLoadedMods, true));
			}
			var closeButtonRect = new Rect(inRect.width - windowButtonSize.x, inRect.height - windowButtonSize.y, windowButtonSize.x, windowButtonSize.y);
			if (closingScheduled) {
				closingScheduled = false;
				Close();
			}
			if (Widgets.ButtonText(closeButtonRect, "CloseButton".Translate())) {
				GUI.FocusControl(null); // unfocus, so that a focused text field may commit its value
				closingScheduled = true;
			}
		}

		// draws the header with the name of the mod
		private void DrawModEntryHeader(ModEntry entry, float width, ref float curY) {
			if (entry.ModName.NullOrEmpty()) return;
			var labelRect = new Rect(0f, curY, width, ModEntryLabelHeight).ContractedBy(ModEntryLabelPadding);
			Text.Font = GameFont.Medium;
			Widgets.Label(labelRect, entry.ModName);
			Text.Font = GameFont.Small;
			// draw open setting or expand handle listing button
			if (entry.SettingsPack == null || !entry.SettingsPack.AlwaysExpandEntry) {
				var isVanillaEntry = entry.SettingsPack == null;
				var isExpanded = expandedModEntries.Contains(entry);
				string buttonLabel;
				if (isVanillaEntry) {
					buttonLabel = "HugsLib_setting_show_settings";
				} else {
					buttonLabel = isExpanded ? "HugsLib_setting_collapse_mod" : "HugsLib_setting_expand_mod";
				}
				buttonLabel = buttonLabel.Translate();
				var buttonWidth = Text.CalcSize(buttonLabel).x + 20f;
				var buttonRect = new Rect(width - (buttonWidth + HandleEntryPadding), curY + (ModEntryLabelHeight - ModEntryShowSettingsButtonHeight) / 2f, buttonWidth,
					ModEntryShowSettingsButtonHeight);
				if (Widgets.ButtonText(buttonRect, buttonLabel)) {
					if (isVanillaEntry) {
						Find.WindowStack.Add(new Dialog_VanillaModSettings(entry.VanillaMod));
					} else {
						if (isExpanded) {
							expandedModEntries.Remove(entry);
						} else {
							expandedModEntries.Add(entry);
						}
					}
				}
			}

			curY += ModEntryLabelHeight;
			var color = GUI.color;
			GUI.color = ModEntryLineColor;
			Widgets.DrawLineHorizontal(0f, curY, width);
			GUI.color = color;
			curY += ModEntryLabelPadding;
		}

		// draws the label and appropriate input for a single setting
		private void DrawHandleEntry(SettingHandle handle, Rect parentRect, ref float curY, float scrollViewHeight) {
			var entryHeight = HandleEntryHeight;
			var anyCustomDrawer = handle.CustomDrawerFullWidth ?? handle.CustomDrawer;
			if (anyCustomDrawer != null && handle.CustomDrawerHeight > entryHeight) {
				entryHeight = handle.CustomDrawerHeight + HandleEntryPadding * 2;
			}
			var skipDrawing = curY - scrollPosition.y + entryHeight < 0f || curY - scrollPosition.y > scrollViewHeight;
			if (!skipDrawing) {
				var entryRect = new Rect(parentRect.x, parentRect.y + curY, parentRect.width, entryHeight)
					.ContractedBy(HandleEntryPadding);
				bool valueChanged = false;
				if (handle.CustomDrawerFullWidth != null) {
					try {
						valueChanged = handle.CustomDrawerFullWidth(entryRect);
					} catch (Exception e) {
						HugsLibController.Logger.ReportException(e, currentlyDrawnEntry, true,
							$"{nameof(SettingHandle)}.{nameof(SettingHandle.CustomDrawerFullWidth)}");
					}
				} else {
					valueChanged = DrawDefaultHandleEntry(handle, entryRect);
				}
				if (valueChanged) {
					FlagConvertibleHandleAsModified(handle);
					settingsHaveChanged = true;
				}
			}
			curY += entryHeight;
		}

		private bool DrawDefaultHandleEntry(SettingHandle handle, Rect entryRect) {
			var mouseOverEntry = Mouse.IsOver(entryRect);
			if (mouseOverEntry) Widgets.DrawHighlight(entryRect);
			var controlRect = new Rect(entryRect.x + entryRect.width / 2f, entryRect.y,
				entryRect.width / 2f, entryRect.height);
			GenUI.SetLabelAlign(TextAnchor.MiddleLeft);
			var leftHalfRect = new Rect(entryRect.x, entryRect.y,
				entryRect.width / 2f - HandleEntryPadding, entryRect.height);
			// give full width to the label if custom control drawer is used- this allows handle titles to be used as section titles
			var labelRect = handle.CustomDrawer == null ? leftHalfRect : entryRect;
			// reduce text size if label is long and wraps over to the second line
			var expectedLabelHeight = Text.CalcHeight(handle.Title, labelRect.width);
			if (expectedLabelHeight > labelRect.height) {
				Text.Font = GameFont.Tiny;
				labelRect = new Rect(labelRect.x, labelRect.y - 1f, labelRect.width, labelRect.height + 2f);
			} else {
				Text.Font = GameFont.Small;
			}
			Widgets.Label(labelRect, handle.Title);
			Text.Font = GameFont.Small;
			GenUI.ResetLabelAlign();
			var valueChanged = false;
			if (handle.CustomDrawer == null) {
				var handleType = handle.ValueType;
				if (handleType.IsEnum) handleType = typeof(Enum);
				handleDrawers.TryGetValue(handleType, out var drawer);
				if (drawer == null) drawer = defaultHandleDrawer;
				valueChanged = drawer(handle, controlRect, handleControlInfo[handle]);
			} else {
				try {
					valueChanged = handle.CustomDrawer(controlRect);
				} catch (Exception e) {
					HugsLibController.Logger.ReportException(e, currentlyDrawnEntry, true,
						$"{nameof(SettingHandle)}.{nameof(SettingHandle.CustomDrawer)}");
				}
			}
			if (mouseOverEntry) {
				DrawEntryHoverMenu(entryRect, handle);
			}
			return valueChanged;
		}

		private void DrawEntryHoverMenu(Rect entryRect, IHoverMenuHandle handle) {
			var menuSize = hoverMenu.DrawSize;
			var hoverMenuPos = new Vector2(
				entryRect.x + entryRect.width / 2f - HandleEntryPadding - menuSize.x,
				entryRect.y + entryRect.height / 2f - menuSize.y / 2f
			);
			hoverMenu.Draw(hoverMenuPos, handle);
		}

		private static void FlagConvertibleHandleAsModified(SettingHandle handle) {
			if (handle.ValueType.IsClass) {
				// required for SettingHandleConvertible values to be eligible for saving,
				// since changes in reference-type values can't be automatically detected
				handle.HasUnsavedChanges = true;
			}
		}
		
		// draws the input control for string settings
		private bool DrawHandleInputText(SettingHandle handle, Rect controlRect, HandleControlInfo info) {
			var evt = Event.current;
			GUI.SetNextControlName(info.controlName);
			info.inputValue = Widgets.TextField(controlRect, info.inputValue);
			var focused = GUI.GetNameOfFocusedControl() == info.controlName;
			if (focused) {
				info.validationScheduled = true;
				if (evt.type == EventType.KeyUp && (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)) {
					focused = false;
				}
			}
			var changed = false;
			if (info.validationScheduled && !focused) {
				try {
					if (handle.Validator != null && !handle.Validator(info.inputValue)) {
						info.badInput = true;
					} else {
						info.badInput = false;
						handle.StringValue = info.inputValue;
						changed = true;
					}
				} catch (Exception e) {
					HugsLibController.Logger.ReportException(e, currentlyDrawnEntry, false, "SettingsHandle.Validator");
				}
				info.validationScheduled = false;
			}
			if (info.badInput) {
				DrawBadTextValueOutline(controlRect);
			}
			return changed;
		}

		// draws the input control for integer settings
		private bool DrawHandleInputSpinner(SettingHandle handle, Rect controlRect, HandleControlInfo info) {
			var buttonSize = controlRect.height;
			var leftButtonRect = new Rect(controlRect.x, controlRect.y, buttonSize, buttonSize);
			var rightButtonRect = new Rect(controlRect.x + controlRect.width - buttonSize, controlRect.y, buttonSize, buttonSize);
			var changed = false;
			if (Widgets.ButtonText(leftButtonRect, "-")) {
				if (int.TryParse(info.inputValue, out var parsed)) {
					info.inputValue = (parsed - handle.SpinnerIncrement).ToString();
				}
				info.validationScheduled = true;
				changed = true;
			}
			if (Widgets.ButtonText(rightButtonRect, "+")) {
				if (int.TryParse(info.inputValue, out var parsed)) {
					info.inputValue = (parsed + handle.SpinnerIncrement).ToString();
				}
				info.validationScheduled = true;
				changed = true;
			}
			var textRect = new Rect(controlRect.x + buttonSize + 1, controlRect.y, controlRect.width - buttonSize * 2 - 2f, controlRect.height);
			if (DrawHandleInputText(handle, textRect, info)) {
				changed = true;
			}
			return changed;
		}

		// draws the input control for boolean settings
		private bool DrawHandleInputCheckbox(SettingHandle handle, Rect controlRect, HandleControlInfo info) {
			const float defaultCheckboxHeight = 24f;
			var checkOn = bool.Parse(info.inputValue);
			Widgets.Checkbox(controlRect.x, controlRect.y + (controlRect.height - defaultCheckboxHeight) / 2, ref checkOn);
			if (checkOn != bool.Parse(info.inputValue)) {
				handle.StringValue = info.inputValue = checkOn.ToString();
				return true;
			}
			return false;
		}

		// draws the input control for Enum settings
		private bool DrawHandleInputEnum(SettingHandle handle, Rect controlRect, HandleControlInfo info) {
			if (info.enumNames == null) return false;
			var readableValue = (handle.EnumStringPrefix + info.inputValue).Translate();
			if (Widgets.ButtonText(controlRect, readableValue)) {
				var floatOptions = new List<FloatMenuOption>();
				foreach (var valueName in info.enumNames) {
					var name = valueName;
					var readableOption = (handle.EnumStringPrefix + name).Translate();
					floatOptions.Add(new FloatMenuOption(readableOption, () => {
						handle.StringValue = info.inputValue = name;
						info.validationScheduled = true;
					}));
				}
				Find.WindowStack.Add(new FloatMenu(floatOptions));
			}
			if (info.validationScheduled) {
				info.validationScheduled = false;
				return true;
			}
			return false;
		}

		private void DrawBadTextValueOutline(Rect rect) {
			var prevColor = GUI.color;
			GUI.color = BadValueOutlineColor;
			Widgets.DrawBox(rect);
			GUI.color = prevColor;
		}

		private void ResetHugsLibSettingsForLoadedMods() {
			foreach (var pack in HugsLibController.Instance.Settings.ModSettingsPacks) {
				foreach (var handle in pack.Handles) {
					handle.ResetToDefault();
				}
			}
			settingsHaveChanged = true;
			PopulateControlInfo();
		}

		private void OnHoverMenuHandleReset(IHoverMenuHandle handle) {
			if (handle is SettingHandle settingHandle) {
				handleControlInfo[settingHandle] = new HandleControlInfo(settingHandle);
			}
			settingsHaveChanged = true;
		}
		
		// TodoMajor: remove this
		private void ResetSetting(SettingHandle handle) {
			if (!handle.CanBeReset) return;
			handle.ResetToDefault();
			handleControlInfo[handle] = new HandleControlInfo(handle);
			FlagConvertibleHandleAsModified(handle);
			settingsHaveChanged = true;
		}

		// pulls all available mods with settings to display
		private void EnumerateModsWithSettings() {
			try {
				listedMods.Clear();
				expandedModEntries.Clear();
				totalContentHeight = 0;

				// get HugsLib settings packs
				foreach (var pack in HugsLibController.Instance.Settings.ModSettingsPacks) {
					try {
						var entry = new ModEntry(pack.EntryName, pack, null) {
							DisplayPriority = pack.DisplayPriority
						};
						listedMods.Add(entry);
					} catch (Exception e) {
						HugsLibController.Logger.Error("Exception while enumerating HugsLib settings for {0}: {1}", pack.ModId, e);
					}
				}
				// get vanilla mods
				foreach (var mod in LoadedModManager.ModHandles) {
					if (mod == null) continue;
					try {
						if (!mod.SettingsCategory().NullOrEmpty()) {
							listedMods.Add(new ModEntry(mod.SettingsCategory(), null, mod));
						}
					} catch (Exception e) {
						HugsLibController.Logger.Error("Exception while enumerating vanilla settings for {0}: {1}", mod.GetType(), e);
					}
				}
				// normalize improperly named mods
				foreach (var listedMod in listedMods) {
					if (listedMod.ModName.NullOrEmpty()) {
						listedMod.ModName = "HugsLib_setting_unnamed_mod".Translate();
						listedMod.DisplayPriority = ModSettingsPack.ListPriority.Lower;
					}
				}
				// sort by display priority, entry name
				listedMods.Sort((p1, p2) => {
					if (p1.DisplayPriority != p2.DisplayPriority) return p1.DisplayPriority.CompareTo(p2.DisplayPriority);
					return String.Compare(p1.ModName, p2.ModName, StringComparison.Ordinal);
				});
			} catch (Exception e) {
				HugsLibController.Logger.ReportException(e);
			}
		}

		// updated the settings handles for all HugsLib mods
		private void RefreshSettingsHandles() {
			foreach (var mod in listedMods) {
				if (mod.SettingsPack != null) {
					mod.Handles.Clear();
					mod.Handles.AddRange(mod.SettingsPack.Handles.Where(h => !h.NeverVisible).OrderBy(h => h.DisplayOrder));
				}
			}
		}

		// prepares support objects to store data for settings handle controls
		private void PopulateControlInfo() {
			handleControlInfo.Clear();
			for (int i = 0; i < listedMods.Count; i++) {
				for (int j = 0; j < listedMods[i].Handles.Count; j++) {
					var handle = listedMods[i].Handles[j];
					handleControlInfo.Add(handle, new HandleControlInfo(handle));
				}
			}
		}

		// stores a mod to be displayed in the settings listing
		private class ModEntry {
			public string ModName;
			public ModSettingsPack.ListPriority DisplayPriority;
			public List<SettingHandle> Handles = new List<SettingHandle>();
			public readonly ModSettingsPack SettingsPack;
			public readonly Mod VanillaMod;

			public bool Visible {
				get { return VanillaMod != null || Handles.Count > 0; }
			}

			public ModEntry(string modName, ModSettingsPack settingsPack, Mod vanillaMod) {
				ModName = modName;
				SettingsPack = settingsPack;
				VanillaMod = vanillaMod;
			}
		}

		// support data for each settings handle to allow gui controls to properly display and validate
		private class HandleControlInfo {
			public readonly string controlName;
			public readonly List<string> enumNames;
			public bool badInput;
			public string inputValue;
			public bool validationScheduled;

			public HandleControlInfo(SettingHandle handle) {
				controlName = "control" + handle.GetHashCode();
				inputValue = handle.StringValue;
				enumNames = TryGetEnumNames(handle);
			}

			private List<string> TryGetEnumNames(SettingHandle handle) {
				var valueType = handle.ValueType;
				if (!valueType.IsEnum) return null;
				var values = Enum.GetValues(valueType);
				var result = new List<string>(values.Length);
				foreach (var value in values) {
					result.Add(Enum.GetName(valueType, value));
				}
				return result;
			}
		}
	}
}