using System;
using Verse;

namespace HugsLib.Utils {
	/// <summary>
	/// Utility methods for displaying debug messages during development.
	/// </summary>
	public static class Tracer {
		/// <summary>
		/// Writes comma-separated log messages if the game is in Dev mode.
		/// Any non-strings will be converted to strings, and null values will appear explicitly.
		/// </summary>
		/// <param name="messages">Messages to output</param>
		public static void Trace(params object[] messages) {
			if (!Prefs.DevMode) return;
			ExpandObjectsToTraceableStrings(messages);
			Log.Message(messages.ListElements());
		}

		/// <summary>
		/// Similar to Trace, but feeds the messages through String.Format first.
		/// </summary>
		/// <param name="format">The string to interpolate</param>
		/// <param name="formatArgs">Interpolation arguments</param>
		public static void TraceFormat(string format, params object[] formatArgs) {
			if (!Prefs.DevMode) return;
			ExpandObjectsToTraceableStrings(formatArgs);
			Trace(String.Format(format, formatArgs));
		}

		internal static void ExpandObjectsToTraceableStrings(object[] traceables) {
			for (int i = 0; i < traceables.Length; i++) {
				var obj = traceables[i];
				if (obj == null) {
					traceables[i] = "[null]";
				} else if (!(obj is string)) {
					traceables[i] = obj.ToString();
				}
			}
		} 
	}
}