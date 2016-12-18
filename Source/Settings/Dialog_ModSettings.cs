using System;
using System.Collections.Generic;
using System.Linq;
using HugsLib.Utils;
using UnityEngine;
using Verse;

namespace HugsLib.Settings {
	/**
	 * An options window for all configurable settings exposed by mods using the library
	 */
	public class Dialog_ModSettings : Window {
		private delegate bool SettingsHandleDrawer(SettingHandle handle, Rect inRect, HandleControlInfo info);

		private const float TitleLabelHeight = 40f;
		private const float ModEntryLabelHeight = 40f;
		private const float ModEntryLabelPadding = 4f;
		private const float ModEntryHeight = ModEntryLabelHeight + ModEntryLabelPadding;
		private const float HandleEntryPadding = 3f;
		private const float HandleEntryHeight = 34f;
		private const float ScrollBarWidthMargin = 18f;

		private readonly List<ModEntry> listedMods = new List<ModEntry>();
		private readonly Color ModEntryLineColor = new Color(0.3f, 0.3f, 0.3f);
		private readonly Color BadValueOutlineColor = new Color(.9f, .1f, .1f, 1f);
		private readonly Dictionary<SettingHandle, HandleControlInfo> handleControlInfo = new Dictionary<SettingHandle, HandleControlInfo>();
		private readonly SettingsHandleDrawer defaultHandleDrawer;
		private readonly Dictionary<Type, SettingsHandleDrawer> handleDrawers; 

		private Vector2 scrollPosition;
		private float totalContentHeight;
		private bool settingsHaveChanged;
		private string currentlyDrawnEntry;
		private bool closingScheduled;
		
		public override Vector2 InitialSize {
			get { return new Vector2(650f, 700f); }
		}
		
		public Dialog_ModSettings() {
			closeOnEscapeKey = true;
			doCloseButton = false;
			doCloseX = true;
			forcePause = true;
			absorbInputAroundWindow = true;
			resizeable = false;
			defaultHandleDrawer = DrawHandleInputText;
			// these pairs specify which type of input field will be drawn for handles of this type. defaults to the string input
			handleDrawers = new Dictionary<Type, SettingsHandleDrawer> {
				{typeof (int), DrawHandleInputSpinner},
				{typeof (bool), DrawHandleInputCheckbox},
				{typeof (Enum), DrawHandleInputEnum}
			};
		}

		public override void PreOpen() {
			base.PreOpen();
			settingsHaveChanged = false;
			EnumerateSettings();
			PopulateControlInfo();
		}

