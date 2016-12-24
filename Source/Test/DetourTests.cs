#if TEST_DETOUR
using System;
using System.Reflection;
using HugsLib.Source.Detour;
using HugsLib.Utils;
using Verse;

namespace HugsLib.Test {
	public class DetourTests : ModBase {
		public override string ModIdentifier {
			get { return "DetourTests"; }
		}

		public new ModLogger Logger {
			get { return base.Logger; }
		}

		public static DetourTests Instance { get; private set; }

		public DetourTests() {
			Instance = this;
		}

		private class DetourTestDestinations {
			// simple methods
			[DetourMethod(typeof (DetourTestSources), "PublicInstanceMethod")]
			public void PublicInstanceMethod() {
				Instance.Logger.Message("public instance method");
			}

			[DetourMethod(typeof (DetourTestSources), "PrivateInstanceMethod")]
			private void PrivateInstanceMethod() {
				Instance.Logger.Message("private instance method");
			}

			[DetourMethod(typeof (DetourTestSources), "PublicStaticMethod")]
			public static void PublicStaticMethod() {
				Instance.Logger.Message("public static method");
			}

			[DetourMethod(typeof (DetourTestSources), "PrivateStaticMethod")]
			private static void PrivateStaticMethod() {
				Instance.Logger.Message("private static method");
			}

			// parameter overloads
			[DetourMethod(typeof (DetourTestSources), "Overload")]
			public void Overload(string asd, string qwe) {
				Instance.Logger.Message("overload string");
			}

			[DetourMethod(typeof (DetourTestSources), "Overload")]
			public void Overload(int asd, int qwe) {
				Instance.Logger.Message("overload int");
			}

			// properties
			[DetourProperty(typeof (DetourTestSources), "GetterOnly", DetourProperty.Getter)]
			public string GetterOnly {
				get {
					Instance.Logger.Message("public getterOnly getter");
					return "asd";
				}
				set { Instance.Logger.Error("public getterOnly setter"); }
			}

			[DetourProperty(typeof (DetourTestSources), "SetterOnly", DetourProperty.Setter)]
			public string SetterOnly {
				get {
					Instance.Logger.Error("public setterOnly getter");
					return "asd";
				}
				set { Instance.Logger.Message("public setterOnly setter"); }
			}

			[DetourProperty(typeof (DetourTestSources), "Both")]
			public string Both {
				get {
					Instance.Logger.Message("public both getter");
					return "asd";
				}
				set { Instance.Logger.Message("public both setter"); }
			}

			private int CompatTestReturn(int param1, string param2) {
				return 0;
			}

			private string CompatTestParamsTypes(int param1, int param2) {
				return null;
			}

			private string CompatTestParamsCount(int param1, int param2, int param3) {
				return null;
			}
		}

		public override void Initialize() {
			base.Initialize();
			// instance tests
			Logger.Message("Running tests...");
			Type sourceType = typeof (DetourTestSources);
			DetourTestSources sources = new DetourTestSources();
			sources.PublicInstanceMethod();
			sourceType.GetMethod("PrivateInstanceMethod", Helpers.AllBindingFlags).Invoke(sources, null);
			
			// static tests
			DetourTestSources.PublicStaticMethod();
			sourceType.GetMethod("PrivateStaticMethod", Helpers.AllBindingFlags).Invoke(null, null);

			// overloads
			sources.Overload(1, 1);
			sources.Overload("asd", "qwe");

			// properties
			var x = sources.GetterOnly;
			sources.GetterOnly = "asd";
			x = sources.SetterOnly;
			sources.SetterOnly = "asd";
			x = sources.Both;
			sources.Both = "asd";

			// source-destinaton compatibility
			// these cover only some of the possible cases and should be refactored into a proper test suite
			var sourceMethod = typeof (DetourTestSources).GetMethod("CompatTest", Helpers.AllBindingFlags);
			if (DetourValidator.IsValidDetourPair(sourceMethod, typeof(DetourTestDestinations).GetMethod("CompatTestReturn", Helpers.AllBindingFlags))) {
				Logger.Error("non-matching return types passed");	
			}
			if (DetourValidator.IsValidDetourPair(sourceMethod, typeof(DetourTestDestinations).GetMethod("CompatTestParamsTypes", Helpers.AllBindingFlags))) {
				Logger.Error("non-matching param types passed");
			}
			if (DetourValidator.IsValidDetourPair(sourceMethod, typeof(DetourTestDestinations).GetMethod("CompatTestParamsCount", Helpers.AllBindingFlags))) {
				Logger.Error("non-matching param count passed");
			}
			if (DetourValidator.IsValidDetourPair(sourceMethod, typeof(DetourTestContainerWithField).GetMethod("CompatTest", Helpers.AllBindingFlags))) {
				Logger.Error("type with field passed");
			}
			sourceMethod = typeof(DetourTestSources).GetMethod("CompatTestTwo", Helpers.AllBindingFlags);
			if (DetourValidator.IsValidDetourPair(sourceMethod, typeof(DetourTestContainerStatic).GetMethod("CompatTestExtensionInvalid", Helpers.AllBindingFlags))) {
				Logger.Error("invalid extension method passed");
			}
			if (!DetourValidator.IsValidDetourPair(sourceMethod, typeof(DetourTestContainerStatic).GetMethod("CompatTestExtensionValid", Helpers.AllBindingFlags))) {
				Logger.Error("valid extension method failed");
			}

			Logger.Message("detour tests finished");
		}
	}

	public class DetourTestSources {
		// simple methods
		public void PublicInstanceMethod() {
			DetourTests.Instance.Logger.Error("public instance method");
		}

		private void PrivateInstanceMethod() {
			DetourTests.Instance.Logger.Error("private instance method");
		}

		public static void PublicStaticMethod() {
			DetourTests.Instance.Logger.Error("public static method");
		}

		private static void PrivateStaticMethod() {
			DetourTests.Instance.Logger.Error("private static method");
		}

		// parameter overloads
		public void Overload(string asd, string qwe) {
			DetourTests.Instance.Logger.Error("public overload string");
		}

		public void Overload(int asd, int qwe) {
			DetourTests.Instance.Logger.Error("public overload int");
		}
		
		// Source-destination compatibility
		public string CompatTest(int param1, string param2) {
			return null;
		}

		public string CompatTestTwo(int param1, string param2) {
			return null;
		}

		// properties
		public string GetterOnly {
			get {
				DetourTests.Instance.Logger.Error("public getterOnly getter");
				return "asd";
			}
			set { DetourTests.Instance.Logger.Message("public getterOnly setter"); }
		}

		public string SetterOnly {
			get {
				DetourTests.Instance.Logger.Message("public setterOnly getter");
				return "asd";
			}
			set { DetourTests.Instance.Logger.Error("public setterOnly setter"); }
		}

		public string Both {
			get {
				DetourTests.Instance.Logger.Error("public both getter");
				return "asd";
			}
			set { DetourTests.Instance.Logger.Error("public both setter"); }
		}
	}

	public static class DetourTestContainerStatic {
		private static string CompatTestExtensionInvalid(this DetourTestContainerWithField self, int param1, string param2) {
			return null;
		}
		private static string CompatTestExtensionValid(this DetourTestSources self, int param1, string param2) {
			return null;
		}
	}

	public class DetourTestContainerWithField {
		private int field;
		public string CompatTest(int param1, string param2) {
			return null;
		}
	}
}

#endif