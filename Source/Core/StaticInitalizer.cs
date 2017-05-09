using Verse;

namespace HugsLib.Core {
	/// <summary>
	/// Provides an entry point for late controller setup during static constructor initialization.
	/// </summary>
	[StaticConstructorOnStartup]
	internal static class StaticInitalizer {
		static StaticInitalizer() {
			HugsLibController.Instance.LateInitalize();
		}
	}
}