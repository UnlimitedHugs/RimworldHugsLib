using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using HugsLib.Core;
using HugsLib.Logs;
using HugsLib.News;
using HugsLib.Quickstart;
using HugsLib.Settings;
using HugsLib.Spotter;
using HugsLib.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using Verse;

[assembly:InternalsVisibleTo("HugsLibTests")]

namespace HugsLib {
	/// <summary>
	/// The hub of the library. Instantiates classes that extend ModBase and forwards some of the more useful events to them.
	/// The assembly version of the library should reflect the current major Rimworld version, i.e.: 0.18.0.0 for B18.
	/// This gives us the ability to release updates to the library without breaking compatibility with the mods that implement it.
	/// See Core.HugsLibMod for the entry point.
	/// </summary>
	public class HugsLibController {
		private const string SceneObjectName = "HugsLibProxy";
		private const string ModIdentifier = "HugsLib";
		private const string ModPackName = "HugsLib";
		private const string HarmonyInstanceIdentifier = "UnlimitedHugs.HugsLib";
		private const string HarmonyDebugCommandLineArg = "harmony_debug";

		private static bool earlyInitializationCompleted;
		private static bool lateInitializationCompleted;

		private static HugsLibController instance;

		public static HugsLibController Instance {
			get { return instance ?? (instance = new HugsLibController()); }
		}

		private static VersionFile libraryVersionFile;
		private static AssemblyVersionInfo libraryVersionInfo;
		public static Version LibraryVersion {
			get {
				if (libraryVersionInfo == null) ReadOwnVersion();
				if (libraryVersionFile != null && libraryVersionFile.OverrideVersion != null) 
					return libraryVersionFile.OverrideVersion;
				if (libraryVersionInfo != null) return libraryVersionInfo.HighestVersion;
				return typeof(HugsLibController).Assembly.GetName().Version;
			}
		}

		public static ModSettingsManager SettingsManager {
			get { return Instance.Settings; }
		}

		// most of the initialization happens during Verse.Mod instantiation. Pretty much no vanilla data is yet loaded at this point.
		internal static void EarlyInitialize() {
			try
			{
				if (earlyInitializationCompleted) {
					Logger.Warning("Attempted repeated early initialization of controller: " + Environment.StackTrace);
					return;
				}
				earlyInitializationCompleted = true;
				CreateSceneObject();
				Instance.InitializeController();
			} catch (Exception e) {
				Logger.Error("An exception occurred during early initialization: " + e);
			}
		}

		private static ModLogger _logger;
		internal static ModLogger Logger {
			get {
				return _logger ?? (_logger = new ModLogger(ModIdentifier));
			}
		}

		private static void CreateSceneObject() {
			// this must execute in the main thread
			LongEventHandler.ExecuteWhenFinished(() => {
				if (GameObject.Find(SceneObjectName) != null) {
					Logger.Error("Another version of the library is already loaded. The HugsLib assembly should be loaded as a standalone mod.");
					return;
				}
				var obj = new GameObject(SceneObjectName);
				UnityEngine.Object.DontDestroyOnLoad(obj);
				obj.AddComponent<UnityProxyComponent>();
			});
		}

		private static void ReadOwnVersion() {
			var ownAssembly = typeof(HugsLibController).Assembly;
			var ownContentPack = LoadedModManager.RunningMods
				.FirstOrDefault(p => p.assemblies != null && p.assemblies.loadedAssemblies.Contains(ownAssembly));
			if (ownContentPack != null) {
				libraryVersionFile = VersionFile.TryParseVersionFile(ownContentPack);
				libraryVersionInfo = AssemblyVersionInfo.ReadModAssembly(ownAssembly, ownContentPack);
			} else {
				Logger.Error("Failed to identify own ModContentPack");
			}
		}

		private readonly List<ModBase> childMods = new List<ModBase>();
		private readonly List<ModBase> earlyInitializedMods = new List<ModBase>();
		private readonly List<ModBase> initializedMods = new List<ModBase>();
		private readonly HashSet<Assembly> autoHarmonyPatchedAssemblies = new HashSet<Assembly>();
		private Dictionary<Assembly, ModContentPack> assemblyContentPacks;
		private bool initializationInProgress;

		public ModSettingsManager Settings { get; private set; }
		public UpdateFeatureManager UpdateFeatures { get; private set; }
		public TickDelayScheduler TickDelayScheduler { get; private set; }
		public DistributedTickScheduler DistributedTicker { get; private set; }
		public DoLaterScheduler DoLater { get; private set; }
		public LogPublisher LogUploader { get; private set; }
		public ModSpottingManager ModSpotter { get; private set; }

