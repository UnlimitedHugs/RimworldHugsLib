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
			giveShortHashMethod = typeof(ShortHashGiver).GetMethod("GiveShortHash", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(Def) }, null);
			if (giveShortHashMethod == null) {
				HugsLibController.Logger.Error("Failed to reflect ShortHashGiver.GiveShortHash");
			}
		}

		public static void GiveShortHasToDef(Def newDef) {
			if(giveShortHashMethod == null) throw new Exception("Hasher not initalized");
			giveShortHashMethod.Invoke(null, new object[] { newDef });
		}
	}
}