using System.IO;
using HugsLib.Spotter;
using NUnit.Framework;

namespace HugsLibTests {
	[TestFixture]
	public class ModSpottingManagerTests {
		private FileInfo testingFile;
		private ModLoggerStub logger;

		[SetUp]
		public void SetUpTest() {
			logger = new ModLoggerStub();
			testingFile = new FileInfo(Path.GetTempFileName());
		}
		
		[TearDown]
		public void CleanUpTest() {
			if(testingFile.Exists) testingFile.Delete();
			logger.AssertNothingLogged();
		}

		[Test]
		public void FirstTimeSeenSingleRun() {
			var spotter = PrepareSpotter(true, "one", "two");
			Assert.IsTrue(spotter.FirstTimeSeen("one"), "one");
			Assert.IsTrue(spotter.FirstTimeSeen("two"), "two");
			Assert.IsFalse(spotter.FirstTimeSeen("three"), "three");
		}

		[Test]
		public void FirstTimeSeenPersistence() {
			PrepareSpotter(true, "one", "two");

			var s = PrepareSpotter(false, "two", "three");
			Assert.IsFalse(s.FirstTimeSeen("one"), "one");
			Assert.IsFalse(s.FirstTimeSeen("two"), "two");
			Assert.IsTrue(s.FirstTimeSeen("three"), "three");
			Assert.IsFalse(s.FirstTimeSeen("four"), "four");
		}

		[Test]
		public void AnytimeSeenSingleRun() {
			var s = PrepareSpotter(true, "one", "two");
			Assert.IsTrue(s.AnytimeSeen("one"), "one");
			Assert.IsTrue(s.AnytimeSeen("two"), "two");
			Assert.IsFalse(s.AnytimeSeen("three"), "three");
		}

		[Test]
		public void AnytimeSeenPersistence() {
			PrepareSpotter(true, "one", "two");
            
			var s = PrepareSpotter(false, "two", "three");
			Assert.IsTrue(s.AnytimeSeen("one"), "one");
			Assert.IsTrue(s.AnytimeSeen("two"), "two");
			Assert.IsTrue(s.AnytimeSeen("three"), "three");
			Assert.IsFalse(s.AnytimeSeen("four"), "four");
		}

		[Test]
		public void SetFirstTimeSeen() {
			var s = PrepareSpotter(true, "one");
			s.SetFirstTimeSeen("two", true);
			Assert.IsTrue(s.FirstTimeSeen("two"), "first time after on");
			Assert.IsTrue(s.AnytimeSeen("two"), "anytime after on");

			s.SetFirstTimeSeen("two", false);
			Assert.IsFalse(s.FirstTimeSeen("two"), "first time after off");
			Assert.IsTrue(s.AnytimeSeen("two"), "anytime after off");
		}
		
		[Test]
		public void SetFirstTimeSeenNotPersisted() {
			var s = PrepareSpotter(true, "one");
			s.SetFirstTimeSeen("two", true);
			
			s = PrepareSpotter(false, "one");
			Assert.IsFalse(s.FirstTimeSeen("two"), "not seen first time");
			Assert.IsFalse(s.AnytimeSeen("two"), "not seen anytime");
		}
		
		[Test]
		public void InvalidDataFileNotOverwritten() {
			var packageIds = new[] {"one", "two"};
			File.WriteAllText(testingFile.Name, "INVALID");
			PrepareSpotter(true, packageIds);
			logger.AssertWarningsContain("Exception loading", $"Skipping {nameof(ModSpottingManager)} saving");

			var s = PrepareSpotter(false, packageIds);
			Assert.IsTrue(s.FirstTimeSeen("one"), "one");
			Assert.IsTrue(s.FirstTimeSeen("two"), "two");
			logger.Clear();
		}

		[Test]
		public void CaseInsensitivePackageIds() {
			var spotter = PrepareSpotter(true, "Author.oNe");
			Assert.IsTrue(spotter.FirstTimeSeen("Author.oNe"), "one same case");
			Assert.IsTrue(spotter.FirstTimeSeen("author.OnE"), "one different case");
			Assert.IsFalse(spotter.AnytimeSeen("Author.0ne"), "unrelated id not seen");
		}

		private ModSpottingManager PrepareSpotter(bool deleteFile, params string[] packageIds) {
			if(deleteFile && testingFile.Exists) testingFile.Delete();
			var spotter = new ModSpottingManager(testingFile.Name, logger);
			spotter.InspectPackageIds(packageIds);
			return spotter;
		}
	}
}