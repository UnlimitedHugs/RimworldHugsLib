using Verse;

namespace HugsLib.Core {
	/// <summary>
	/// Entry point for the library.
	/// Instantiated by the game at the start of DoPlayLoad().
	/// </summary>
	public class HugsLibMod : Mod {
		public HugsLibMod(ModContentPack content) : base(content) {
			HugsLibController.EarlyInitialize(content);
		}
	}
}