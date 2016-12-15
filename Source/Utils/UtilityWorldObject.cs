using RimWorld.Planet;
using Verse;

namespace HugsLib.Utils {
	/**
	 * Base type for utility WorldObjects repurposed to store data. See UtilityWorldObjectManager for more info.
	 */
	public abstract class UtilityWorldObject : WorldObject {
		public override void SpawnSetup() {
		}

		public override void PostRemove() {
		}

		public override void Print(LayerSubMesh subMesh) {
		}

		public override void Draw() {
		}
	}
}