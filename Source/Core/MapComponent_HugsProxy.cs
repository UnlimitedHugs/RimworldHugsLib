using Verse;

namespace HugsLib.Core {
	/**
	 * A map component to hook into the map loading flow. Will not be saved in the map data.
	 */
	public class MapComponent_HugsProxy : MapComponent {
		public MapComponent_HugsProxy() {
			var ownType = GetType();
			var firstInstance = !Find.Map.components.Any(c => c.GetType() == ownType); // multiple instantiation safeguard
			if(firstInstance) HugsLibController.Instance.OnMapComponentsIntializing();
		}

		// we don't want this component to end up in the save
		public override void MapComponentTick() {
			Find.Map.components.Remove(this);
		}
	}
}