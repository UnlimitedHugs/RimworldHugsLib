using System;

namespace HugsLib.Settings {
	/// <summary>
	/// Used to preserve the state of the Mod Settings window between multiple openings.
	/// State is not persisted between game restarts.
	/// </summary>
	internal class ModSettingsWindowState : IModSettingsWindowState {
		private static ModSettingsWindowState instance;
		public static ModSettingsWindowState Instance {
			get { return instance ?? (instance = new ModSettingsWindowState()); }
		}

		public string[] ExpandedSettingPackIds { get; set; } = Array.Empty<string>();
		public float VerticalScrollPosition { get; set; }
	}

	internal interface IModSettingsWindowState {
		string[] ExpandedSettingPackIds { get; set; }
		float VerticalScrollPosition { get; set; }
	}
}