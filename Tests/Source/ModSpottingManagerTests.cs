﻿using System;
using System.Collections.Generic;
using System.IO;
using HugsLib.Spotter;
using NUnit.Framework;

namespace HugsLibTests {
	[TestFixture]
	public class ModSpottingManagerTests {
		private FileInfo testingFile;

		[SetUp]
		public void SetUpTest() {
			testingFile = new FileInfo(Path.GetTempFileName());
		}
		
		[TearDown]
		public void CleanUpTest() {
			if(testingFile.Exists) testingFile.Delete();
		}

		[Test]
		public void BasicTest() {
			var packageIds = new[] {"one", "two"};
			var spotter = PrepareSpotter(3, true, packageIds);
			Assert.IsTrue(spotter.FirstTimeSeen("one"), "first seen one");
			Assert.IsTrue(spotter.FirstTimeSeen("two"), "first seen two");
			Assert.IsTrue(!spotter.FirstTimeSeen("three"), "not first seen three");
			Assert.AreEqual(3, spotter.TryGetFirstSeenTime("one")?.Year, "one first time seen now");
			Assert.IsTrue(!spotter.TryGetLastSeenTime("one").HasValue, "one no last time seen");
		}

		[Test]
		public void PersistenceTest() {
			var packageIds = new[] {"one", "two"};
			PrepareSpotter(1, true, packageIds);
			
			var spotter = PrepareSpotter(2, false, packageIds);
			Assert.IsTrue(!spotter.FirstTimeSeen("one"), "one not first time seen");
			Assert.AreEqual(1, spotter.TryGetFirstSeenTime("one")?.Year, "one first seen time");
			Assert.AreEqual(1, spotter.TryGetLastSeenTime("one")?.Year, "one last seen time");
		}

		[Test]
		public void LastSeenTimeTest() {
			var packageIds = new[] {"one"};
			PrepareSpotter(1, true, packageIds);

			PrepareSpotter(2, false, packageIds);
			
			var spotter = PrepareSpotter(3, false, packageIds);
			Assert.AreEqual(1, spotter.TryGetFirstSeenTime("one")?.Year, "one first seen time");
			Assert.AreEqual(2, spotter.TryGetLastSeenTime("one")?.Year, "one last seen time");
		}

		[Test]
		public void NewForceFirstTimeSeenTest() {
			var packageIds = new[] {"one", "two"};
			var spotter = PrepareSpotter(1, true, packageIds);
			Assert.IsTrue(!spotter.TryGetFirstSeenTime("three").HasValue, "three not seen before");
			spotter.ToggleFirstTimeSeen("three", true);
			Assert.IsTrue(spotter.FirstTimeSeen("three"), "three first time seen after");

			spotter = PrepareSpotter(2, false, packageIds);
			Assert.AreEqual(1, spotter.TryGetFirstSeenTime("three")?.Year, "three first seen time after reload");
			Assert.IsTrue(!spotter.FirstTimeSeen("three"), "three not first time seen after reload");
		}

		[Test]
		public void OldForceFirstTimeSeenTest() {
			var packageIds = new[] {"one", "two"};
			PrepareSpotter(1, true, packageIds);

			var spotter = PrepareSpotter(2, false, packageIds);
			Assert.IsTrue(spotter.TryGetFirstSeenTime("one").HasValue, "one seen before");
			Assert.IsTrue(!spotter.FirstTimeSeen("one"), "one not first time seen before");
			spotter.ToggleFirstTimeSeen("one", true);
			Assert.IsTrue(spotter.FirstTimeSeen("one"), "one first time seen after");

			spotter = PrepareSpotter(3, false, packageIds);
			Assert.AreEqual(2, spotter.TryGetFirstSeenTime("one")?.Year, "one first seen time final");
			Assert.AreEqual(2, spotter.TryGetLastSeenTime("one")?.Year, "one last seen time final");
		}

