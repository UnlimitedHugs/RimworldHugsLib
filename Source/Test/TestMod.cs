#if TEST_MOD
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using HugsLib.Settings;
using HugsLib.Source.Settings;
using HugsLib.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using Verse;

namespace HugsLib.Test {
	/// <summary>
	/// This mod is for testing the various facilities of the library
	/// </summary>
	public class TestMod : ModBase {
		public static TestMod Instance { get; private set; }

		internal new ModLogger Logger {
			get { return base.Logger; }
		}

		public TestMod() {
			Instance = this;
		}

		public override string ModIdentifier {
			get { return "TestMod"; }
		}

		protected override bool HarmonyAutoPatch {
			get { return false; }
		}

		public override void Initialize() {
			Logger.Message("Initialized");
		}

		public override void Tick(int currentTick) {
			//Logger.Message("Tick:"+currentTick);
		}

		public override void Update() {
			//Logger.Message("Update");
		}

		public override void FixedUpdate() {
			//Logger.Message("FixedUpdate");
		}

		public override void OnGUI() {
			//Logger.Message("OnGUI");
		}

		public override void WorldLoaded() {
			Logger.Message("WorldLoaded");
			UtilityWorldObjectManager.GetUtilityWorldObject<TestUWO1>().UpdateAndReport();
			UtilityWorldObjectManager.GetUtilityWorldObject<TestUWO2>().UpdateAndReport();
		}

		public override void MapComponentsInitializing(Map map) {
			Logger.Message("MapComponentsInitializing on map:" + map);
		}

		public override void MapGenerated(Map map) {
			Logger.Message("MapGenerated:" + map);
		}

		public override void MapLoaded(Map map) {
			Logger.Message("MapLoaded:" + map);
			try {
				map.mapDrawer.MapMeshDirty(new IntVec3(0, 0, 0), MapMeshFlag.Buildings);
			} catch (Exception e) {
				Logger.Error("MapLoaded fired before map mesh regeneration " + e);
			}

			//HugsLibController.Instance.CallbackScheduler.ScheduleCallback(() => Logger.Trace("scheduler callback"), 150, true);
		}

		public override void MapDiscarded(Map map) {
			Logger.Message("MapDiscarded:" + map);
		}

		public override void SceneLoaded(Scene scene) {
			Logger.Message("SceneLoaded:" + scene.name);
		}

		public override void SettingsChanged() {
			Logger.Message("SettingsChanged");
		}

		private enum HandleEnum {
			DefaultValue,
			ValueOne,
			ValueTwo
		}

		public override void DefsLoaded() {
			Logger.Message("DefsLoaded");
			var str = Settings.GetHandle("str", "String value", "", "value");
			var spinner = Settings.GetHandle("intSpinner", "Spinner", "desc", 5, Validators.IntRangeValidator(0, 30));
			spinner.SpinnerIncrement = 2;
			var enumHandle = Settings.GetHandle("enumThing", "Enum setting", "", HandleEnum.DefaultValue, null, "test_enumSetting_");
			var toggle = Settings.GetHandle("toggle", "Toggle setting extra long title that would not fit into one line", "Toggle setting", false);
			var custom = Settings.GetHandle("custom", "custom setting", "custom setting desc", false);
			custom.CustomDrawerHeight = 30f;
			custom.CustomDrawer = rect => {
				if (Widgets.ButtonText(new Rect(rect.x, rect.y, rect.width, custom.CustomDrawerHeight), "I Iz Button")) {
					custom.CustomDrawerHeight = custom.CustomDrawerHeight > 30 ? 30f : 400f;
				}
				return false;
			};
			TestCustomTypeSetting();
			//TestConditionalVisibilitySettings();	
		}



		private void TestConditionalVisibilitySettings() {
			for (int i = 0; i < 50; i++) {
				var toggle = Settings.GetHandle("toggle" + i, "toggle", null, false);
				var index = i;
				toggle.VisibilityPredicate = () => Input.mousePosition.x/22 < index;
			}
		}

		private void TestCustomTypeSetting() {
			var custom = Settings.GetHandle<CustomHandleType>("customType", null, null);
			custom.NeverVisible = true;
			if (custom.Value == null) custom.Value = new CustomHandleType { Nums = new List<int>() };
			custom.Value.Nums.Add(Rand.Range(1, 100));
			if (custom.Value.Nums.Count > 10) {
				custom.Value.Nums.RemoveAt(0);
			}
			custom.Value.Prop++;
			HugsLibController.SettingsManager.SaveChanges();
			Logger.Trace(string.Format("Custom setting values: Nums:{0} Prop:{1}", custom.Value.Nums.Join(","), custom.Value.Prop));
		}

		//<customType>aasd1w423</customType>
		[Serializable]
		public class CustomHandleType : SettingHandleConvertible {
			[XmlElement] public List<int> Nums = new List<int>();

			[XmlElement]
			public int Prop { get; set; }

			public override void FromString(string settingValue) {
				SettingHandleConvertibleUtility.DeserializeValuesFromString(settingValue, this);
			}

			public override string ToString() {
				return SettingHandleConvertibleUtility.SerializeValuesToString(this);
			}
		}

	}


}
#endif