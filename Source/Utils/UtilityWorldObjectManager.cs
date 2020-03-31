using System;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

#pragma warning disable 618 // Obsolete warning
// TODO: Remove in the next major update
namespace HugsLib.Utils {
	/// <summary>
	/// Handles utility WorldObjects of custom types.
	/// Utility WorldObjects are a map-independent storage method for custom data.
	/// All UWOs share the same def and aren't visible on the world, but are saved and loaded with it.
	/// </summary>
	public static class UtilityWorldObjectManager {
		public const string InjectedDefName = "UtilityWorldObject";
		public const int UtilityObjectTile = 0;

		/// <summary>
		/// Returns an existing UWO or creates a new one, adding it to the world.
		/// </summary>
		/// <typeparam name="T">Your custom type that extends UtilityWorldObject</typeparam>
		[Obsolete("It is recommended to transition to Verse.GameComponent or RimWorld.Planet.WorldComponent for data storage")]
		public static T GetUtilityWorldObject<T>() where T : UtilityWorldObject {
			var worldObjects = GetHolder();
			var obj = (T)worldObjects.ObjectsAt(UtilityObjectTile).FirstOrDefault(o => o is T);
			if (obj == null) {
				var def = DefDatabase<WorldObjectDef>.GetNamed(InjectedDefName);
				def.worldObjectClass = typeof(T);
				obj = (T)WorldObjectMaker.MakeWorldObject(def);
				def.worldObjectClass = typeof (WorldObject);
				obj.Tile = UtilityObjectTile;
				worldObjects.Add(obj);
			}
			return obj;
		}

		public static bool UtilityWorldObjectExists<T>() where T : UtilityWorldObject {
			return GetHolder().ObjectsAt(UtilityObjectTile).Any(o => o is T);
		}

		internal static void OnDefsLoaded() {
			InjectUtilityObjectDef();
		}

		internal static void OnWorldLoaded() {
			CheckForWorldObjectsWithoutDef();
		}

		// a safeguard against UWO's without a def breaking saved games
		private static void CheckForWorldObjectsWithoutDef() {
			var allObjects = GetHolder().AllWorldObjects;
			for (int i = allObjects.Count-1; i >= 0; i--) {
				var obj = allObjects[i];
				if (obj.def == null && obj is UtilityWorldObject) {
					HugsLibController.Logger.Error(obj.GetType().FullName + ".def is null on load. Forgot to call base.ExposeData()?");		
					allObjects.RemoveAt(i); // kill it with fire
				}
			}
		}

		private static WorldObjectsHolder GetHolder() {
			if (Current.Game == null || Current.Game.World == null) throw new Exception("A world must be loaded to get a WorldObject");
			return Current.Game.World.worldObjects;
		}

		private static void InjectUtilityObjectDef() {
			var def = new WorldObjectDef {
				defName = InjectedDefName,
				worldObjectClass = typeof(WorldObject),
				canHaveFaction = false,
				selectable = false,
				neverMultiSelect = true,
				useDynamicDrawer = true
			};
			
			InjectedDefHasher.GiveShortHashToDef(def, typeof(WorldObject));
			DefDatabase<WorldObjectDef>.Add(def);
		}
	}
}