		internal Harmony HarmonyInst { get; private set; }

		private HugsLibController() {
		}

		// called during Verse.Mod instantiation
		private void InitializeController() {
			try {
				ReadOwnVersion();
				Logger.Message("version {0}", LibraryVersion);
				PrepareReflection();
				ApplyHarmonyPatches();
				Settings = new ModSettingsManager();
				Settings.BeforeModSettingsSaved += OnBeforeModSettingsSaved;
				UpdateFeatures = new UpdateFeatureManager();
				UpdateFeatures.OnEarlyInitialize();
				TickDelayScheduler = new TickDelayScheduler();
				DistributedTicker = new DistributedTickScheduler();
				DoLater = new DoLaterScheduler();
				LogUploader = new LogPublisher();
				var librarySettings = Settings.GetModSettings(ModIdentifier);
				QuickstartController.OnEarlyInitialize(librarySettings);
				ModSpotter = new ModSpottingManager();
				ModSpotter.OnEarlyInitialize();
				new LibraryVersionChecker(LibraryVersion, Logger).OnEarlyInitialize();
				LoadOrderChecker.ValidateLoadOrder();
				EnumerateModAssemblies();
				EarlyInitializeChildMods();
			} catch (Exception e) {
				Logger.ReportException(e);
			}
		}

		private void EarlyInitializeChildMods() {
			try {
				initializationInProgress = true;
				EnumerateChildMods(true);
				for (int i = 0; i < childMods.Count; i++) {
					var childMod = childMods[i];
					if (earlyInitializedMods.Contains(childMod)) continue;
					earlyInitializedMods.Add(childMod);
					var modId = childMod.LogIdentifierSafe;
					try {
						childMod.EarlyInitialize();
					} catch (Exception e) {
						Logger.ReportException(e, modId);
					}
				}
			} catch (Exception e) {
				Logger.ReportException(e);
			} finally {
				initializationInProgress = false;
			}
		}

		// called during static constructor initialization
		internal void LateInitialize() {
			try {
				if (!earlyInitializationCompleted) {
					Logger.Error("Attempted late initialization before early initialization: " + Environment.StackTrace);
					return;
				}
				if (lateInitializationCompleted) {
					Logger.Warning("Attempted repeated late initialization of controller: " + Environment.StackTrace);
					return;
				}
				lateInitializationCompleted = true;
				RegisterOwnSettings();
				QuickstartController.OnLateInitialize();
				LongEventHandler.ExecuteWhenFinished(HarmonyUtility.LogHarmonyPatchIssueErrors);
				LongEventHandler.QueueLongEvent(LoadReloadInitialize, "Initializing", true, null);
			} catch (Exception e) {
				Logger.Error("An exception occurred during late initialization: " + e);
			}
		}

