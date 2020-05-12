using System;
using System.Collections.Generic;
using System.Linq;
using HugsLib.Core;
using NUnit.Framework;

namespace HugsLibTests {
	[TestFixture]
	public class LibraryVersionCheckerTests {
		private readonly ModLoggerStub logger = new ModLoggerStub();

		[Test]
		public void NoRequiredVersions() {
			var report = RunChecker();
			Assert.IsNull(report, "none");
			report = RunChecker("one");
			Assert.IsNull(report, "one");
		}

		[Test]
		public void RequiresLower() {
			var report = RunChecker("two");
			Assert.IsNull(report);
		}

		[Test]
		public void RequiresEqual() {
			var report = RunChecker("three");
			Assert.IsNull(report);
		}

		[Test]
		public void RequiresHigher() {
			var report = RunChecker("four");
			AssertReport(report, "four", "1.1");
		}

		[Test]
		public void MultipleNoHigher() {
			var report = RunChecker("one", "two", "three");
			Assert.IsNull(report);
		}

		[Test]
		public void MultipleOneHigher() {
			var report = RunChecker("one", "two", "three", "four");
			AssertReport(report, "four", "1.1");
		}

		[Test]
		public void MultipleHighestReported() {
			var report = RunChecker("three", "four", "five");
			AssertReport(report, "four", "1.1");
		}

		[TearDown]
		public void Cleanup() {
			logger.AssertNothingLogged();
		}

		private LibraryVersionChecker.VersionMismatchReport? RunChecker(params string[] modIdsToInclude) {
			var checker = new LibraryVersionChecker(new Version(1, 0, 0), logger) {
				RequiredLibraryVersionEnumerator =
					modIdsToInclude.Select(id => {
						var requiredVersion = requiredVersionsForMods[id] is string v ? new Version(v) : null;
						return (id, requiredVersion);
					})
			};
			var task = checker.RunVersionCheckAsync();
			return checker.TryWaitForTaskResult(task, TimeSpan.FromSeconds(1));
		}

		private static void AssertReport(
			LibraryVersionChecker.VersionMismatchReport? report, string modName, string version) {
			Assert.That(report?.ExpectedVersion, Is.EqualTo(new Version(version)));
			Assert.That(report?.ModName, Is.EqualTo(modName));
		}

		private Dictionary<string, string> requiredVersionsForMods =
			new Dictionary<string, string> {
				{"one", null},
				{"two", "0.9"},
				{"three", "1.0"},
				{"four", "1.1"},
				{"five", "1.0.5"}
			};
	}
}