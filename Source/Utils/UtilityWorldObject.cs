using RimWorld.Planet;
using Verse;

namespace HugsLib.Utils {
	/// <summary>
	/// Base type for utility WorldObjects repurposed to store data. See UtilityWorldObjectManager for more info.
	/// </summary>
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