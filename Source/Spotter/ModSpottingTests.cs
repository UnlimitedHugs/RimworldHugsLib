#if TEST_MOD
using System;
using System.Collections.Generic;
using System.IO;
using Verse;

namespace HugsLib.Spotter {
	/// <summary>
	/// Unit tests for <see cref="ModSpottingManager"/>
	/// </summary>
	internal static class ModSpottingTests {
		public static void Run() {
			try {
				BasicTest();
				PersistenceTest();
				LastSeenTimeTest();
				NewForceFirstTimeSeenTest();
				OldForceFirstTimeSeenTest();
				ChangingModSetTest();
			} catch (Exception e) {
				HugsLibController.Logger.Error(
					$"{nameof(ModSpottingTests)} assert failed: {e.Message}\n{e.StackTrace}"
				);
			} finally {
				DeleteTestingFile();
			}
		}

		private static void BasicTest() {
			var packageIds = new[] {"one", "two"};
			var spotter = PrepareSpotter(3, true, packageIds);
			Assert(spotter.FirstTimeSeen("one"), "first seen one");
			Assert(spotter.FirstTimeSeen("two"), "first seen two");
			Assert(!spotter.FirstTimeSeen("three"), "not first seen three");
			AssertEqual(3, spotter.TryGetFirstSeenTime("one")?.Year, "one first time seen now");
			Assert(!spotter.TryGetLastSeenTime("one").HasValue, "one no last time seen");
		}

		private static void PersistenceTest() {
			var packageIds = new[] {"one", "two"};
			PrepareSpotter(1, true, packageIds);
			
			var spotter = PrepareSpotter(2, false, packageIds);
			Assert(!spotter.FirstTimeSeen("one"), "one not first time seen");
			AssertEqual(1, spotter.TryGetFirstSeenTime("one")?.Year, "one first seen time");
			AssertEqual(1, spotter.TryGetLastSeenTime("one")?.Year, "one last seen time");
		}

		private static void LastSeenTimeTest() {
			var packageIds = new[] {"one"};
			PrepareSpotter(1, true, packageIds);

			PrepareSpotter(2, false, packageIds);
			
			var spotter = PrepareSpotter(3, false, packageIds);
			AssertEqual(1, spotter.TryGetFirstSeenTime("one")?.Year, "one first seen time");
			AssertEqual(2, spotter.TryGetLastSeenTime("one")?.Year, "one last seen time");
		}

		private static void NewForceFirstTimeSeenTest() {
			var packageIds = new[] {"one", "two"};
			var spotter = PrepareSpotter(1, true, packageIds);
			Assert(!spotter.TryGetFirstSeenTime("three").HasValue, "three not seen before");
			spotter.MakeFirstTimeSeen("three");
			Assert(spotter.FirstTimeSeen("three"), "three first time seen after");

			spotter = PrepareSpotter(2, false, packageIds);
			AssertEqual(1, spotter.TryGetFirstSeenTime("three")?.Year, "three first seen time after reload");
			Assert(!spotter.FirstTimeSeen("three"), "three not first time seen after reload");
		}

		private static void OldForceFirstTimeSeenTest() {
			var packageIds = new[] {"one", "two"};
			PrepareSpotter(1, true, packageIds);

			var spotter = PrepareSpotter(2, false, packageIds);
			Assert(spotter.TryGetFirstSeenTime("one").HasValue, "one seen before");
			Assert(!spotter.FirstTimeSeen("one"), "one not first time seen before");
			spotter.MakeFirstTimeSeen("one");
			Assert(spotter.FirstTimeSeen("one"), "one first time seen after");

			spotter = PrepareSpotter(3, false, packageIds);
			AssertEqual(2, spotter.TryGetFirstSeenTime("one")?.Year, "one first seen time final");
			AssertEqual(2, spotter.TryGetLastSeenTime("one")?.Year, "one last seen time final");
		}

		private static void ChangingModSetTest() {
			var packageIds = new[] {"one", "two"};
			PrepareSpotter(1, true, packageIds);
			
			packageIds = new[] {"two", "three"};
			var spotter = PrepareSpotter(2, false, packageIds);
			AssertEqual(1, spotter.TryGetLastSeenTime("one")?.Year, "one last seen at 2");
			AssertEqual(1, spotter.TryGetLastSeenTime("two")?.Year, "two last seen at 2");
			Assert(!spotter.FirstTimeSeen("two"), "two not seen first time at 2");
			Assert(spotter.FirstTimeSeen("three"), "three seen first time at 2");
			AssertEqual(2, spotter.TryGetFirstSeenTime("three")?.Year, "three first time seen at 2");
			Assert(!spotter.TryGetFirstSeenTime("four").HasValue, "four not seen at 2");

			packageIds = new[] {"one", "four"};
			spotter = PrepareSpotter(2, false, packageIds);
			Assert(!spotter.FirstTimeSeen("one"), "one not seen first time at 3");
			AssertEqual(1, spotter.TryGetLastSeenTime("one")?.Year, "one last seen at 3");
			AssertEqual(2, spotter.TryGetLastSeenTime("two")?.Year, "two last seen at 3");
			Assert(spotter.FirstTimeSeen("four"), "four seen first time at 3");
		}

		private static void Assert(bool condition, string expectedMessage) {
			if (!condition) throw new Exception($"expected {expectedMessage}");
		}

		private static void AssertEqual<T>(T expected, T actual, string expectedMessage) {
			if (!Equals(expected, actual)) throw new Exception($"{expectedMessage}: expected: {expected.ToStringSafe()}, actual: {actual.ToStringSafe()}");
		}

		private static ModSpottingManager PrepareSpotter(int currentYear, bool deleteFile, IEnumerable<string> packageIds) {
			var testingFile = GetTestingFilePath();
			if(deleteFile) DeleteTestingFile();
			var spotter = new ModSpottingManager(testingFile.Name) {
				DateTimeSource = new TestingDateTimeSource {Time = new DateTime(currentYear, 1, 1)}
			};
			spotter.InspectPackageIds(packageIds);
			return spotter;
		}

		private static FileInfo GetTestingFilePath() {
			string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "HugsLib");
			var directoryInfo = new DirectoryInfo(path);
			if (!directoryInfo.Exists) directoryInfo.Create();
			return new FileInfo(Path.Combine(path, "SpottingTests.xml"));
		}

		private static void DeleteTestingFile() {
			var file = GetTestingFilePath();
			if (file.Exists) {
				file.Delete();
			}
		}

		private class TestingDateTimeSource : ICurrentDateTimeSource {
			public DateTime Time { get; set; } = new DateTime(0);
			public DateTime Now {
				get { return Time; }
			}
		}
	}
}
#endif