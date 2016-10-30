using System;
using UnityEngine;
using Verse;

namespace HugsLib {
	/**
	 * Detects when the game reloads all defs. Uses polling and and an injected def.
	 */
	public class DefReloadWatcher {
		private const float CheckEverySeconds = .1f;
		private const string TokenDefName = "HugsLibDetectionToken";

		private readonly Action reloadedCallback;
		private float nextCheckTime;
		private bool waitingForLongEvent;
		
		public DefReloadWatcher(Action reloadedCallback) {
			this.reloadedCallback = reloadedCallback;
			InjectDef();
		}

		public void Update() {
			var curTime = Time.realtimeSinceStartup;
			if (waitingForLongEvent || nextCheckTime > curTime) return;
			nextCheckTime = curTime + CheckEverySeconds;
			if (DefDatabase<ThingDef>.GetNamedSilentFail(TokenDefName) != null) return;
			waitingForLongEvent = true;
			LongEventHandler.ExecuteWhenFinished(() => {
				waitingForLongEvent = false;
				InjectDef();
				if (reloadedCallback != null) reloadedCallback();
			});
		}

		private void InjectDef() {
			if (DefDatabase<ThingDef>.GetNamedSilentFail(TokenDefName) != null) return;
			var def = new ThingDef {
				defName = TokenDefName,
				label = "HugsLib reload detector",
				thingClass = typeof(Thing),
				menuHidden = true
			};
			HugsLibUtility.AddSHortHashToInjectedDef(def);
			DefDatabase<ThingDef>.Add(def);
		}
	}
}