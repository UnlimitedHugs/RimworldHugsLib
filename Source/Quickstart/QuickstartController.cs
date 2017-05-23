using UnityEngine;
using Verse;

namespace HugsLib.Quickstart {
	[StaticConstructorOnStartup]
	public static class QuickstartController {
		private static readonly Texture2D quickstartIconTex = ContentFinder<Texture2D>.Get("quickstartIcon");

		internal static void DrawDebugToolbarButton(WidgetRow widgets) {
			if (widgets.ButtonIcon(quickstartIconTex, "Open the quickstart settings.\n\nThis lets you automatically generate a map or load an existing save when the game is started.")) {
				Find.WindowStack.Add(new Dialog_QuickstartSettings());
			}
		}
	}
}