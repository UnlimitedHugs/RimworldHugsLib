using System;
using System.Collections.Generic;
using System.Linq;
using HugsLib.Utils;

namespace HugsLibTests {
	/// <summary>
	/// A stand-in for <see cref="ModLogger"/> for use in unit testing.
	/// </summary>
	public class ModLoggerStub : IModLogger {
		private readonly List<string> messages = new List<string>();
		private readonly List<string> warnings = new List<string>();
		private readonly List<string> errors = new List<string>();
		private readonly List<string> exceptions = new List<string>();
		private readonly (List<string> list, string name)[] lists;

		public ModLoggerStub() {
			lists = new[] {
				(messages, "Messages"),
				(warnings, "Warning"),
				(errors, "Errors"),
				(exceptions, "Exceptions")
			};
		}

		public void Message(string message, params object[] substitutions) {
			messages.Add(string.Format(message, substitutions));
		}

		public void Warning(string message, params object[] substitutions) {
			warnings.Add(string.Format(message, substitutions));
		}

		public void Error(string message, params object[] substitutions) {
			errors.Add(string.Format(message, substitutions));
		}

		public void ReportException(Exception e, string modIdentifier = null, bool reportOnceOnly = false,
			string location = null) {
			exceptions.Add(e?.ToString());
		}

		public void AssertNothingLogged() {
			if (messages.Count + warnings.Count + errors.Count + exceptions.Count > 0) {
				throw new Exception("Expected nothing logged. Actual entries: " + this);
			}
		}

		public void AssertWarningsContain(params string[] containedStringInOrder) {
			AssertListEntriesContain(lists[1], containedStringInOrder);
		}

		public void Clear() {
			messages.Clear();
			warnings.Clear();
			errors.Clear();
			exceptions.Clear();
		}

		public override string ToString() {
			return $"[{nameof(ModLoggerStub)}{lists.Select(ListEntryType).Join("")}";

			string ListEntryType((List<string>, string) listType) {
				var (list, name) = listType;
				return list.Count == 0
					? string.Empty
					: $"\n{name}:" + list.Select(s => $"\n> {s}").Join("");
			}
		}

		private void AssertListEntriesContain((List<string>, string) listType, IEnumerable<string> expectedContents) {
			var (entriesList, listName) = listType;
			var workListExpected = expectedContents.ToList();
			var workListActual = entriesList.ToList();
			if (workListExpected.Count != workListActual.Count) {
				throw new Exception($"{listName} count mismatch: " +
					$"expected elements: {workListExpected.ListElements()}" + $" actual: {this}");
			}
			for (var i = 0; i < workListExpected.Count; i++) {
				if (!workListActual[i]?.Contains(workListExpected[i]) ?? false) {
					throw new Exception($"Expected {listName.ToLower()} to contain \"{workListExpected[i]}\" " +
						$"at index {i}. Actual: {this}");
				}
			}
		}
	}
}