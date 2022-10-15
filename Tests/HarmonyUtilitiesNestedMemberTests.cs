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
			Assert.That(Resolve(One), Is.EqualTo(OwnPrefix + "One"));
		}

		private static class A {
			public static void Two() {
			}
		}

		[Test]
		public void NestedParent() {
			Assert.That(Resolve(A.Two), Is.EqualTo(OwnPrefix + "A.Two"));
		}

		private static class B {
			public static class C {
				public static void Three() {
				}
			}
		}

		[Test]
		public void DepthLimit() {
			Assert.That(Resolve(B.C.Three, 2), Is.EqualTo("B.C.Three"));
		}

		[Test]
		public void DynamicMethod() {
			const string memberName = "testy";
			var dyn = new DynamicMethod(memberName, null, null, typeof(HarmonyUtilitiesNestedMemberTests).Module);
			Assert.That(HarmonyUtility.GetNestedMemberName(dyn), Is.EqualTo(memberName));
		}

		private static string Resolve(Action method, int maxDepth = 10) {
			return HarmonyUtility.GetNestedMemberName(method.Method, maxDepth);
		}
	}
}