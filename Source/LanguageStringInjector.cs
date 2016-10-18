using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Verse;

namespace HugsLib {
	/**
	 * Injects one of the dll-embedded language xml files according to the current game language
	 */
	internal class LanguageStringInjector {
		private const string ValidResourceNamePrefix = "HugsLib.Languages.";
		private const string DetectionStringId = "HugsLibDetectionString";

		private Dictionary<string, string> availableLanguages;

		public void InjectEmbeddedStrings() {
			if(DetectionStringId.CanTranslate()) return; // a parallel running version already injected
			if (availableLanguages == null) {
				availableLanguages = LoadEmbeddedData();
			}
			if(TryInjectLanguageXml(LanguageDatabase.activeLanguage, LanguageDatabase.activeLanguage.folderName)) return;
			// inject english strings into the current language as fallback- no point showing corrupted strings
			TryInjectLanguageXml(LanguageDatabase.activeLanguage, LanguageDatabase.defaultLanguage.folderName);
		}

		private Dictionary<string, string> LoadEmbeddedData() {
			var assembly = typeof(LanguageStringInjector).Assembly;
			var languageData = new Dictionary<string, string>();
			try {
				foreach (var resourceName in assembly.GetManifestResourceNames()) {
					if (!resourceName.StartsWith(ValidResourceNamePrefix)) continue;
					var languageName = resourceName.Substring(ValidResourceNamePrefix.Length).Split('.')[0];
					var stream = assembly.GetManifestResourceStream(resourceName);
					if (stream == null) continue;
					var contents = new StreamReader(stream).ReadToEnd();
					languageData.Add(languageName, contents);
				}
			} catch (Exception e) {
				HugsLibController.Logger.Warning("Failed to retrieve embedded language files. Exception was: "+e);
			}
			return languageData;
		}

		private bool TryInjectLanguageXml(LoadedLanguage targetLang, string languageId) {
			string xmlString;
			availableLanguages.TryGetValue(languageId, out xmlString);
			if (xmlString == null) return false;

			var valuesToAdd = new Dictionary<string, string>();
			try {
				valuesToAdd.Add(DetectionStringId, "");
				foreach (var pair in EnumerateXmlStrings(xmlString)) {
					if (targetLang.keyedReplacements.ContainsKey(pair.Key) || valuesToAdd.ContainsKey(pair.Key)) {
						HugsLibController.Logger.Warning("Duplicate code-linked translation key: {0} in language {1}", pair.Key, languageId);
					} else {
						valuesToAdd.Add(pair.Key, pair.Value);
					}
				}
			} catch (Exception e) {
				HugsLibController.Logger.Warning("Failed to inject {0} strings. Exception was: {1}", languageId, e);
				valuesToAdd.Clear();
			}
			foreach (var pair in valuesToAdd) {
				targetLang.keyedReplacements.Add(pair.Key, pair.Value);
			}
			return true;
		}

		private IEnumerable<KeyValuePair<string, string>> EnumerateXmlStrings(string xmlData) {
			var doc = XDocument.Parse(xmlData);
			if (doc.Root == null) throw new FormatException("Missing root element");
			foreach (var element in doc.Root.Elements()) {
				string key = element.Name.ToString();
				string text = element.Value;
				text = text.Replace("\\n", "\n");
				yield return new KeyValuePair<string, string>(key, text);
			}
		}
	}
}