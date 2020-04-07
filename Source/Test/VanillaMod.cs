#if TEST_MOD
using UnityEngine;
using Verse;

namespace HugsLib.Test {
	public class VanillaMod : Mod {
		public VanillaMod(ModContentPack content) : base(content) {
			HugsLibController.Logger.Message("vanilla mod 1 construct");
		}

		public override void DoSettingsWindowContents(Rect inRect) {
			Widgets.ButtonText(new Rect(inRect.x, inRect.y, 200f, 30f), "stuff1");
			Widgets.ButtonText(new Rect(inRect.x, inRect.y + 40f, 200f, 30f), "things1");
		}

		public override string SettingsCategory() {
			return "VanillaMod I";
		}
	}

	public class VanillaMod2 : Mod {
		public VanillaMod2(ModContentPack content)
			: base(content) {
				HugsLibController.Logger.Message("vanilla mod 2 construct");
		}

		public override void DoSettingsWindowContents(Rect inRect) {
			Widgets.ButtonText(new Rect(inRect.x + 210, inRect.y, 200f, 30f), "stuff2");
			Widgets.ButtonText(new Rect(inRect.x + 210, inRect.y + 40f, 200f, 30f), "things2");
			//throw new Exception("honk");
		}

		public override string SettingsCategory() {
			return "VanillaMod II";
		}
	}
}
#endif