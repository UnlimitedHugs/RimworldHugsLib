using System;
using System.Reflection;
using Verse;

namespace HugsLib.Utils {
	/// <summary>
	/// Adds a hash to a manually instantiated def to avoid def collisions.
	/// </summary>
	public static class InjectedDefHasher {
		private delegate void GiveShortHash(Def def, Type defType);
		private static GiveShortHash giveShortHashDelegate;

		internal static void PrepareReflection() {
			var methodInfo = typeof(ShortHashGiver).GetMethod("GiveShortHash", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(Def), typeof(Type) }, null);
			if (methodInfo == null) {
				HugsLibController.Logger.Error("Failed to reflect ShortHashGiver.GiveShortHash");
				return;
			}
			giveShortHashDelegate = (GiveShortHash)Delegate.CreateDelegate(typeof(GiveShortHash), methodInfo);
		}

		/// <summary>
		/// Give a short hash to a def created at runtime.
		/// Short hashes are used for proper saving of defs in compressed maps within a save file.
		/// </summary>
		/// <param name="newDef"></param>
		/// <param name="defType">The type of defs your def will be saved with. For example, use typeof(ThingDef) if your def extends ThingDef.</param>
		public static void GiveShortHashToDef(Def newDef, Type defType) {
			if (giveShortHashDelegate == null) throw new Exception("Hasher not initialized");
			giveShortHashDelegate(newDef, defType);
		}
	}
}