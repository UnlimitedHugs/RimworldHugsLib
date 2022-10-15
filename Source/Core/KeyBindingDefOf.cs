using RimWorld;
using Verse;
// ReSharper disable UnassignedField.Global

namespace HugsLib.Core {
	/// <summary>
	/// Holds references to key binding defs used by the library.
	/// </summary>
	[DefOf]
	public static class HugsLibKeyBindings {
		public static KeyBindingDef PublishLogs = null!;
		public static KeyBindingDef OpenLogFile = null!;
		public static KeyBindingDef RestartRimworld = null!;
		public static KeyBindingDef HLOpenModSettings = null!;
		public static KeyBindingDef HLOpenUpdateNews = null!;
	}
}