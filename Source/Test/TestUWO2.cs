using HugsLib.Utils;
using Verse;

namespace HugsLib.Test {
#if TEST_MOD
	public class TestUWO2 : UtilityWorldObject {
		private string testString = "";
		
		public override void ExposeData() {
			base.ExposeData();
			Scribe_Values.LookValue(ref testString, "testString", "");
		}

		public void UpdateAndReport() {
			testString += "+";
			TestMod.Instance.Logger.Message(GetType().Name + " testString:" + testString);
		}
	}
#endif
}