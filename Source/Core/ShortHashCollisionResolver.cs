using System;
using System.Collections;
using System.Collections.Generic;
using HugsLib.Utils;
using Verse;

namespace HugsLib.Core {
	/**
	 * A fix for Defs that ended up being assigned a duplicate short hash.
	 * Looks through all defs and when a collision is detected assigns a new short has to the Def.
	 * This is a temporary fix for a vanilla issue.
	 */
	public class ShortHashCollisionResolver {
		public static void ResolveCollisions() {
			var seenHashes = new HashSet<ushort>();
			var defsToRehash = new List<Def>();
			foreach (Type current in GenDefDatabase.AllDefTypesWithDatabases()) {
				var type = typeof(DefDatabase<>).MakeGenericType(current);
				var property = type.GetProperty("AllDefs");
				var getMethod = property.GetGetMethod();
				var allDefsInDatabase = (IEnumerable)getMethod.Invoke(null, null);
				defsToRehash.Clear();
				foreach (Def def in allDefsInDatabase) {
					if (seenHashes.Contains(def.shortHash)) {
						defsToRehash.Add(def);
					} else {
						seenHashes.Add(def.shortHash);
					}
				}
				defsToRehash.SortBy(d => d.defName);
				for (int i = 0; i < defsToRehash.Count; i++) {
					var def = defsToRehash[i];
					def.shortHash = 0;
					InjectedDefHasher.GiveShortHasToDef(def);
					Log.Message(def.defName+" "+def.shortHash);
				}
				seenHashes.Clear();
			}
		}
	}
}