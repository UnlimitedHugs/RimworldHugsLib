using UnityEngine;
using Verse;

namespace HugsLib {
	/**
	 * Replaces windows of a given type with an instance of another type.
	 * A simple alternative to detouring for windows that are not hooked up to other components.
	 */
	public class WindowReplacer<T, U> where T: Window where U: Window, new() {
		private readonly Window replacement;

		public WindowReplacer() {
			replacement = new U();
		}
		
		public void OnGUI() {
			if(Event.current.type != EventType.Repaint || Current.Root == null || Current.Root.uiRoot == null) return;
			var stack = Current.Root.uiRoot.windows;
			if(stack == null || stack.Count == 0) return;
			var window = GetTargetWindow(stack);
			if (window == null) return;
			stack.TryRemove(window);
			stack.Add(replacement);
		}

		private Window GetTargetWindow(WindowStack stack) {
			for (int i = stack.Count - 1; i >= 0; i--) {
				if (stack[i] is T && !(stack[i] is U)) return stack[i];
			}
			return null;
		}
	}
}