		[Test]
		public void ChangingModSetTest() {
			var packageIds = new[] {"one", "two"};
			PrepareSpotter(1, true, packageIds);
			
			packageIds = new[] {"two", "three"};
			var spotter = PrepareSpotter(2, false, packageIds);
			Assert.AreEqual(1, spotter.TryGetLastSeenTime("one")?.Year, "one last seen at 2");
			Assert.AreEqual(1, spotter.TryGetLastSeenTime("two")?.Year, "two last seen at 2");
			Assert.IsTrue(!spotter.FirstTimeSeen("two"), "two not seen first time at 2");
			Assert.IsTrue(spotter.FirstTimeSeen("three"), "three seen first time at 2");
			Assert.AreEqual(2, spotter.TryGetFirstSeenTime("three")?.Year, "three first time seen at 2");
			Assert.IsTrue(!spotter.TryGetFirstSeenTime("four").HasValue, "four not seen at 2");

			packageIds = new[] {"one", "four"};
			spotter = PrepareSpotter(2, false, packageIds);
			Assert.IsTrue(!spotter.FirstTimeSeen("one"), "one not seen first time at 3");
			Assert.AreEqual(1, spotter.TryGetLastSeenTime("one")?.Year, "one last seen at 3");
			Assert.AreEqual(2, spotter.TryGetLastSeenTime("two")?.Year, "two last seen at 3");
			Assert.IsTrue(spotter.FirstTimeSeen("four"), "four seen first time at 3");
		}

		[Test]
		public void CaseInsensitivePackageIdTest() {
			var packageIds = new[] {"Author.oNe"};
			var spotter = PrepareSpotter(1, true, packageIds);
			Assert.IsTrue(spotter.FirstTimeSeen("Author.oNe"), "one same case");
			Assert.IsTrue(spotter.FirstTimeSeen("author.OnE"), "one different case");
			Assert.IsTrue(!spotter.TryGetFirstSeenTime("Author.0ne").HasValue, "unrelated id not seen");
		}

		[Test]
		public void ToggleFirstTimeSeen() {
			var packageIds = new[] {"one"};
			var spotter = PrepareSpotter(1, true, packageIds);
			Assert.IsTrue(spotter.FirstTimeSeen("one"), "one first time seen before");
			spotter.ToggleFirstTimeSeen("one", true);
			Assert.IsTrue(spotter.FirstTimeSeen("one"), "one first time seen 1");
			spotter.ToggleFirstTimeSeen("one", false);
			Assert.IsFalse(spotter.FirstTimeSeen("one"), "one not first time seen 2");
			spotter.ToggleFirstTimeSeen("one", false);
			Assert.IsFalse(spotter.FirstTimeSeen("one"), "one not first time seen 3");

			spotter = PrepareSpotter(2, false, packageIds);
			Assert.IsFalse(spotter.FirstTimeSeen("one"), "one not first time seen after reload 1");
			spotter.ToggleFirstTimeSeen("one", true);
			Assert.IsTrue(spotter.FirstTimeSeen("one"), "one toggled after reload");

			spotter = PrepareSpotter(3, false, packageIds);
			Assert.IsFalse(spotter.FirstTimeSeen("one"), "one not first time seen after reload 2");
		}

		private ModSpottingManager PrepareSpotter(int currentYear, bool deleteFile, IEnumerable<string> packageIds) {
			if(deleteFile && testingFile.Exists) testingFile.Delete();
			var spotter = new ModSpottingManager(testingFile.Name) {
				DateTimeSource = new TestingDateTimeSource {Time = new DateTime(currentYear, 1, 1)}
			};
			spotter.InspectPackageIds(packageIds);
			return spotter;
		}

		private class TestingDateTimeSource : ICurrentDateTimeSource {
			public DateTime Time { get; set; } = new DateTime(0);
			public DateTime Now {
				get { return Time; }
			}
		}
	}
}