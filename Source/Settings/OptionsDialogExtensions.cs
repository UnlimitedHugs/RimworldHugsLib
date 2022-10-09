using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace HugsLib.Settings;

internal static class OptionsDialogExtensions {
	private static FieldInfo cachedModsField;

	public static void InjectHugsLibModEntries(Dialog_Options dialog) {
		var stockEntries = (IEnumerable<Mod>)cachedModsField.GetValue(dialog);
		var hugsLibEntries = HugsLibController.Instance.Settings.ModSettingsPacks
			.Where(p => p.Handles.Any(h => !h.NeverVisible))
			.Select(p => {
				var label = p.EntryName.NullOrEmpty()
					? "HugsLib_setting_unnamed_mod".Translate().ToString()
					: p.EntryName;
				return new SettingsProxyMod(label, p.ModId);
			});
		var combinedEntries = stockEntries
			.Concat(hugsLibEntries)
			.OrderBy(m => m.SettingsCategory());

		cachedModsField.SetValue(dialog, combinedEntries);
	}

	public static Window GetModSettingsWindow(Mod forMod) {
		return forMod is SettingsProxyMod proxy
			? new Dialog_ModSettings {
				WindowState = new ModSettingsWindowState {
					ExpandedSettingPackIds = new[] { proxy.SettingPackId },
				}
			}
			: new RimWorld.Dialog_ModSettings(forMod);
	}

	public static void PrepareReflection() {
		const string fieldName = "cachedModsWithSettings";
		cachedModsField =
			typeof(Dialog_Options).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
		if (cachedModsField == null || cachedModsField.FieldType != typeof(IEnumerable<Mod>))
			HugsLibController.Logger.Error($"Failed to reflect {nameof(Dialog_Options)}.{fieldName}");
	}
}

internal class SettingsProxyMod : Mod {
	public string SettingPackId { get; }
	private readonly string entryLabel;

	[UsedImplicitly]
	public SettingsProxyMod(ModContentPack content) : base(content) {
	}

	public SettingsProxyMod(string entryLabel, string settingPackId) : base(null) {
		this.entryLabel = entryLabel;
		SettingPackId = settingPackId;
	}

	public override string SettingsCategory() {
		return entryLabel;
	}
}