#if TEST_TICKERS
using System.Collections.Generic;
using System.Linq;
using HugsLib.Utils;
using UnityEngine;
using Verse;

namespace HugsLib.Test {
	/// <summary>
	/// A setup for testing the DistributedTickScheduler and TickDelayScheduler
	/// </summary>
	public class TickersTests : ModBase {
		private static TickersTests instance;
		public static TickersTests Instance {
			get { return instance; }
		}

		private TickingThingDef testDef;

		public new ModLogger Logger {
			get { return base.Logger; }
		}

		public override string ModIdentifier {
			get { return "TickersTest"; }
		}

		protected override bool HarmonyAutoPatch {
			get { return false; }
		}

		public TickersTests() {
			instance = this;
		}

		public override void OnGUI() {
			Find.WindowStack.ImmediateWindow(9999999, new Rect(50, 50, 280, 120), WindowLayer.Super, () => {
				Widgets.Label(new Rect(0, 0, 300, 30), "DistributedTickScheduler entries: " + HugsLibController.Instance.DistributedTicker.DebugGetAllEntries().Count());
				Widgets.Label(new Rect(0, 30, 300, 30), "DistributedTickScheduler calls last tick: " + HugsLibController.Instance.DistributedTicker.DebugCountLastTickCalls());
				Widgets.Label(new Rect(0, 60, 300, 30), "DistributedTickScheduler active tickers: " + HugsLibController.Instance.DistributedTicker.DebugGetNumTickers());
				Widgets.Label(new Rect(0, 90, 300, 30), "TickDelayScheduler entries: " + HugsLibController.Instance.TickDelayScheduler.GetAllPendingCallbacks().Count());	
			}, false, false, 0);
			
		}

		public override void Initialize() {
			testDef = new TickingThingDef();
		}

		public override void MapLoaded(Map map) {
			base.MapLoaded(map);
			var center = map.Center;
			for (int i = 0; i < 50; i++) {
				var testThing = (TickingTestThing)ThingMaker.MakeThing(testDef);
				testThing.tickInterval = 30;
				GenPlace.TryPlaceThing(testThing, center, map, ThingPlaceMode.Near);
			}
			for (int i = 0; i < 50; i++) {
				var testThing = (TickingTestThing)ThingMaker.MakeThing(testDef);
				testThing.tickInterval = 50;
				GenPlace.TryPlaceThing(testThing, center+new IntVec3(0, 0, 15), map, ThingPlaceMode.Near);
			}
		}
	}

	public class TickingTestThing : ThingWithComps {
		public int tickInterval;

		public override void SpawnSetup(Map map, bool respawningAfterLoad) {
			base.SpawnSetup(map, respawningAfterLoad);
			HugsLibController.Instance.DistributedTicker.RegisterTickability(TickMethod, tickInterval, this);
		}

		public override IEnumerable<Gizmo> GetGizmos() {
			yield return new Command_Action {
				defaultLabel = "Despawn for 5",
				action = () => {
					var map = Map;
					HugsLibController.Instance.TickDelayScheduler.ScheduleCallback(() => GenSpawn.Spawn(this, Position, map), 5 * 60);
					DeSpawn();
				}
			};

			yield return new Command_Action {
				defaultLabel = "Callback in 3",
				action = () => HugsLibController.Instance.TickDelayScheduler.ScheduleCallback(MessageCallback, 3 * 60, this)
			};

			yield return new Command_Action {
				defaultLabel = "Recurring callback",
				action = () => HugsLibController.Instance.TickDelayScheduler.ScheduleCallback(MessageCallback, 3 * 60, this, true)
			};

			yield return new Command_Action {
				defaultLabel = "Clear callback",
				action = () => HugsLibController.Instance.TickDelayScheduler.TryUnscheduleCallback(MessageCallback)
			};
		}

		private void MessageCallback() {
			TickersTests.Instance.Logger.Message(ToString());
		}

		private void TickMethod() {
			GenDraw.DrawFieldEdges(new List<IntVec3>(1) {Position}, Color.magenta);
		}
	}

	public class TickingThingDef : ThingDef {
		public TickingThingDef() {
			defName = "testTickingThingDef";
			thingClass = typeof(TickingTestThing);
			label = defName;
			drawerType = DrawerType.RealtimeOnly;
			category = ThingCategory.Item;
			graphicData = new GraphicData() {
				graphicClass = typeof(Graphic_Single),
				shaderType = ShaderType.Cutout,
				texPath = "Things/Building/Art/Snowman",
				drawSize = Vector2.one
			};
			selectable = true;
			tickerType = TickerType.Never;
			isSaveable = false;
			InjectedDefHasher.GiveShortHashToDef(this, typeof(ThingDef));
		}
	}
}
#endif