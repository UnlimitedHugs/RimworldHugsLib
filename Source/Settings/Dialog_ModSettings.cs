using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace HugsLib.Settings {
	/// <summary>
	/// An options window for settings exposed by mods using the library
	/// </summary>
	public class Dialog_ModSettings : Window {
		private delegate bool SettingsHandleDrawer(SettingHandle handle, Rect inRect, HandleControlInfo info);

		private const float TitleLabelHeight = 40f;
		private const float ModEntryLabelHeight = 40f;
		private const float ModEntryLabelPadding = 4f;
		private const float HandleEntryPadding = 3f;
		private const float HandleEntryHeight = 34f;
		private const float ScrollBarWidthMargin = 18f;

		private readonly ModSettingsPack currentPack;
		private readonly string currentPackName;
		private readonly Color ModEntryLineColor = new Color(0.3f, 0.3f, 0.3f);
		private readonly Color BadValueOutlineColor = new Color(.9f, .1f, .1f, 1f);
		private readonly List<SettingHandle> handles = new List<SettingHandle>();
		private readonly Dictionary<SettingHandle, HandleControlInfo> handleControlInfo =
			new Dictionary<SettingHandle, HandleControlInfo>();
		private readonly SettingsHandleDrawer defaultHandleDrawer;
		private readonly Dictionary<Type, SettingsHandleDrawer> handleDrawers;

		private Vector2 scrollPosition;
		private float totalContentHeight;
		private bool closingScheduled;


		public override Vector2 InitialSize {
			get { return new Vector2(650f, 700f); }
		}

		public Dialog_ModSettings(ModSettingsPack pack) {
			currentPack = pack ?? throw new ArgumentNullException(nameof(pack));
			currentPackName = pack.EntryName.NullOrEmpty()
				? "HugsLib_setting_unnamed_mod".Translate().ToString()
				: pack.EntryName;
			closeOnCancel = true;
			closeOnAccept = false;
			doCloseButton = false;
			doCloseX = true;
			forcePause = true;
			absorbInputAroundWindow = true;
			resizeable = false;
			defaultHandleDrawer = DrawHandleInputText;
			// these pairs specify which type of input field will be drawn for handles of this type. defaults to the string input
			handleDrawers = new Dictionary<Type, SettingsHandleDrawer> {
				{ typeof(int), DrawHandleInputSpinner },
				{ typeof(bool), DrawHandleInputCheckbox },
				{ typeof(Enum), DrawHandleInputEnum }
			};
		}

		public override void PreOpen() {
			base.PreOpen();
			TryRestoreWindowState();
			RefreshSettingsHandles();
			RefreshSettingsHandles();
			PopulateControlInfo();
		}

		public override void PostClose() {
			base.PostClose();
			TrySaveWindowState();
			HugsLibController.Instance.Settings.SaveChanges();
		}

		public override void DoWindowContents(Rect inRect) {
			var windowButtonSize = CloseButSize;
			var contentRect =
				new Rect(0, 0, inRect.width, inRect.height - (windowButtonSize.y + 10f)).ContractedBy(10f);
			GUI.BeginGroup(contentRect);
			var titleRect = new Rect(0f, 0f, contentRect.width, TitleLabelHeight);
			Text.Font = GameFont.Medium;
			GenUI.SetLabelAlign(TextAnchor.MiddleCenter);
			Widgets.Label(titleRect, "HugsLib_settings_windowTitle".Translate());
			GenUI.ResetLabelAlign();
			Text.Font = GameFont.Small;
			var scrollViewVisible =
				new Rect(0f, titleRect.height, contentRect.width, contentRect.height - titleRect.height);
			var scrollBarVisible = totalContentHeight > scrollViewVisible.height;
			var scrollViewTotal = new Rect(0f, 0f,
				scrollViewVisible.width - (scrollBarVisible ? ScrollBarWidthMargin : 0), totalContentHeight);
			Widgets.BeginScrollView(scrollViewVisible, ref scrollPosition, scrollViewTotal);
			var curY = 0f;
			DrawModEntryHeader(scrollViewTotal.width, ref curY);
			for (int j = 0; j < handles.Count; j++) {
				var handle = handles[j];
				if (handle.VisibilityPredicate != null) {
					try {
						if (!handle.VisibilityPredicate()) continue;
					} catch (Exception e) {
						HugsLibController.Logger.ReportException(e, currentPackName, true,
							"SettingsHandle.VisibilityPredicate");
					}
				}
				DrawHandleEntry(handle, scrollViewTotal, ref curY, scrollViewVisible.height);
			}
			Widgets.EndScrollView();
			totalContentHeight = curY;
			GUI.EndGroup();
			Text.Font = GameFont.Small;
			var resetButtonRect =
				new Rect(0, inRect.height - windowButtonSize.y, windowButtonSize.x, windowButtonSize.y);
			if (Widgets.ButtonText(resetButtonRect, "HugsLib_settings_resetAll".Translate())) {
				ShowResetPrompt("HugsLib_settings_resetAll_prompt".Translate(),
					HugsLibController.SettingsManager.ModSettingsPacks.SelectMany(p => p.Handles));
			}
			var closeButtonRect = new Rect(inRect.width - windowButtonSize.x, inRect.height - windowButtonSize.y,
				windowButtonSize.x, windowButtonSize.y);
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
		private void DrawModEntryHeader(float width, ref float curY) {
			var entryTitleRect = new Rect(0f, curY, width, ModEntryLabelHeight);
			var mouseOverTitle = Mouse.IsOver(entryTitleRect);
			if (mouseOverTitle) {
				Widgets.DrawHighlight(entryTitleRect);
			}
			var labelRect = entryTitleRect.ContractedBy(ModEntryLabelPadding);
			Text.Font = GameFont.Medium;
			Widgets.Label(labelRect, currentPackName);
			Text.Font = GameFont.Small;

			var entryButtonsTopRight = new Vector2(width, curY);
			DrawFloatMenuButton(new Vector2(entryButtonsTopRight.x, entryButtonsTopRight.y));

			curY += ModEntryLabelHeight;
			var color = GUI.color;
			GUI.color = ModEntryLineColor;
			Widgets.DrawLineHorizontal(0f, curY, width);
			GUI.color = color;
			curY += ModEntryLabelPadding;

			void DrawFloatMenuButton(Vector2 topRight) {
				var buttonTopRight = new Vector2(topRight.x - ModEntryLabelPadding,
					topRight.y + (ModEntryLabelHeight - ModSettingsWidgets.HoverMenuHeight) / 2f);
				var hasExtraMenuEntries = currentPack.ContextMenuEntries != null;
				var hasContextMenuEntries = currentPack.CanBeReset || currentPack.ContextMenuEntries != null;
				if (ModSettingsWidgets.DrawHoverMenuButton(
						buttonTopRight, hasContextMenuEntries, hasExtraMenuEntries)) {
					OpenModEntryContextMenu();
				}

				void OpenModEntryContextMenu() {
					var resetOptionLabel =
						currentPack.CanBeReset ? "HugsLib_settings_resetMod".Translate(currentPackName) : null;
					ModSettingsWidgets.OpenExtensibleContextMenu(resetOptionLabel,
						OnResetOptionSelected, delegate { }, currentPack.ContextMenuEntries);
				}

				void OnResetOptionSelected() {
					ShowResetPrompt("HugsLib_settings_resetMod_prompt".Translate(currentPackName), currentPack.Handles);
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
						HugsLibController.Logger.ReportException(e, currentPackName, true,
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
			var cachedTitle = controlInfo.HandleTitle ?? (controlInfo.HandleTitle = new CachedLabel(handle.Title));
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
					HugsLibController.Logger.ReportException(e, currentPackName, true,
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
					() => ResetSettingHandles(handle), delegate { }, handle.ContextMenuEntries);
			}
		}

		// draws the input control for string settings
		private bool DrawHandleInputText(SettingHandle handle, Rect controlRect, HandleControlInfo info) {
			var evt = Event.current;
			GUI.SetNextControlName(info.ControlName);
			info.InputValue = Widgets.TextField(controlRect, info.InputValue);
			var focused = GUI.GetNameOfFocusedControl() == info.ControlName;
			if (focused) {
				info.ValidationScheduled = true;
				if (evt.type == EventType.KeyUp
					&& (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)) {
					focused = false;
				}
			}
			var changed = false;
			if (info.ValidationScheduled && !focused) {
				try {
					if (handle.Validator != null && !handle.Validator(info.InputValue)) {
						info.BadInput = true;
					} else {
						info.BadInput = false;
						handle.StringValue = info.InputValue;
						changed = true;
					}
				} catch (Exception e) {
					HugsLibController.Logger.ReportException(e, currentPackName, false, "SettingsHandle.Validator");
				}
				info.ValidationScheduled = false;
			}
			if (info.BadInput) {
				DrawBadTextValueOutline(controlRect);
			}
			return changed;
		}

		// draws the input control for integer settings
		private bool DrawHandleInputSpinner(SettingHandle handle, Rect controlRect, HandleControlInfo info) {
			var buttonSize = controlRect.height;
			var leftButtonRect = new Rect(controlRect.x, controlRect.y, buttonSize, buttonSize);
			var rightButtonRect = new Rect(controlRect.x + controlRect.width - buttonSize, controlRect.y, buttonSize,
				buttonSize);
			var changed = false;
			if (Widgets.ButtonText(leftButtonRect, "-")) {
				if (int.TryParse(info.InputValue, out var parsed)) {
					info.InputValue = (parsed - handle.SpinnerIncrement).ToString();
				}
				info.ValidationScheduled = true;
				changed = true;
			}
			if (Widgets.ButtonText(rightButtonRect, "+")) {
				if (int.TryParse(info.InputValue, out var parsed)) {
					info.InputValue = (parsed + handle.SpinnerIncrement).ToString();
				}
				info.ValidationScheduled = true;
				changed = true;
			}
			var textRect = new Rect(controlRect.x + buttonSize + 1, controlRect.y,
				controlRect.width - buttonSize * 2 - 2f, controlRect.height);
			if (DrawHandleInputText(handle, textRect, info)) {
				changed = true;
			}
			return changed;
		}

		// draws the input control for boolean settings
		private bool DrawHandleInputCheckbox(SettingHandle handle, Rect controlRect, HandleControlInfo info) {
			const float defaultCheckboxHeight = 24f;
			var checkOn = bool.Parse(info.InputValue);
			Widgets.Checkbox(controlRect.x, controlRect.y + (controlRect.height - defaultCheckboxHeight) / 2,
				ref checkOn);
			if (checkOn != bool.Parse(info.InputValue)) {
				handle.StringValue = info.InputValue = checkOn.ToString();
				return true;
			}
			return false;
		}

		// draws the input control for Enum settings
		private bool DrawHandleInputEnum(SettingHandle handle, Rect controlRect, HandleControlInfo info) {
			if (info.EnumNames == null) return false;
			var readableValue = (handle.EnumStringPrefix + info.InputValue).Translate();
			if (Widgets.ButtonText(controlRect, readableValue)) {
				var floatOptions = new List<FloatMenuOption>();
				foreach (var valueName in info.EnumNames) {
					var name = valueName;
					var readableOption = (handle.EnumStringPrefix + name).Translate();
					floatOptions.Add(new FloatMenuOption(readableOption, () => {
						handle.StringValue = info.InputValue = name;
						info.ValidationScheduled = true;
					}));
				}
				ModSettingsWidgets.OpenFloatMenu(floatOptions);
			}
			if (info.ValidationScheduled) {
				info.ValidationScheduled = false;
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

		private void TryRestoreWindowState() {
			var state = ModSettingsWindowState.Instance;
			if (state == null) return;
			scrollPosition = state.LastSettingsPackId == currentPack.ModId
				? new Vector2(0f, state.VerticalScrollPosition)
				: Vector2.zero;
		}

		private void TrySaveWindowState() {
			var state = ModSettingsWindowState.Instance;
			if (state == null) return;
			state.VerticalScrollPosition = scrollPosition.y;
			state.LastSettingsPackId = currentPack.ModId;
		}

		private void ShowResetPrompt(string message, IEnumerable<SettingHandle> resetHandles) {
			var resetHandlesArr = resetHandles.ToArray();
			var hiddenHandlesWithOwners = GetHiddenResettableHandles(resetHandlesArr)
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
				ResetSettingHandles(resetHandlesArr.ToArray());
			}
		}

		private void ResetSettingHandles(params SettingHandle[] resetHandles) {
			var resetCount = 0;
			foreach (var handle in resetHandles) {
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

		// updated the settings handles for current mod
		private void RefreshSettingsHandles() {
			handles.Clear();
			handles.AddRange(currentPack.Handles
				.Where(h => !h.NeverVisible)
				.OrderBy(h => h.DisplayOrder)
			);
			foreach (var handle in handles) {
				handle.ValueChanged -= OnHandleValueChanged;
				handle.ValueChanged += OnHandleValueChanged;
			}
		}

		private void OnHandleValueChanged(SettingHandle handle) {
			ResetHandleControlInfo(handle);
		}

		// prepares support objects to store data for settings handle controls
		private void PopulateControlInfo() {
			handleControlInfo.Clear();
			foreach (var handle in handles) {
				handleControlInfo.Add(handle, new HandleControlInfo(handle));
			}
		}

		// support data for each settings handle to allow gui controls to properly display and validate
		private class HandleControlInfo {
			public readonly string ControlName;
			public readonly List<string> EnumNames;
			public bool BadInput;
			public string InputValue;
			public bool ValidationScheduled;
			public CachedLabel HandleTitle;

			public HandleControlInfo(SettingHandle handle) {
				ControlName = "control" + handle.GetHashCode();
				InputValue = handle.StringValue;
				EnumNames = TryGetEnumNames(handle);
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