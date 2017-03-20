#if TEST_ATTRIB
using System;
using System.Collections.Generic;
using System.Reflection;
using HugsLib.Source.Attrib;
using HugsLib.Utils;

namespace HugsLib.Test {
	/// <summary>
	/// Tests for the AttributeDetector callback system
	/// No tests for: signature validation, exceptions during invocation, detection of new types
    /// </summary>
	public class AttributeTests : ModBase {
		public override string ModIdentifier {
			get { return "AttributeTests"; }
		}

		private static readonly List<string> classCalls = new List<string>();
		private static readonly List<string> repeatCalls = new List<string>();
		private static readonly List<string> memberCalls = new List<string>();
			
		[DetectableAttributeHandler(typeof(TestAttributeClass))]
		public static void ClassHandler(MemberInfo info, Attribute attrib) { // handle classes
			if(attrib == null) throw new Exception("null attrib");
			classCalls.Add(info.Name);
		}

		[DetectableAttributeHandler(typeof(TestAttributeClass))]
		public static void RepeatClassHandler(MemberInfo info, Attribute attrib) { // additional handler for classes
			repeatCalls.Add(info.Name);
		}

		[DetectableAttributeHandler(typeof(TestAttributeMember))] // handle members
		public static void MemberHandler(MemberInfo info, Attribute attrib) {
			if (attrib == null) throw new Exception("null attrib");
			memberCalls.Add(info.Name);
		}

		public override void Initialize() {
			var repeatCallsMatch = true;
			if (repeatCalls.Count == classCalls.Count) {
				for (int i = 0; i < repeatCalls.Count; i++) {
					if (repeatCalls[i] != classCalls[i]) repeatCallsMatch = false;
				}
			} else {
				repeatCallsMatch = false;
			}
			if (classCalls.Count == 2 && classCalls[0] == "RandomClassOne" && classCalls[1] == "RandomClassTwo" &&
			    memberCalls.Count == 2 && memberCalls[0] == "MethodOne" && memberCalls[1] == "MethodTwo" && 
				repeatCallsMatch) {
				Logger.Trace("AttributeTests passed");
			} else {
				Logger.Error("AttributeTests failed! Details:");
				Logger.Error("classCalls:{0} :: memberCalls:{1}", classCalls.Join(","), memberCalls.Join(","));
			}
		}
	}
	
	[AttributeUsage(AttributeTargets.Class)]
	public class TestAttributeClass : Attribute, IDetectableAttribute {
	}

	[AttributeUsage(AttributeTargets.Method)]
	public class TestAttributeMember : Attribute, IDetectableAttribute {
	}

	[TestAttributeClass]
	public class RandomClassOne {
	}

	[TestAttributeClass]
	public class RandomClassTwo {
		[TestAttributeMember]
		private void MethodOne() {
		}
	}

	public class RandomClassThree {
		[TestAttributeMember]
		private void MethodTwo() {
		}
	}
}
#endif