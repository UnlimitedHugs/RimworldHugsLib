using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace HugsLib.Core {
	/**
	 * Replaces windows of a given type with an instance of another type.
	 * A simple alternative to detouring for windows that are not hooked up to other components.
	 */
	public class WindowReplacer {
		private readonly List<KeyValuePair<Type, Type>> watchList = new List<KeyValuePair<Type, Type>>();

		public void RegisterReplacement<T, U>() where T : Window where U : Window, new() {
			watchList.Add(new KeyValuePair<Type, Type>(typeof(T), typeof(U)));
		}

		public void OnGUI() {
			if(Event.current.type != EventType.Repaint || Current.Root == null || Current.Root.uiRoot == null) return;
			var stack = Current.Root.uiRoot.windows;
			if(stack == null || stack.Count == 0) return;
			for (int i = stack.Count - 1; i >= 0; i--) {
				var window = stack[i];
				var windowType = window.GetType(); 
				for (int j = 0; j < watchList.Count; j++) {
					if (windowType == watchList[j].Key && windowType != watchList[j].Value) {
						ReplaceWindow(window, watchList[j].Value);
					}
				}
			}
		}

		private void ReplaceWindow(Window original, Type replacementType) {
			Find.WindowStack.TryRemove(original);
			var replacement = (Window)Activator.CreateInstance(replacementType);
			Find.WindowStack.Add(replacement);
		}
	}
}