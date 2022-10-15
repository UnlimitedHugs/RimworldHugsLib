using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace HugsLib.Utils {
	/// <summary>
	/// Adds a hash to a manually instantiated def to avoid def collisions.
	/// </summary>
	public static class InjectedDefHasher {
		private delegate void GiveShortHash(Def def, Type defType, HashSet<ushort> takenHashes);

		private static FieldInfo takenHashesPerDefTypeField = null!;
		private static GiveShortHash giveShortHashDelegate = null!;

		internal static void PrepareReflection() {
			var methodInfo = typeof(ShortHashGiver).GetMethod("GiveShortHash", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(Def), typeof(Type), typeof(HashSet<ushort>) }, null);
			if (methodInfo == null) {
				HugsLibController.Logger.Error("Failed to reflect ShortHashGiver.GiveShortHash");
				return;
			}
			giveShortHashDelegate = (GiveShortHash)Delegate.CreateDelegate(typeof(GiveShortHash), methodInfo);

			var fieldInfo =
				typeof(ShortHashGiver).GetField("takenHashesPerDeftype", BindingFlags.NonPublic | BindingFlags.Static);
			if (fieldInfo == null)
			{
				HugsLibController.Logger.Error("Failed to reflect ShortHashGiver.takenHashesPerDeftype");
				return;
			}
			
			takenHashesPerDefTypeField = fieldInfo;
		}

		/// <summary>
		/// Give a short hash to a def created at runtime.
		/// Short hashes are used for proper saving of defs in compressed maps within a save file.
		/// </summary>
		/// <param name="newDef"></param>
		/// <param name="defType">The type of defs your def will be saved with. For example, use typeof(ThingDef) if your def extends ThingDef.</param>
		public static void GiveShortHashToDef(Def newDef, Type defType) {
			if (giveShortHashDelegate == null || takenHashesPerDefTypeField == null) throw new Exception("Hasher not initialized");

			var takenHashes = GetTakenHashes(defType);
			giveShortHashDelegate(newDef, defType, takenHashes);
		}

		private static HashSet<ushort> GetTakenHashes(Type defType)
		{
			var dict = (Dictionary<Type, HashSet<ushort>>)takenHashesPerDefTypeField.GetValue(null);

			if (dict.TryGetValue(defType, out var takenHashes))
			{
				return takenHashes;
			}

			return dict[defType] = new();
		}
	}
}