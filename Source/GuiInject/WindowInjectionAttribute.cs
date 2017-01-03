using System;
using HugsLib.Source.Attrib;
using Verse;

namespace HugsLib.GuiInject {
	/**
	 * Add this to a static method to have it called each time the window type specified as the argument is drawn.
	 * This allows to inject code into any window in the game.
	 * Specify the Mode argument to select when (relative to the original window contents) the method will be called.
	 * See below for the required method signature.
	 */
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	internal sealed class WindowInjectionAttribute : Attribute, IDetectableAttribute {
		public const string ExpectedSignature = "void(Window drawnWindow, Rect contentRect)";

		public Type WindowType { get; private set; }

		public WindowInjectionManager.InjectMode Mode { get; set; }

		public WindowInjectionAttribute(Type windowType) {
			WindowType = windowType;
			if (windowType == null || !typeof (Window).IsAssignableFrom(windowType)) {
				throw new Exception("windowType must be a descendant of Verse.Window");
			}
		}
	}
}