		// executed both at startup and after a def reload
		internal void LoadReloadInitialize() {
			try {
				initializationInProgress = true; // prevent the Unity events from causing race conditions during async loading
				CheckForIncludedHugsLibAssembly();
				EnumerateModAssemblies();
				EnumerateChildMods(false);
				for (int i = 0; i < childMods.Count; i++) {
					var childMod = childMods[i];
					childMod.ModIsActive = assemblyContentPacks.ContainsKey(childMod.GetType().Assembly);
					if (initializedMods.Contains(childMod)) continue; // no need to reinitialize already loaded mods
					initializedMods.Add(childMod);
					var modId = childMod.LogIdentifierSafe;
					try {
						childMod.StaticInitialize();
						childMod.Initialize();
					} catch (Exception e) {
						Logger.ReportException(e, modId);
					}
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
				if (DoLater != null) DoLater.OnUpdate();
				for (int i = 0; i < initializedMods.Count; i++) {
					try {
						initializedMods[i].Update();
					} catch (Exception e) {
						Logger.ReportException(e, initializedMods[i].LogIdentifierSafe, true);
					}
				}
			} catch (Exception e) {
				Logger.ReportException(e, null, true);
			}
		}

		internal void OnTick() {
			if (initializationInProgress) return;
			try {
				DoLater.OnTick();
				var currentTick = Find.TickManager.TicksGame;
				for (int i = 0; i < initializedMods.Count; i++) {
					try {
						initializedMods[i].Tick(currentTick);
					} catch (Exception e) {
						Logger.ReportException(e, initializedMods[i].LogIdentifierSafe, true);
					}
				}
				TickDelayScheduler.Tick(currentTick);
				DistributedTicker.Tick(currentTick);
			} catch (Exception e) {
				Logger.ReportException(e, null, true);
			}
		}

		internal void OnFixedUpdate() {
			if (initializationInProgress) return;
			try {
				for (int i = 0; i < initializedMods.Count; i++) {
					try {
						initializedMods[i].FixedUpdate();
					} catch (Exception e) {
						Logger.ReportException(e, initializedMods[i].LogIdentifierSafe, true);
					}
				}
			} catch (Exception e) {
				Logger.ReportException(e, null, true);
			}
		}

		internal void OnGUI() {
			if (initializationInProgress) return;
			try {
				if (DoLater != null) DoLater.OnGUI();
				KeyBindingHandler.OnGUI();
				for (int i = 0; i < initializedMods.Count; i++) {
					try {
						initializedMods[i].OnGUI();
					} catch (Exception e) {
						Logger.ReportException(e, initializedMods[i].LogIdentifierSafe, true);
					}
				}
			} catch (Exception e) {
				Logger.ReportException(e, null, true);
			}
		}

		internal void OnGUIUnfiltered() {
			QuickstartController.OnGUIUnfiltered();
		}

		internal void OnSceneLoaded(Scene scene) {
			try {
				for (int i = 0; i < childMods.Count; i++) {
					try {
						childMods[i].SceneLoaded(scene);
					} catch (Exception e) {
						Logger.ReportException(e, childMods[i].LogIdentifierSafe);
					}
				}
			} catch (Exception e) {
				Logger.ReportException(e);
			}
		}

		internal void OnApplicationQuit() {
			try {
				for (int i = 0; i < childMods.Count; i++) {
					try {
						childMods[i].ApplicationQuit();
					} catch (Exception e) {
						Logger.ReportException(e, childMods[i].LogIdentifierSafe);
					}
				}
				Settings.SaveChanges();
			} catch (Exception e) {
				Logger.ReportException(e);
			}
		}

		internal void OnGameInitializationStart(Game game) {
			try {
				var currentTick = game.tickManager.TicksGame;
				TickDelayScheduler.Initialize(currentTick);
				DistributedTicker.Initialize(currentTick);
				game.tickManager.RegisterAllTickabilityFor(new HugsTickProxy {CreatedByController = true});
			} catch (Exception e) {
				Logger.ReportException(e);
			}
		}

		internal void OnPlayingStateEntered() {
			try {
				UtilityWorldObjectManager.OnWorldLoaded();
				for (int i = 0; i < childMods.Count; i++) {
					try {
						childMods[i].WorldLoaded();
					} catch (Exception e) {
						Logger.ReportException(e, childMods[i].LogIdentifierSafe);
					}
				}
			} catch (Exception e) {
				Logger.ReportException(e);
			}
		}

		internal void OnMapGenerated(Map map) {
			try {
				for (int i = 0; i < childMods.Count; i++) {
					try {
						childMods[i].MapGenerated(map);
					} catch (Exception e) {
						Logger.ReportException(e, childMods[i].LogIdentifierSafe);
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
						Logger.ReportException(e, childMods[i].LogIdentifierSafe);
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

		internal bool ShouldHarmonyAutoPatch(Assembly assembly, string modId) {
			if (autoHarmonyPatchedAssemblies.Contains(assembly)) {
				Logger.Warning("The {0} assembly contains multiple ModBase mods with HarmonyAutoPatch set to true. This warning was caused by modId {1}.", assembly.GetName().Name, modId);
				return false;
			} else {
				autoHarmonyPatchedAssemblies.Add(assembly);
				return true;
			}
		}

		private void OnMapLoaded(Map map) {
			try {
				DoLater.OnMapLoaded(map);
				for (int i = 0; i < childMods.Count; i++) {
					try {
						childMods[i].MapLoaded(map);
					} catch (Exception e) {
						Logger.ReportException(e, childMods[i].LogIdentifierSafe);
					}
				}
				// show update news dialog
				UpdateFeatures.TryShowDialog(false);
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
						Logger.ReportException(e, childMods[i].LogIdentifierSafe);
					}
				}
			} catch (Exception e) {
				Logger.ReportException(e);
			}
		}

		private void OnBeforeModSettingsSaved() {
			try {
				for (int i = 0; i < initializedMods.Count; i++) {
					try {
						var mod = initializedMods[i];
						if (mod.SettingsPackInternalAccess != null && mod.SettingsPackInternalAccess.HasUnsavedChanges) {
							initializedMods[i].SettingsChanged();
						}
					} catch (Exception e) {
						Logger.ReportException(e, initializedMods[i].LogIdentifierSafe);
					}
				}
			} catch (Exception e) {
				Logger.ReportException(e);
			}
		}

		private void OnDefsLoaded() {
			try {
				UtilityWorldObjectManager.OnDefsLoaded();
				for (int i = 0; i < childMods.Count; i++) {
					try {
						childMods[i].DefsLoaded();
					} catch (Exception e) {
						Logger.ReportException(e, childMods[i].LogIdentifierSafe);
					}
				}
			} catch (Exception e) {
				Logger.ReportException(e);
			}
		}

		// will run on startup and on reload. On reload it will add newly loaded mods
		private void EnumerateChildMods(bool earlyInitMode) {
			var modBaseDescendantsInLoadOrder = typeof(ModBase).InstantiableDescendantsAndSelf()
				.Select(t => new Pair<Type, ModContentPack>(t, assemblyContentPacks.TryGetValue(t.Assembly)))
				.Where(pair => pair.Second != null) // null pack => mod is disabled
				.OrderBy(pair => pair.Second.loadOrder).ToArray();

			var instantiatedThisRun = new List<string>();
			foreach (var pair in modBaseDescendantsInLoadOrder) {
				var subclass = pair.First;
				var pack = pair.Second;
				var hasEarlyInit = subclass.HasAttribute<EarlyInitAttribute>();
				if (hasEarlyInit != earlyInitMode) continue;
				if (childMods.Find(cm => cm.GetType() == subclass) != null) continue; // skip duplicate types present in multiple assemblies
				try {
					ModBase.CurrentlyProcessedContentPack = pack;
					var modbase = (ModBase)Activator.CreateInstance(subclass, true);
					ModBase.CurrentlyProcessedContentPack = null;
					modbase.ApplyHarmonyPatches();
					modbase.VersionInfo = AssemblyVersionInfo.ReadModAssembly(subclass.Assembly, pack);
					childMods.Add(modbase);
					instantiatedThisRun.Add(modbase.LogIdentifierSafe);
				} catch (Exception e) {
					Logger.ReportException(e, subclass.ToString(), false, "child mod instantiation");
				}
			}
			if (instantiatedThisRun.Count > 0) {
				var template = earlyInitMode ? "early-initializing {0}" : "initializing {0}";
				Logger.Message(template, instantiatedThisRun.ListElements());
			}
		}

		private void EnumerateModAssemblies() {
			assemblyContentPacks = new Dictionary<Assembly, ModContentPack>();
			foreach (var modContentPack in LoadedModManager.RunningMods) {
				foreach (var loadedAssembly in modContentPack.assemblies.loadedAssemblies) {
					assemblyContentPacks[loadedAssembly] = modContentPack;
				}
			}
		}

		// Ensure that no other mod has accidentally included the dll
		private void CheckForIncludedHugsLibAssembly() {
			var controllerTypeName = GetType().FullName;
			if(controllerTypeName == null) throw new NullReferenceException();
			foreach (var modContentPack in LoadedModManager.RunningMods) {
				foreach (var loadedAssembly in modContentPack.assemblies.loadedAssemblies) {
					if (loadedAssembly.GetType(controllerTypeName, false) != null && modContentPack.Name != ModPackName) {
						Logger.Error("Found HugsLib assembly included by mod {0}. The dll should never be included by other mods.", modContentPack.Name);
					}
				}
			}
		}

		private void ApplyHarmonyPatches() {
			try {
				if (ShouldHarmonyAutoPatch(typeof(HugsLibController).Assembly, ModIdentifier)) {
					Harmony.DEBUG = GenCommandLine.CommandLineArgPassed(HarmonyDebugCommandLineArg);
					HarmonyInst = new Harmony(HarmonyInstanceIdentifier);
					HarmonyInst.PatchAll(typeof(HugsLibController).Assembly);
				}
			} catch (Exception e) {
				Logger.ReportException(e);
			}
		}

		private void PrepareReflection() {
			InjectedDefHasher.PrepareReflection();
			LogWindowExtensions.PrepareReflection();
			OptionsDialogExtensions.PrepareReflection();
		}

		private void RegisterOwnSettings() {
			try {
				var pack = Settings.GetModSettings(ModIdentifier);
				pack.EntryName = assemblyContentPacks[Assembly.GetCallingAssembly()]?.Name ?? "HugsLib";
				UpdateFeatures.RegisterSettings(pack);
				LogPublisher.RegisterSettings(pack);
			} catch (Exception e) {
				Logger.ReportException(e);
			}
		}
	}
}