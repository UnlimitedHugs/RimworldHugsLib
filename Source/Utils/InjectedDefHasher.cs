using System;
using System.Reflection;
using Verse;

namespace HugsLib.Utils {
	/// <summary>
	/// Adds a hash to a manually instantiated def to avoid def collisions.
	/// </summary>
	public static class InjectedDefHasher {
		private static MethodInfo giveShortHashMethod;

		internal static void PrepareReflection() {
			giveShortHashMethod = typeof(ShortHashGiver).GetMethod("GiveShortHash", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(Def), typeof(Type) }, null);
			if (giveShortHashMethod == null) {
				HugsLibController.Logger.Error("Failed to reflect ShortHashGiver.GiveShortHash");
			}
		}

		/// <summary>
		/// Give a short hash to a def created at runtime.
		/// Short hashes are used for proper saving of defs in compressed maps within a save file.
		/// </summary>
		/// <param name="newDef"></param>
		/// <param name="defType">The type of defs your def will be saved with. For example, use typeof(ThingDef) if your def extends ThingDef.</param>
		public static void GiveShortHasToDef(Def newDef, Type defType) {
			if(giveShortHashMethod == null) throw new Exception("Hasher not initalized");
			giveShortHashMethod.Invoke(null, new object[] { newDef, defType });
		}
	}
}