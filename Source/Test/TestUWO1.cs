#if TEST_MOD
using HugsLib.Utils;
using Verse;

namespace HugsLib.Test {
	public class TestUWO1 : UtilityWorldObject {
		private int testInt;
		
		public override void ExposeData() {
			base.ExposeData();
			Scribe_Values.LookValue(ref testInt, "testInt", 0);
		}
		
		public void UpdateAndReport() {
			testInt++;
			TestMod.Instance.Logger.Message(GetType().Name + " testInt:" + testInt);
		}
	}
}
#endif