using System;
using System.Diagnostics;
using System.Text;
using Verse;

namespace HugsLib {
	/**
	 * A logger that prefixes all mesages with the identifier of the issuing mod
	 * The Trace methods will only output when Dev mode is active
	 */
	public class ModLogger {
		private readonly StringBuilder builder;
		private readonly string logPrefix;
		private string lastExceptionLocation;

		public ModLogger(string logPrefix) {
			this.logPrefix = logPrefix;
			builder = new StringBuilder();
		}

		public void Message(string message, params object[] substitiutions) {
			Log.Message(FormatOutput(message, substitiutions));
		}

		public void Warning(string message, params object[] substitiutions) {
			Log.Warning(FormatOutput(message, substitiutions));
		}

		public void Error(string message, params object[] substitiutions) {
			Log.Error(FormatOutput(message, substitiutions));
		}

		public void Trace(params object[] strings) {
			if (!Prefs.DevMode) return;
			if (strings.Length == 1) {
				var msg = strings[0];
				Log.Message(FormatOutput(msg != null ? msg.ToString() : "null"));
			} else {
				Log.Message(strings.ListElements());
			}
		}

		public void TraceFormat(string message, params object[] substitiutions) {
			if (!Prefs.DevMode) return;
			Log.Message(FormatOutput(message, substitiutions));
		}

		public void ReportException(Exception e, string modIdentifier = null, bool reportOnceOnly = false, string location = null) {
			if (location == null) {
				location = new StackFrame(1, true).GetMethod().Name;
			}
			if(reportOnceOnly && lastExceptionLocation == location) return;
			lastExceptionLocation = location;
			string message;
			if (modIdentifier != null) {
				message = FormatOutput("{0} caused an exception during {1}: {2}", modIdentifier, location, e);
			} else {
				message = FormatOutput("Exception during {0}: {1}", location, e);
			}
			Log.Error(message);
		}

		private string FormatOutput(string message, params object[] substitiutions) {
			builder.Length = 0;
			builder.Append("[");
			builder.Append(logPrefix);
			builder.Append("] ");
			if (substitiutions.Length>0) {
				builder.AppendFormat(message, substitiutions);
			} else {
				builder.Append(message);
			}
			return builder.ToString();
		}
	}
}