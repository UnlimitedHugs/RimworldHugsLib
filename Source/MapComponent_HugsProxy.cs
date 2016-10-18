using Verse;

namespace HugsLib {
	/**
	 * A map component to hook into the map loading flow. Will not be saved in the map data.
	 * Warning: this component will be instantiated by the game as many times as there are HugsLib assemblies loaded
	 */
	public class MapComponent_HugsProxy : MapComponent {
		public MapComponent_HugsProxy() {
			var ownType = GetType();
			var firstInstance = !Find.Map.components.Any(c => c.GetType() == ownType);
			if(firstInstance) HugsLibController.Instance.OnMapComponentsIntializing();
		}

		// we don't want this component to end up in the save
		public override void MapComponentTick() {
			Find.Map.components.Remove(this);
		}
	}
}