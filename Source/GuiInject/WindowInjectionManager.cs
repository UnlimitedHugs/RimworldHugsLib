using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace HugsLib.GuiInject {
	public static class WindowInjectionManager {
		public delegate void DrawInjectedContents(Window window, Rect inRect);

		public enum InjectMode {
			AfterContents,
			BeforeContents,
			ReplaceContents
		}

		// If a callback causes an exception it is removed for the sake of performance. You can set this to false in Dev mode for debugging.
		public static bool RemoveErroringCallbacks = true;

		private static readonly Dictionary<string, WindowInjection> windowInjections = new Dictionary<string, WindowInjection>();
		private static readonly Dictionary<Type, WindowInjectionSet> injectionSets = new Dictionary<Type, WindowInjectionSet>(); 

		public static bool AddInjection(Type windowType, DrawInjectedContents callback, string injectionId, InjectMode mode = InjectMode.AfterContents, bool errorOnFail = true) {
			if (windowType == null || !typeof(Window).IsAssignableFrom(windowType)) {
				if(errorOnFail) throw new Exception("windowType must be a descendant of Verse.Window");
				return false;
			}
			if (callback == null) {
				if(errorOnFail) throw new Exception("callback cannot be null");
				return false;
			}
			if (injectionId.NullOrEmpty()) {
				if(errorOnFail) throw new Exception("injectionId must be a valid string");
				return false;
			}
			if (windowInjections.ContainsKey(injectionId)) {
				if(errorOnFail) throw new Exception(string.Format("Cannot add window injection with id {0}, injection with his id already exists", injectionId));
				return false;
			}
			var injection = new WindowInjection(injectionId, windowType, mode, callback);
			windowInjections.Add(injectionId, injection);
			ActivateInjection(injection);
			return true;
		}

		public static void RemoveInjection(string injectionId) {
			WindowInjection injection;
			windowInjections.TryGetValue(injectionId, out injection);
			if(injection == null) throw new Exception(string.Format("Could not remove injection with id {0} as it does not exist", injectionId));
			windowInjections.Remove(injectionId);
			DeactivateInjection(injection);
		}

		public static bool InjectionExists(string injectionId) {
			return windowInjections.ContainsKey(injectionId);	
		}

		// Returns all active injections. This is mostly for debugging purposes.
		public static IEnumerable<WindowInjection> AllActiveInjections {
			get { return windowInjections.Values; }
		}

		// returns the set for the provided window type, or null if no injections are defined for it
		internal static WindowInjectionSet GetSetForWindowType(Type type) {
			WindowInjectionSet set;
			injectionSets.TryGetValue(type, out set);
			return set;
		}

		// adds the injection to one of the sets, allowing it to be called
		private static void ActivateInjection(WindowInjection injection) {
			WindowInjectionSet set;
			injectionSets.TryGetValue(injection.windowType, out set);
			if (set == null) {
				set = new WindowInjectionSet();
				injectionSets.Add(injection.windowType, set);
			}
			switch (injection.mode) {
				case InjectMode.BeforeContents:
					if(set.beforeContents == null) set.beforeContents = new List<DrawInjectedContents>();
					set.beforeContents.Add(injection.callback);
					break;
				case InjectMode.AfterContents:
					if(set.afterContents == null) set.afterContents = new List<DrawInjectedContents>();
					set.afterContents.Add(injection.callback);
					break;
				case InjectMode.ReplaceContents:
					if(set.replaceContents == null) set.replaceContents = new List<DrawInjectedContents>();
					set.replaceContents.Add(injection.callback);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		// removes the injection from its set, which will prevent it from being called
		private static void DeactivateInjection(WindowInjection injection) {
			WindowInjectionSet set;
			injectionSets.TryGetValue(injection.windowType, out set);
			if(set == null) return;
			if (set.beforeContents != null) {
				set.beforeContents.Remove(injection.callback);
				if (set.beforeContents.Count == 0) set.beforeContents = null;
			}
			if (set.afterContents != null) {
				set.afterContents.Remove(injection.callback);
				if (set.afterContents.Count == 0) set.afterContents = null;
			}
			if (set.replaceContents != null) {
				set.replaceContents.Remove(injection.callback);
				if (set.replaceContents.Count == 0) set.replaceContents = null;
			}
			if (set.beforeContents == null && set.afterContents == null && set.replaceContents == null) {
				injectionSets.Remove(injection.windowType);
			}
		}

		public class WindowInjectionSet {
			public List<DrawInjectedContents> beforeContents;
			public List<DrawInjectedContents> afterContents;
			public List<DrawInjectedContents> replaceContents;
		}
	}
}