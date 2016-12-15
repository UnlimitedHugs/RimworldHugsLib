using System;
using Verse;

namespace HugsLib.Utils {
	public static class MapComponentUtility {
		/**
		 * Injects a map component into the current map if it does not already exist. 
		 * Required for new MapComponents that were not active at map creation.
		 * The injection is performed at ExecuteWhenFinished to allow calling this method in MapComponent constructors.
		 */
		public static void EnsureIsActive(this MapComponent mapComponent) {
			if (mapComponent == null) throw new Exception("MapComponent is null");
			LongEventHandler.ExecuteWhenFinished(() => {
				if (mapComponent.map == null || mapComponent.map.components == null) throw new Exception("The map component requires a loaded map to be made active.");
				var components = mapComponent.map.components;
				if (components.Any(c => c == mapComponent)) return;
				components.Add(mapComponent);
			});
		}

		
		// Gets the map component of the given type from a map
		public static T GetMapComponent<T>(this Map map) where T : MapComponent {
			if (map == null || map.components == null) throw new Exception("Cannot get component from null or uninitialized map");
			var comp = (T)map.components.Find(c => c is T);
			if (comp == null) throw new Exception(string.Format("Map component of type {0} not found in map", typeof(T)));
			return comp;
		}
	}
}