		public override void PostClose() {
			base.PostClose();
			if(settingsHaveChanged) HugsLibController.Instance.Settings.SaveChanges();
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
					currentlyDrawnEntry = entry.ModName;
					DrawModEntryHeader(entry, scrollViewTotal.width, ref curY);
					for (int j = 0; j < entry.handles.Count; j++) {
						var handle = entry.handles[j];
						if (handle.VisibilityPredicate != null) {
							try {
								if(!handle.VisibilityPredicate()) continue;
							} catch (Exception e) {
								HugsLibController.Logger.ReportException(e, currentlyDrawnEntry, true, "SettingsHandle.VisibilityPredicate");
							}
						}
						bool skipDrawing = curY - scrollPosition.y + HandleEntryHeight < 0f || curY - scrollPosition.y > scrollViewVisible.height;
						DrawHandleEntry(handle, scrollViewTotal, ref curY, skipDrawing);
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
				Find.WindowStack.Add(new Dialog_Confirm("HugsLib_settings_resetAll_prompt".Translate(), ResetAllSettings, true));
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
			if(entry.ModName.NullOrEmpty()) return;
			var labelRect = new Rect(0f, curY, width, ModEntryLabelHeight).ContractedBy(ModEntryLabelPadding);
			Text.Font = GameFont.Medium;
			Widgets.Label(labelRect, entry.ModName);
			Text.Font = GameFont.Small;
			curY += ModEntryLabelHeight;
			var color = GUI.color;
			GUI.color = ModEntryLineColor;
			Widgets.DrawLineHorizontal(0f, curY, width);
			GUI.color = color;
			curY += ModEntryLabelPadding;
		}

		// draws the label and appropriate input for a single setting
		private void DrawHandleEntry(SettingHandle handle, Rect parentRect, ref float curY, bool skipDrawing) {
			if (!skipDrawing) {
				var entryRect = new Rect(parentRect.x, parentRect.y + curY, parentRect.width, HandleEntryHeight).ContractedBy(HandleEntryPadding);
				var mouseOver = Mouse.IsOver(entryRect);
				if (mouseOver) Widgets.DrawHighlight(entryRect);
				var controlRect = new Rect(entryRect.x + entryRect.width / 2f, entryRect.y, entryRect.width / 2f, entryRect.height);
				GenUI.SetLabelAlign(TextAnchor.MiddleLeft);
				var leftHalfRect = new Rect(entryRect.x, entryRect.y, entryRect.width / 2f - HandleEntryPadding, entryRect.height);
				var labelRect = handle.CustomDrawer == null ? leftHalfRect : entryRect; // give full width to the label just in case
				Widgets.Label(labelRect, handle.Title);
				GenUI.ResetLabelAlign();
				bool valueChanged = false;
				if (handle.CustomDrawer == null) {
					SettingsHandleDrawer drawer;
					var handleType = handle.ValueType;
					if (handleType.IsEnum) handleType = typeof (Enum);
					handleDrawers.TryGetValue(handleType, out drawer);
					if (drawer == null) drawer = defaultHandleDrawer;
					valueChanged = drawer(handle, controlRect, handleControlInfo[handle]);
				} else {
					try {
						valueChanged = handle.CustomDrawer(controlRect);
					} catch (Exception e) {
						HugsLibController.Logger.ReportException(e, currentlyDrawnEntry, true, "SettingsHandle.CustomDrawer");
					}
				}
				if (valueChanged) settingsHaveChanged = true;
				if (mouseOver) {
					if (!handle.Description.NullOrEmpty()) {
						TooltipHandler.TipRegion(entryRect, handle.Description);
					}
					if (Input.GetMouseButtonUp(1)) {
						var options = new List<FloatMenuOption>(1);
						options.Add(new FloatMenuOption("HugsLib_settings_resetValue".Translate(), () => {
							ResetSetting(handle);
						}));
						Find.WindowStack.Add(new FloatMenu(options));
					}
				}
			}
			curY += HandleEntryHeight;
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
				int parsed;
				if (int.TryParse(info.inputValue, out parsed)) {
					info.inputValue = (parsed - handle.SpinnerIncrement).ToString();
				}
				info.validationScheduled = true;
				changed = true;
			}
			if (Widgets.ButtonText(rightButtonRect, "+")) {
				int parsed;
				if (int.TryParse(info.inputValue, out parsed)) {
					info.inputValue = (parsed + handle.SpinnerIncrement).ToString();
				}
				info.validationScheduled = true;
				changed = true;
			}
			var textRect = new Rect(controlRect.x + buttonSize + 1, controlRect.y, controlRect.width - buttonSize*2 - 2f, controlRect.height);
			if (DrawHandleInputText(handle, textRect, info)) {
				changed = true;
			}
			return changed;
		}

		// draws the input control for boolean settings
		private bool DrawHandleInputCheckbox(SettingHandle handle, Rect controlRect, HandleControlInfo info) {
			const float defaultCheckboxHeight = 24f;
			var checkOn = bool.Parse(info.inputValue);
			Widgets.Checkbox(controlRect.x, controlRect.y + (controlRect.height - defaultCheckboxHeight)/2, ref checkOn);
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

		private void ResetAllSettings() {
			foreach (var pack in listedMods) {
				foreach (var handle in pack.handles) {
					handle.ResetToDefault();
				}
			}
			settingsHaveChanged = true;
			PopulateControlInfo();
		}

		private void ResetSetting(SettingHandle handle) {
			handle.ResetToDefault();
			handleControlInfo[handle] = new HandleControlInfo(handle);
			settingsHaveChanged = true;
		}

		private void EnumerateSettings() {
			listedMods.Clear();
			totalContentHeight = 0;
			var packs = HugsLibController.Instance.Settings.ModSettingsPacks.ToList();
			// sort by display priority, entry name
			packs.Sort((p1, p2) => {
				if (p1.DisplayPriority != p2.DisplayPriority) return p1.DisplayPriority.CompareTo(p2.DisplayPriority);
				return String.Compare(p1.EntryName, p2.EntryName, StringComparison.Ordinal);
			});
			foreach (var pack in packs) {
				var handles = pack.Handles.Where(h => !h.NeverVisible).ToList();
				if(handles.Count == 0) continue;
				totalContentHeight += ModEntryHeight + handles.Count * HandleEntryHeight;
				listedMods.Add(new ModEntry(pack.EntryName, handles));
			}
		}

		private void PopulateControlInfo() {
			handleControlInfo.Clear();
			for (int i = 0; i < listedMods.Count; i++) {
				for (int j = 0; j < listedMods[i].handles.Count; j++) {
					var handle = listedMods[i].handles[j];
					handleControlInfo.Add(handle, new HandleControlInfo(handle));
				}
			}
		}

		private class ModEntry {
			public readonly string ModName;
			public readonly List<SettingHandle> handles;

			public ModEntry(string modName, List<SettingHandle> handles) {
				ModName = modName;
				this.handles = handles;
			}
		}

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