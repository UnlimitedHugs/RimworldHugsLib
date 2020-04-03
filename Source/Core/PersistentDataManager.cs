using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Verse;

namespace HugsLib.Core {
	/// <summary>
	/// A base for managers that save data in xml format, to be stored in the save data folder
	/// </summary>
	public abstract class PersistentDataManager {

		public static bool IsValidElementName(string tagName) {
			try {
				XmlConvert.VerifyName(tagName);
				return true;
			} catch {
				return false;
			}
		}
		
		protected abstract string FileName { get; }

		protected abstract void LoadFromXml(XDocument xml);

		protected abstract void WriteXml(XDocument xml);

		protected virtual string FolderName {
			get { return "HugsLib"; }
		}

		protected virtual bool SuppressLoadSaveExceptions {
			get { return true; }
		}
		
		protected virtual bool DisplayLoadSaveWarnings {
			get { return true; }
		}

		protected void LoadData() {
			var filePath = GetSettingsFilePath(FileName);
			if (!File.Exists(filePath)) return;
			try {
				var doc = XDocument.Load(filePath);
				LoadFromXml(doc);
			} catch (Exception ex) {
				if (DisplayLoadSaveWarnings) {
					HugsLibController.Logger.Warning("Exception loading xml from " + filePath + ". " +
													"Loading defaults instead. Exception was: " + ex
					);
				}
				if (!SuppressLoadSaveExceptions) throw;
			}
		}

		protected void SaveData() {
			var filePath = GetSettingsFilePath(FileName);
			try {
				var doc = new XDocument();
				WriteXml(doc);
				doc.Save(filePath);
			} catch (Exception ex) {
				if (DisplayLoadSaveWarnings) {
					HugsLibController.Logger.Warning("Failed to save xml to " + filePath + ". Exception was: " + ex);
				}
				if (!SuppressLoadSaveExceptions) throw;
			}
		}

		private string GetSettingsFilePath(string fileName) {
			string path = Path.Combine(GenFilePaths.SaveDataFolderPath, FolderName);
			var directoryInfo = new DirectoryInfo(path);
			if (!directoryInfo.Exists) {
				directoryInfo.Create();
			}
			return Path.Combine(path, fileName);
		}
	}
}