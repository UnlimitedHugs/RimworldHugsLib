using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace HugsLib.Utils {
	/// <summary>
	/// Adds a hash to a manually instantiated def to avoid def collisions.
	/// </summary>
	public static class InjectedDefHasher {
		private delegate void GiveShortHashTakenHashes(Def def, Type defType, HashSet<ushort> takenHashes);
		private delegate void GiveShortHash(Def def, Type defType);
		private static GiveShortHash giveShortHashDelegate;

		internal static void PrepareReflection() {
			try {
				var takenHashesField = typeof(ShortHashGiver).GetField(
					"takenHashesPerDeftype", BindingFlags.Static | BindingFlags.NonPublic);
				var takenHashesDictionary = takenHashesField?.GetValue(null) as Dictionary<Type, HashSet<ushort>>;
				if (takenHashesDictionary == null) throw new Exception("taken hashes");

				var methodInfo = typeof(ShortHashGiver).GetMethod(
					"GiveShortHash", BindingFlags.NonPublic | BindingFlags.Static,
					null, new[] { typeof(Def), typeof(Type), typeof(HashSet<ushort>) }, null);
				if (methodInfo == null) throw new Exception("hashing method");

				var hashDelegate = (GiveShortHashTakenHashes)Delegate.CreateDelegate(
					typeof(GiveShortHashTakenHashes), methodInfo);
				giveShortHashDelegate = (def, defType) => {
					var takenHashes = takenHashesDictionary.TryGetValue(defType);
					if (takenHashes == null) {
						takenHashes = new HashSet<ushort>();
						takenHashesDictionary.Add(defType, takenHashes);
					}
					hashDelegate(def, defType, takenHashes);
				};
			} catch (Exception e) {
				HugsLibController.Logger.Error($"Failed to reflect short hash dependencies: {e.Message}");
			}
		}

		/// <summary>
		/// Give a short hash to a def created at runtime.
		/// Short hashes are used for proper saving of defs in compressed maps within a save file.
		/// </summary>
		/// <param name="newDef"></param>
		/// <param name="defType">The type of defs your def will be saved with. For example,
		/// use typeof(ThingDef) if your def extends ThingDef.</param>
		public static void GiveShortHashToDef(Def newDef, Type defType) {
			if (giveShortHashDelegate == null) throw new Exception("Hasher not initialized");
			giveShortHashDelegate(newDef, defType);
		}
	}
}