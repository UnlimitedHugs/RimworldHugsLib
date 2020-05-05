﻿using System;
using System.Reflection.Emit;
using HugsLib.Utils;
using NUnit.Framework;

namespace HugsLibTests {
	[TestFixture]
	public class HarmonyUtilitiesNestedMemberTests {
		private const string OwnPrefix = nameof(HarmonyUtilitiesNestedMemberTests) + ".";

		private static void One() {
		}

		[Test]
		public void TopLevelParent() {
			Assert.AreEqual(OwnPrefix + "One", Resolve(One));
		}

		private static class A {
			public static void Two() {
			}
		}

		[Test]
		public void NestedParent() {
			Assert.AreEqual(OwnPrefix + "A.Two", Resolve(A.Two));
		}

		private static class B {
			public static class C {
				public static void Three() {
				}
			}
		}

		[Test]
		public void DepthLimit() {
			Assert.AreEqual("B.C.Three", Resolve(B.C.Three, 2));
		}

		[Test]
		public void DynamicMethod() {
			const string memberName = "testy";
			var dyn = new DynamicMethod(memberName, null, null, typeof(HarmonyUtilitiesNestedMemberTests).Module);
			Assert.AreEqual(HarmonyUtility.GetNestedMemberName(dyn), memberName);
		}

		[Test]
		public void GenericTypeMember() {
			
		}

		private static string Resolve(Action method, int maxDepth = 10) {
			return HarmonyUtility.GetNestedMemberName(method.Method, maxDepth);
		}
	}
}