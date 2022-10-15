using System;
using System.IO;
using System.Linq;
using HugsLib.Settings;
using NUnit.Framework;

namespace HugsLibTests {
	[TestFixture]
	public class ModSettingsUnsavedChangesTests {
		private string tempFilePath;
		private ModLoggerStub logger;
		private ModSettingsPack[] savedModifiedPacks;
		private bool afterSavedCallbackInvoked;

		[SetUp]
		public void SetUp() {
			tempFilePath = Path.GetTempFileName();
			File.Delete(tempFilePath);
			logger = new ModLoggerStub();
			savedModifiedPacks = Array.Empty<ModSettingsPack>();
			afterSavedCallbackInvoked = false;
		}

		[TearDown]
		public void TearDown() {
			try {
				logger.AssertNothingLogged();
			} finally {
				if (File.Exists(tempFilePath)) File.Delete(tempFilePath);
			}
		}

		[Test]
		public void NotUnsavedWhenJustCreated() {
			var handle = GetHandle();
			Assert.IsFalse(handle.ParentPack.ParentManager.HasUnsavedChanges);
		}

		[Test]
		public void NotUnsavedWhenLoaded() {
			var handle = GetHandle();
			handle.Value = 1;
			handle.ParentPack.SaveChanges();
			handle = GetHandle();
			Assert.That(handle.Value, Is.EqualTo(1));
			Assert.IsFalse(handle.ParentPack.ParentManager.HasUnsavedChanges, "unsaved after load");
		}

		[Test]
		public void ControllerNotSavingWithoutChanges() {
			var manager = GetManager();
			manager.SaveChanges();
			Assert.IsFalse(afterSavedCallbackInvoked);
		}

		[Test]
		public void UnsavedFlagging() {
			var handle = GetHandle();
			AssertHierarchyUnsavedChanges(handle, false, "before");
			handle.Value += 1;
			AssertHierarchyUnsavedChanges(handle, true, "after");
		}

		[Test]
		public void ClearUnsavedAfterSaveChanges() {
			var handle = GetHandle();
			var pack = handle.ParentPack;
			var manager = pack.ParentManager;
			handle.Value++;
			manager.SaveChanges();
			AssertHierarchyUnsavedChanges(handle, false, "after save");
		}

		[Test]
		public void CorrectPackReportedUnsaved() {
			var manager = GetManager();
			var pack1 = manager.GetModSettings("pack1");
			var handle1 = pack1.GetHandle<int>("testHandle", null, null);
			var pack2 = manager.GetModSettings("pack2");
			var handle2 = pack2.GetHandle<int>("testHandle", null, null);

			handle1.Value++;
			manager.SaveChanges();
			AssertSavedModifiedPacks(pack1);
			
			savedModifiedPacks = Array.Empty<ModSettingsPack>();

			handle2.Value++;
			manager.SaveChanges();
			AssertSavedModifiedPacks(pack2);
		}

		[Test]
		public void DefaultValuesSetUnsavedFlag() {
			var handle = GetHandle();
			handle.Value = 1;
			handle.ParentPack.SaveChanges();
			AssertHierarchyUnsavedChanges(handle, false, "before");
			handle.Value = default;
			AssertHierarchyUnsavedChanges(handle, true, "after");
		}

		private ModSettingsManager GetManager() {
			var manager = new ModSettingsManager(tempFilePath, logger);
			manager.BeforeModSettingsSaved += () =>
				savedModifiedPacks = manager.ModSettingsPacks.Where(p => p.HasUnsavedChanges).ToArray();
			manager.AfterModSettingsSaved += () => afterSavedCallbackInvoked = true;
			return manager;
		}

		private ModSettingsPack GetPack() {
			return GetManager().GetModSettings("testMod");
		}

		private SettingHandle<int> GetHandle() {
			return GetPack().GetHandle("testHandle", null, null, 0);
		}

		private void AssertSavedModifiedPacks(params ModSettingsPack[] packs) {
			Assert.That(savedModifiedPacks, Is.EquivalentTo(packs));
		}

		private void AssertHierarchyUnsavedChanges(SettingHandle handle, bool modified, string message) {
			Assert.That(handle.ParentPack.ParentManager.HasUnsavedChanges, Is.EqualTo(modified), "manager unsaved " + message);
			Assert.That(handle.ParentPack.HasUnsavedChanges, Is.EqualTo(modified), "pack unsaved " + message);
			Assert.That(handle.HasUnsavedChanges, Is.EqualTo(modified), "handle unsaved " + message);
		}
	}
}