using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
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
		private const float ModEntryExpandButtonPadding = 20f;
		private const float ModEntryShowSettingsButtonHeight = 34f;
		private const float HandleEntryPadding = 3f;
		private const float HandleEntryHeight = 34f;
		private const float ScrollBarWidthMargin = 18f;

		private readonly List<ModEntry> listedMods = new List<ModEntry>();
		private readonly HashSet<ModEntry> expandedModEntries = new HashSet<ModEntry>();
		private readonly Color ModEntryLineColor = new Color(0.3f, 0.3f, 0.3f);
		private readonly Color BadValueOutlineColor = new Color(.9f, .1f, .1f, 1f);
		private readonly Dictionary<SettingHandle, HandleControlInfo> handleControlInfo = new Dictionary<SettingHandle, HandleControlInfo>();
		private readonly SettingsHandleDrawer defaultHandleDrawer;
		private readonly Dictionary<Type, SettingsHandleDrawer> handleDrawers;
		private readonly CachedLabel labelShowVanillaSettings = CachedLabel.FromKey("HugsLib_setting_show_settings");
		private readonly CachedLabel labelExpandModEntry = CachedLabel.FromKey("HugsLib_setting_expand_mod");
		private readonly CachedLabel labelCollapseModEntry = CachedLabel.FromKey("HugsLib_setting_collapse_mod");
		private readonly float expandableToggleButtonWidth;

		private Vector2 scrollPosition;
		private float totalContentHeight;
		private string currentlyDrawnEntry;
		private bool closingScheduled;

		internal IModSettingsWindowState WindowState { get; set; }

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
			expandableToggleButtonWidth = 
				Mathf.Max(labelExpandModEntry.Size.x, labelCollapseModEntry.Size.x) + ModEntryExpandButtonPadding;
			defaultHandleDrawer = DrawHandleInputText;
			// these pairs specify which type of input field will be drawn for handles of this type. defaults to the string input
			handleDrawers = new Dictionary<Type, SettingsHandleDrawer> {
				{typeof(int), DrawHandleInputSpinner},
				{typeof(bool), DrawHandleInputCheckbox},
				{typeof(Enum), DrawHandleInputEnum}
			};
		}

		public override void PreOpen() {
			base.PreOpen();
			EnumerateModsWithSettings();
			RefreshSettingsHandles();
			PopulateControlInfo();
			TryRestoreWindowState(WindowState);
		}

		public override void PostClose() {
			base.PostClose();
			TrySaveWindowState(WindowState);
			HugsLibController.Instance.Settings.SaveChanges();
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
				ShowResetPrompt("HugsLib_settings_resetAll_prompt".Translate(),
					HugsLibController.SettingsManager.ModSettingsPacks.SelectMany(p => p.Handles));
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
			var entryTitleRect = new Rect(0f, curY, width, ModEntryLabelHeight);
			var mouseOverTitle = Mouse.IsOver(entryTitleRect);
			if (mouseOverTitle) {
				Widgets.DrawHighlight(entryTitleRect);
			}
			var labelRect = entryTitleRect.ContractedBy(ModEntryLabelPadding);
			Text.Font = GameFont.Medium;
			Widgets.Label(labelRect, entry.ModName);
			Text.Font = GameFont.Small;

			var entryButtonsTopRight = new Vector2(width, curY);
			var activateButtonWidth = DrawActivateEntryButton(entryButtonsTopRight);
			DrawFloatMenuButton(new Vector2(entryButtonsTopRight.x - activateButtonWidth, entryButtonsTopRight.y));

			curY += ModEntryLabelHeight;
			var color = GUI.color;
			GUI.color = ModEntryLineColor;
			Widgets.DrawLineHorizontal(0f, curY, width);
			GUI.color = color;
			curY += ModEntryLabelPadding;

			float DrawActivateEntryButton(Vector2 topRight) {
				if (entry.SettingsPack == null || !entry.SettingsPack.AlwaysExpandEntry) {
					var isExpanded = expandedModEntries.Contains(entry);
					CachedLabel buttonLabel;
					var isVanillaEntry = entry.SettingsPack == null;
					float toggleButtonWidth;
					if (isVanillaEntry) {
						buttonLabel = labelShowVanillaSettings;
						toggleButtonWidth = labelShowVanillaSettings.Size.x + ModEntryExpandButtonPadding;
					} else {
						buttonLabel = isExpanded ? labelCollapseModEntry : labelExpandModEntry;
						toggleButtonWidth = expandableToggleButtonWidth;
					}
					var buttonRect = new Rect(topRight.x - (toggleButtonWidth + ModEntryLabelPadding),
						topRight.y + (ModEntryLabelHeight - ModEntryShowSettingsButtonHeight) / 2f,
						toggleButtonWidth, ModEntryShowSettingsButtonHeight);
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
					return topRight.x - buttonRect.x;
				}
				return 0f;
			}

			void DrawFloatMenuButton(Vector2 topRight) {
				if(!mouseOverTitle) return;
				var buttonTopRight = new Vector2(topRight.x - ModEntryLabelPadding,
					topRight.y + (ModEntryLabelHeight - ModSettingsWidgets.HoverMenuHeight) / 2f);
				var hasExtraMenuEntries = entry.SettingsPack?.ContextMenuEntries != null;
				if (ModSettingsWidgets.DrawHoverMenuButton(
					buttonTopRight, entry.HasContextMenuEntries, hasExtraMenuEntries)){ 
					OpenModEntryContextMenu();
				}

				void OpenModEntryContextMenu() {
					var resetOptionLabel = 
						entry.SettingsPack.CanBeReset ? "HugsLib_settings_resetMod".Translate(entry.ModName) : null;
					ModSettingsWidgets.OpenExtensibleContextMenu(resetOptionLabel, 
						OnResetOptionSelected, delegate {}, entry.SettingsPack.ContextMenuEntries);
				}

				void OnResetOptionSelected() {
					ShowResetPrompt("HugsLib_settings_resetMod_prompt".Translate(entry.ModName),
						entry.SettingsPack.Handles);
				}
			}
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
				var entryRect = new Rect(parentRect.x, parentRect.y + curY, parentRect.width, entryHeight);
				var mouseOverEntry = Mouse.IsOver(entryRect);
				if (mouseOverEntry) Widgets.DrawHighlight(entryRect);
				var trimmedEntryRect = entryRect.ContractedBy(HandleEntryPadding); 
				bool valueChanged = false;
				if (handle.CustomDrawerFullWidth != null) {
					try {
						valueChanged = handle.CustomDrawerFullWidth(trimmedEntryRect);
					} catch (Exception e) {
						HugsLibController.Logger.ReportException(e, currentlyDrawnEntry, true,
							$"{nameof(SettingHandle)}.{nameof(SettingHandle.CustomDrawerFullWidth)}");
					}
				} else {
					valueChanged = DrawDefaultHandleEntry(handle, trimmedEntryRect, mouseOverEntry);
				}
				if (valueChanged) {
					if (handle.ValueType.IsClass) {
						// required for SettingHandleConvertible values to be eligible for saving,
						// since changes in reference-type values can't be automatically detected
						handle.HasUnsavedChanges = true;
					}
				}
			}
			curY += entryHeight;
		}

		private bool DrawDefaultHandleEntry(SettingHandle handle, Rect trimmedEntryRect, bool mouseOverEntry) {
			var controlRect = new Rect(trimmedEntryRect.x + trimmedEntryRect.width / 2f, trimmedEntryRect.y,
				trimmedEntryRect.width / 2f, trimmedEntryRect.height);
			GenUI.SetLabelAlign(TextAnchor.MiddleLeft);
			var leftHalfRect = new Rect(trimmedEntryRect.x, trimmedEntryRect.y,
				trimmedEntryRect.width / 2f - HandleEntryPadding, trimmedEntryRect.height);
			// give full width to the label if custom control drawer is used- this allows handle titles to be used as section titles
			var labelRect = handle.CustomDrawer == null ? leftHalfRect : trimmedEntryRect;
			// reduce text size if label is long and wraps over to the second line
			var controlInfo = handleControlInfo[handle];
			var cachedTitle = controlInfo.handleTitle ?? (controlInfo.handleTitle = new CachedLabel(handle.Title));
			if (cachedTitle.GetHeight(labelRect.width) > labelRect.height) {
				Text.Font = GameFont.Tiny;
				labelRect = new Rect(labelRect.x, labelRect.y - 1f, labelRect.width, labelRect.height + 2f);
			} else {
				Text.Font = GameFont.Small;
			}
			Widgets.Label(labelRect, cachedTitle.Text);
			Text.Font = GameFont.Small;
			GenUI.ResetLabelAlign();
			var valueChanged = false;
			if (handle.CustomDrawer == null) {
				var handleType = handle.ValueType;
				if (handleType.IsEnum) handleType = typeof(Enum);
				handleDrawers.TryGetValue(handleType, out var drawer);
				if (drawer == null) drawer = defaultHandleDrawer;
				valueChanged = drawer(handle, controlRect, controlInfo);
			} else {
				try {
					valueChanged = handle.CustomDrawer(controlRect);
				} catch (Exception e) {
					HugsLibController.Logger.ReportException(e, currentlyDrawnEntry, true,
						$"{nameof(SettingHandle)}.{nameof(SettingHandle.CustomDrawer)}");
				}
			}
			if (mouseOverEntry) {
				DrawEntryHoverMenu(trimmedEntryRect, handle);
			}
			return valueChanged;
		}

		private void DrawEntryHoverMenu(Rect entryRect, SettingHandle handle) {
			var topRight = new Vector2(
				entryRect.x + entryRect.width / 2f - HandleEntryPadding,
				entryRect.y + entryRect.height / 2f - ModSettingsWidgets.HoverMenuHeight / 2f
			);
			var includeResetEntry = handle.CanBeReset && !handle.Unsaved;
			var menuHasExtraOptions = handle.ContextMenuEntries != null;
			var menuEnabled = includeResetEntry || menuHasExtraOptions;
			var menuButtonClicked = ModSettingsWidgets.DrawHandleHoverMenu(
				topRight, handle.Description, menuEnabled, menuHasExtraOptions);
			if (menuButtonClicked) {
				OpenHandleContextMenu();
			}

			void OpenHandleContextMenu() {
				var resetOptionLabel = handle.CanBeReset ? "HugsLib_settings_resetValue".Translate() : null;
				ModSettingsWidgets.OpenExtensibleContextMenu(resetOptionLabel, 
					() => ResetSettingHandles(handle), delegate {}, handle.ContextMenuEntries);
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
				ModSettingsWidgets.OpenFloatMenu(floatOptions);
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

		private void TryRestoreWindowState(IModSettingsWindowState state) {
			if (state == null) return;
			expandedModEntries.Clear();
			var expandedIdSet = (state.ExpandedSettingPackIds ?? Enumerable.Empty<string>()).ToHashSet();
			expandedModEntries.AddRange(listedMods.Where(
				m => m.SettingsPack != null && expandedIdSet.Contains(m.SettingsPack.ModId)
			));
			scrollPosition = new Vector2(0f, state.VerticalScrollPosition);
		}
		
		private void TrySaveWindowState(IModSettingsWindowState state) {
			if (state == null) return;
			state.ExpandedSettingPackIds = expandedModEntries
				.Select(e => e.SettingsPack?.ModId)
				.Where(id => id != null)
				.ToArray();
			state.VerticalScrollPosition = scrollPosition.y;
		}

		private void ShowResetPrompt(string message, IEnumerable<SettingHandle> handlesToReset) {
			var handles = handlesToReset.ToArray();
			var hiddenHandlesWithOwners = GetHiddenResettableHandles(handles)
				.GroupBy(h => h.ParentPack)
				.Select(grp => (
					grp.Key.EntryName,
					grp.Select(h => BlankStringToNull(h.Title) ?? h.Name).ToArray()
				));
			Find.WindowStack.Add(new Dialog_ConfirmReset(message, hiddenHandlesWithOwners, OnConfirmReset));

			string BlankStringToNull(string s) {
				return string.IsNullOrWhiteSpace(s) ? null : s;
			}

			void OnConfirmReset(Dialog_ConfirmReset dialog) {
				IEnumerable<SettingHandle> resetHandles = handles;
				if (!dialog.IncludeHidden) {
					resetHandles = handles.Except(GetHiddenResettableHandles(handles));
				}
				ResetSettingHandles(resetHandles.ToArray());
			}
		}

		private void ResetSettingHandles(params SettingHandle[] handles) {
			var resetCount = 0;
			foreach (var handle in handles) {
				if (handle == null || !handle.CanBeReset) continue;
				try {
					handle.ResetToDefault();
					resetCount++;
				} catch (Exception e) {
					HugsLibController.Logger.Error(
						$"Failed to reset handle {handle.ParentPack.ModId}.{handle.Name}: {e}");
				}
			}
			if (resetCount > 0) {
				Messages.Message("HugsLib_settings_resetSuccessMessage".Translate(resetCount), 
					MessageTypeDefOf.TaskCompletion);
			}
		}

		private void ResetHandleControlInfo(SettingHandle handle) {
			handleControlInfo[handle] = new HandleControlInfo(handle);
		}

		private static IEnumerable<SettingHandle> GetHiddenResettableHandles(IEnumerable<SettingHandle> handles) {
			return handles.Where(h => h.CanBeReset && h.NeverVisible);
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
					mod.Handles.AddRange(mod.SettingsPack.Handles
						.Where(h => !h.NeverVisible)
						.OrderBy(h => h.DisplayOrder)
					);
					foreach (var handle in mod.Handles) {
						handle.ValueChanged -= OnHandleValueChanged;
						handle.ValueChanged += OnHandleValueChanged;
					}
				}
			}
		}

		private void OnHandleValueChanged(SettingHandle handle) {
			ResetHandleControlInfo(handle);
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
			public readonly bool HasContextMenuEntries;

			public bool Visible {
				get { return VanillaMod != null || Handles.Count > 0; }
			}

			public ModEntry(string modName, ModSettingsPack settingsPack, Mod vanillaMod) {
				ModName = modName;
				SettingsPack = settingsPack;
				VanillaMod = vanillaMod;
				HasContextMenuEntries = settingsPack != null
					&& (settingsPack.CanBeReset || settingsPack.ContextMenuEntries != null);
			}
		}

		// support data for each settings handle to allow gui controls to properly display and validate
		private class HandleControlInfo {
			public readonly string controlName;
			public readonly List<string> enumNames;
			public bool badInput;
			public string inputValue;
			public bool validationScheduled;
			public CachedLabel handleTitle;

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