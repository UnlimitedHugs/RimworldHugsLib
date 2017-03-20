using System;
using System.Collections.Generic;
using System.Reflection;
using Harmony;
using HugsLib.Core;
using HugsLib.Logs;
using HugsLib.News;
using HugsLib.Restarter;
using HugsLib.Settings;
using HugsLib.Source.Attrib;
using HugsLib.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using Verse;

namespace HugsLib {
	/// <summary>
	/// The hub of the library. Instantiates classes that extend ModBase and forwards some of the more useful events to them.
	/// The minor version of the assembly should reflect the current major Rimworld version, just like CCL.
	/// This gives us the ability to release updates to the library without breaking compatibility with the mods that implement it.
	/// </summary>
	[StaticConstructorOnStartup]
	public class HugsLibController {
		private const string SceneObjectName = "HugsLibProxy";
		private const string ModIdentifier = "HugsLib";
		private const string HarmonyInstanceIdentifier = "UnlimitedHugs.HugsLib";

		private static HugsLibController instance;
		public static HugsLibController Instance {
			get { return instance ?? (instance = new HugsLibController()); }
		}

		private static VersionFile libraryVersionFile;
		public static VersionShort LibraryVersion {
			get {
				if (libraryVersionFile == null) libraryVersionFile = ReadOwnVersionFile();
				return libraryVersionFile!=null ? libraryVersionFile.OverrideVersion : new VersionShort();
			}
		}

		public static ModSettingsManager SettingsManager {
			get { return Instance.Settings; }
		}

		// entry point
		static HugsLibController() {
			Logger = new ModLogger(ModIdentifier);
			CreateSceneObject();
			Instance.InitializeController();
		}

		internal static ModLogger Logger { get; private set; }

		private static void CreateSceneObject() {
			if (GameObject.Find(SceneObjectName) != null) {
				Logger.Error("Another version of the library is already loaded. The HugsLib assembly should be loaded as a standalone mod.");
				return;
			}
			var obj = new GameObject(SceneObjectName);
			GameObject.DontDestroyOnLoad(obj);
			obj.AddComponent<UnityProxyComponent>();
		}

		private static VersionFile ReadOwnVersionFile() {
			VersionFile file = null;
			var ownAssembly = typeof(HugsLibController).Assembly;
			foreach (var modContentPack in LoadedModManager.RunningMods) {
				foreach (var loadedAssembly in modContentPack.assemblies.loadedAssemblies) {
					if (!loadedAssembly.Equals(ownAssembly)) continue;
					file = VersionFile.TryParseVersionFile(modContentPack);
					if (file == null) Logger.Error("Missing Version.xml file");
				}
			}
			return file;
		}

		private readonly List<ModBase> childMods = new List<ModBase>();
		private readonly List<ModBase> initializedMods = new List<ModBase>();
		private Dictionary<Assembly, ModContentPack> assemblyContentPacks;
		private HarmonyInstance harmonyInstance;
		private SettingHandle<bool> updateNewsSetting;
		private bool initializationInProgress;

		public ModSettingsManager Settings { get; private set; }
		public UpdateFeatureManager UpdateFeatures { get; private set; }
		public CallbackScheduler CallbackScheduler { get; private set; }
		public DistributedTickScheduler DistributedTicker { get; private set; }
		public LogPublisher LogUploader { get; private set; }

		internal AutoRestarter AutoRestarter { get; private set; }
		
		private HugsLibController() {
		}

		private void InitializeController() {
			try {
				PrepareReflection();
				ApplyHarmonyPatches();
				Settings = new ModSettingsManager(OnSettingsChanged);
				UpdateFeatures = new UpdateFeatureManager();
				CallbackScheduler = new CallbackScheduler();
				DistributedTicker = new DistributedTickScheduler();
				LogUploader = new LogPublisher();
				AutoRestarter = new AutoRestarter();
				RegisterOwnSettings();
				ReadOwnVersionFile();
				LongEventHandler.QueueLongEvent(LoadReloadInitialize, "Initializing", true, null);
			} catch (Exception e) {
				Logger.ReportException(e);
			}
		}

