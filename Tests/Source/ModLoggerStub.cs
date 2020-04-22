using System;
using System.Collections.Generic;
using System.Linq;
using HugsLib.Utils;

namespace HugsLibTests {
	public class ModLoggerStub : IModLogger {
		private readonly List<string> messages = new List<string>();
		private readonly List<string> warnings = new List<string>();
		private readonly List<string> errors = new List<string>();
		private readonly List<string> exceptions = new List<string>();
		
		public void Message(string message, params object[] substitutions) {
			messages.Add(string.Format(message, substitutions));
		}

		public void Warning(string message, params object[] substitutions) {
			warnings.Add(string.Format(message, substitutions));
		}

		public void Error(string message, params object[] substitutions) {
			errors.Add(string.Format(message, substitutions));
		}

		public void ReportException(Exception e, string modIdentifier = null, bool reportOnceOnly = false, string location = null) {
			exceptions.Add(e?.ToString());
		}

		public void AssertNothingLogged() {
			AssertMessageType(messages, "messages");
			AssertMessageType(warnings, "warnings");
			AssertMessageType(errors, "errors");
			AssertMessageType(exceptions, "exceptions");
			
			void AssertMessageType(List<string> messageList, string messageName) {
				if (messageList.Count > 0) {
					throw new Exception($"Unexpected {messageList.Count} {messageName} logged: " +
						$"{messageList.Select(s => s ?? "[null]").Join(",\n")}"
					);
				}
			}
		}
	}
}