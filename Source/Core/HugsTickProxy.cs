using Verse;

namespace HugsLib.Core {
	/// <summary>
	/// Forwards ticks to the controller. Will not be saved and is never spawned.
	/// </summary>
	public class HugsTickProxy : Thing {
		// a precaution against ending up in a save. Shouldn't happen, as it is never spawned.
		public bool CreatedByController { get; internal set; }

		public HugsTickProxy() {
			def = new ThingDef{ tickerType = TickerType.Normal, isSaveable = false };
		}

		public override void Tick() {
			if (CreatedByController) HugsLibController.Instance.OnTick();
		}
	}
}