		// executed both at startup and after a def reload
		internal void LoadReloadInitialize() {
			try {
				initializationInProgress = true; // prevent the Unity events from causing race conditions during async loading
				EnumerateModAssemblies();
				ProcessAttibutes(); // do detours and other attribute work for (newly) loaded mods
				EnumerateChildMods();
				var initializationsThisRun = new List<string>();
				for (int i = 0; i < childMods.Count; i++) {
					var childMod = childMods[i];
					childMod.ModIsActive = assemblyContentPacks.ContainsKey(childMod.GetType().Assembly);
					if (initializedMods.Contains(childMod)) continue; // no need to reinitialize already loaded mods
					initializedMods.Add(childMod);
					var modId = childMod.ModIdentifier;
					try {
						childMod.Initialize();
					} catch (Exception e) {
						Logger.ReportException(e, modId);
					}
					initializationsThisRun.Add(modId);
				}
				if (initializationsThisRun.Count > 0) {
					Logger.Message("v{0} initialized {1}", LibraryVersion, initializationsThisRun.ListElements());
				}
				OnDefsLoaded();
			} catch (Exception e) {
				Logger.ReportException(e);
			} finally {
				initializationInProgress = false;
			}
		}

		internal void OnUpdate() {
			if (initializationInProgress) return;
			try {
				for (int i = 0; i < childMods.Count; i++) {
					try {
						childMods[i].Update();
					} catch (Exception e) {
						Logger.ReportException(e, childMods[i].ModIdentifier, true);
					}
				}
			} catch (Exception e) {
				Logger.ReportException(e, null, true);
			}
		}

		public void OnTick() {
			if (initializationInProgress) return;
			try {
				var currentTick = Find.TickManager.TicksGame;
				for (int i = 0; i < childMods.Count; i++) {
					try {
						childMods[i].Tick(currentTick);
					} catch (Exception e) {
						Logger.ReportException(e, childMods[i].ModIdentifier, true);
					}
				}
				CallbackScheduler.Tick(currentTick);
				DistributedTicker.Tick(currentTick);
			} catch (Exception e) {
				Logger.ReportException(e, null, true);
			}
		}

		internal void OnFixedUpdate() {
			if (initializationInProgress) return;
			try {
				for (int i = 0; i < childMods.Count; i++) {
					try {
						childMods[i].FixedUpdate();
					} catch (Exception e) {
						Logger.ReportException(e, childMods[i].ModIdentifier, true);
					}
				}
			} catch (Exception e) {
				Logger.ReportException(e, null, true);
			}
		}

		internal void OnGUI() {
			if (initializationInProgress) return;
			try {
				KeyBindingHandler.OnGUI();
				for (int i = 0; i < childMods.Count; i++) {
					try {
						childMods[i].OnGUI();
					} catch (Exception e) {
						Logger.ReportException(e, childMods[i].ModIdentifier, true);
					}
				}
			} catch (Exception e) {
				Logger.ReportException(e, null, true);
			}
		}

		internal void OnSceneLoaded(Scene scene) {
			try {
				for (int i = 0; i < childMods.Count; i++) {
					try {
						childMods[i].SceneLoaded(scene);
					} catch (Exception e) {
						Logger.ReportException(e, childMods[i].ModIdentifier);
					}
				}
			} catch (Exception e) {
				Logger.ReportException(e);
			}
		}

		internal void OnPlayingStateEntered() {
			try {
				var currentTick = Find.TickManager.TicksGame;
				CallbackScheduler.Initialize(currentTick);
				DistributedTicker.Initialize(currentTick);
				UtilityWorldObjectManager.OnWorldLoaded();
				Current.Game.tickManager.RegisterAllTickabilityFor(new HugsTickProxy { CreatedByController = true });
				for (int i = 0; i < childMods.Count; i++) {
					try {
						childMods[i].WorldLoaded();
					} catch (Exception e) {
						Logger.ReportException(e, childMods[i].ModIdentifier);
					}
				}
			} catch (Exception e) {
				Logger.ReportException(e);
			}
		}

		internal void OnMapComponentsConstructed(Map map) {
			try {
				for (int i = 0; i < childMods.Count; i++) {
					try {
						childMods[i].MapComponentsInitializing(map);
					} catch (Exception e) {
						Logger.ReportException(e, childMods[i].ModIdentifier);
					}
				}
			} catch (Exception e) {
				Logger.ReportException(e);
			}
		}

		internal void OnMapInitFinalized(Map map) {
			// Make sure we execute OnMapLoaded after MapDrawer.RegenerateEverythingNow
			LongEventHandler.QueueLongEvent(() => OnMapLoaded(map), null, false, null);
		}

		private void OnMapLoaded(Map map){
			try {
				for (int i = 0; i < childMods.Count; i++) {
					try {
						childMods[i].MapLoaded(map);
					} catch (Exception e) {
						Logger.ReportException(e, childMods[i].ModIdentifier);
					}
				}
				// show update news dialog
				if (updateNewsSetting.Value) {
					UpdateFeatures.TryShowDialog();
				}
			} catch (Exception e) {
				Logger.ReportException(e);
			}
		}

