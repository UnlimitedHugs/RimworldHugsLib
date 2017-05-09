using System;
using System.Diagnostics;
using System.Text;
using Verse;

namespace HugsLib.Utils {
	//
	/// <summary>
	/// A logger that prefixes all mesages with the identifier of the issuing mod.
	/// </summary>
	public class ModLogger {
		private const string WarningPrefix = "warn";
		private const string ErrorPrefix = "ERR";

		private readonly StringBuilder builder;
		private readonly string logPrefix;
		private string lastExceptionLocation;

		public ModLogger(string logPrefix) {
			this.logPrefix = logPrefix;
			builder = new StringBuilder();
		}

		/// <summary>
		/// Writes a message to the log, prefixing it with the issuing mod identifier.
		/// </summary>
		/// <param name="message">The message to write</param>
		/// <param name="substitiutions">Optional substitution values for the message</param>
		public void Message(string message, params object[] substitiutions) {
			Log.Message(FormatOutput(message, null, substitiutions));
		}

		/// <summary>
		/// Same as Message(), but the console will display the message as a warning.
		/// </summary>
		public void Warning(string message, params object[] substitiutions) {
			Log.Warning(FormatOutput(message, WarningPrefix, substitiutions));
		}

		/// <summary>
		/// Same as Message(), but the console will display the message as an error.
		/// This will open the Log window in in Dev mode.
		/// </summary>
		public void Error(string message, params object[] substitiutions) {
			Log.Error(FormatOutput(message, ErrorPrefix, substitiutions));
		}

		/// <summary>
		/// Writes a message only if Dev mode is enabled.
		/// </summary>
		/// <param name="strings">The strings to display</param>
		public void Trace(params object[] strings) {
			if (!Prefs.DevMode) return;
			if (strings.Length == 1) {
				var msg = strings[0];
				Log.Message(FormatOutput(msg != null ? msg.ToString() : "null", null));
			} else {
				Log.Message(strings.ListElements());
			}
		}

		/// <summary>
		/// Same as Trace(), but formats the message and replaces substitution variables.
		/// </summary>
		public void TraceFormat(string message, params object[] substitiutions) {
			if (!Prefs.DevMode) return;
			Log.Message(FormatOutput(message, null, substitiutions));
		}

		/// <summary>
		/// Writes an error to the log to report an exception.
		/// The message will contain the name of the method that caused the exception if a location is not provided.
		/// </summary>
		/// <param name="e">The excepton that occurred</param>
		/// <param name="modIdentifier">Optional identifier of the mod that caused the exception</param>
		/// <param name="reportOnceOnly">True, if the exception should only be reported once for that specific location. Useful for errors that will trigger each frame or tick.</param>
		/// <param name="location">Optional name of the location where the exception occurred. Will display as "exception during (location)"</param>
		public void ReportException(Exception e, string modIdentifier = null, bool reportOnceOnly = false, string location = null) {
			if (location == null) {
				location = new StackFrame(1, true).GetMethod().Name;
			}
			if(reportOnceOnly && lastExceptionLocation == location) return;
			lastExceptionLocation = location;
			string message;
			if (modIdentifier != null) {
				message = FormatOutput("{0} caused an exception during {1}: {2}", ErrorPrefix, modIdentifier, location, e);
			} else {
				message = FormatOutput("Exception during {0}: {1}", ErrorPrefix, location, e);
			}
			Log.Error(message);
		}

		private string FormatOutput(string message, string extraPrefix, params object[] substitiutions) {
			builder.Length = 0;
			builder.AppendFormat("[{0}]", logPrefix);
			if (extraPrefix!=null) {
				builder.AppendFormat("[{0}]", extraPrefix);
			}
			builder.Append(" ");
			if (substitiutions.Length>0) {
				builder.AppendFormat(message, substitiutions);
			} else {
				builder.Append(message);
			}
			return builder.ToString();
		}
	}
}