		internal void OnMapDiscarded(Map map) {
			try {
				for (int i = 0; i < childMods.Count; i++) {
					try {
						childMods[i].MapDiscarded(map);
					} catch (Exception e) {
						Logger.ReportException(e, childMods[i].ModIdentifier);
					}
				}
			} catch (Exception e) {
				Logger.ReportException(e);
			}
		}

		private void OnSettingsChanged() {
			try {
				for (int i = 0; i < childMods.Count; i++) {
					try {
						childMods[i].SettingsChanged();
					} catch (Exception e) {
						Logger.ReportException(e, childMods[i].ModIdentifier);
					}
				}
			} catch (Exception e) {
				Logger.ReportException(e);
			}
		}

		private void OnDefsLoaded() {
			try {
				ShortHashCollisionResolver.ResolveCollisions();
				RegisterOwnSettings();
				UtilityWorldObjectManager.OnDefsLoaded();
				for (int i = 0; i < childMods.Count; i++) {
					try {
						childMods[i].DefsLoaded();
					} catch (Exception e) {
						Logger.ReportException(e, childMods[i].ModIdentifier);
					}
				}
			} catch (Exception e) {
				Logger.ReportException(e);
			}
		}

		private void ProcessAttibutes() {
			AttributeDetector.ProcessNewTypes();
		}

		// will run on startup and on reload. On reload it will add newly loaded mods
		private void EnumerateChildMods() {
			foreach (var subclass in typeof (ModBase).InstantiableDescendantsAndSelf()) {
				if (childMods.Find(cm => cm.GetType() == subclass) != null) continue; // skip duplicate types present in multiple assemblies
				ModContentPack pack;
				assemblyContentPacks.TryGetValue(subclass.Assembly, out pack);
				if (pack == null) continue; // mod is disabled
				ModBase modbase = null;
				try {
					modbase = (ModBase) Activator.CreateInstance(subclass, true);
					modbase.ModContentPack = pack;
					if (childMods.Find(m => m.ModIdentifier == modbase.ModIdentifier) != null) {
						Logger.Error("Duplicate mod identifier: " + modbase.ModIdentifier);
						continue;
					}
					childMods.Add(modbase);
				} catch (Exception e) {
					Logger.ReportException(e, subclass.ToString(), false, "child mod instantiation");
				}
				if (modbase != null) UpdateFeatures.InspectActiveMod(modbase.ModIdentifier, modbase.GetVersion());
			}
			// sort by load order
			childMods.Sort((cm1, cm2) => cm1.ModContentPack.loadOrder.CompareTo(cm2.ModContentPack.loadOrder));
		}

		private void EnumerateModAssemblies() {
			assemblyContentPacks = new Dictionary<Assembly, ModContentPack>();
			foreach (var modContentPack in LoadedModManager.RunningMods) {
				foreach (var loadedAssembly in modContentPack.assemblies.loadedAssemblies) {
					assemblyContentPacks[loadedAssembly] = modContentPack;
				}
			}
		}

		private void ApplyHarmonyPatches() {
			try {
				harmonyInstance = HarmonyInstance.Create(HarmonyInstanceIdentifier);
				harmonyInstance.PatchAll(typeof (HugsLibController).Assembly);
			} catch (Exception e) {
				Logger.ReportException(e);
			}
		}

		private void PrepareReflection() {
			InjectedDefHasher.PrepareReflection();
			LogWindowInjection.PrepareReflection();
		}

		private void RegisterOwnSettings() {
			var pack = Settings.GetModSettings(ModIdentifier);
			pack.EntryName = "HugsLib_ownSettingsName".Translate();
			pack.DisplayPriority = ModSettingsPack.ListPriority.Lower;
			updateNewsSetting = pack.GetHandle("modUpdateNews", "HugsLib_setting_showNews_label".Translate(), "HugsLib_setting_showNews_desc".Translate(), true);
			var allNewsHandle = pack.GetHandle("showAllNews", "HugsLib_setting_allNews_label".Translate(), "HugsLib_setting_allNews_desc".Translate(), false);
			allNewsHandle.Unsaved = true;
			allNewsHandle.CustomDrawer = rect => {
				if (Widgets.ButtonText(rect, "HugsLib_setting_allNews_button".Translate())) {
					if (!UpdateFeatures.TryShowDialog(true)) {
						Find.WindowStack.Add(new Dialog_MessageBox("HugsLib_setting_allNews_fail".Translate()));
					}
				}
				return false;
			};
			AutoRestarter.CreateSettingsHandles(pack);
		}
	}
}