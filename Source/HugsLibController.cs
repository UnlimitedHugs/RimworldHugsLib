using System;
using System.Collections.Generic;
using System.Reflection;
using HugsLib.Settings;
using RimWorld;
using UnityEngine;
using Verse;

namespace HugsLib {
	[StaticConstructorOnStartup]
	/**
	 * The hub of the library. Instantiates classes that extend ModBase and forwards some of the most useful events to them.
	 * The minor version of the assembly should reflect the current major Rimworld version, just like CCL.
	 * This gives us the ability to release patches to the library without breaking compatibility with the mods that implement it.
	 */
	public class HugsLibController {
		private const string SceneObjectName = "HugsLibProxy";
		private const string ModIdentifier = "HugsLib";
		private const int MapLevelIndex = 1;

		private static HugsLibController instance;
		public static HugsLibController Instance {
			get { return instance ?? (instance = new HugsLibController()); }
		}

		public static AssemblyName AssemblyName {
			get { return typeof(HugsLibController).Assembly.GetName(); }
		}

		public static VersionShort AssemblyVersion {
			get { return AssemblyName.Version; }
		}

		public static ModSettingsManager SettingsManager {
			get { return Instance.Settings; }
		}

		// entry point
		static HugsLibController() {
			Logger = new ModLogger(ModIdentifier);
			CreateSceneObject();
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
		
		private readonly List<ModBase> childMods = new List<ModBase>();
		private readonly List<ModBase> initializedMods = new List<ModBase>(); 
		private Dictionary<Assembly, ModContentPack> assemblyContentPacks;
		private DefReloadWatcher reloadWatcher;
		private WindowReplacer<Dialog_Options, Dialog_OptionsExtended> optionsReplacer;
		private LanguageStringInjector languageInjector;
		private SettingHandle<bool> updateNewsSetting;
		private bool mapLoadedPending = true;

		public ModSettingsManager Settings { get; private set; }
		public UpdateFeatureManager UpdateFeatures { get; private set; }
		public CallbackScheduler CallbackScheduler { get; private set; } // initalized before MapLoaded
		public DistributedTickScheduler DistributedTicker { get; private set; }  // initalized before MapLoaded
		
		private HugsLibController() {
		}

		internal void Initalize() {
			if (Settings != null) return; // double initialization safeguard, shouldn't happen
			try {
				Settings = new ModSettingsManager(OnSettingsChanged);
				UpdateFeatures = new UpdateFeatureManager();
				CallbackScheduler = new CallbackScheduler();
				DistributedTicker = new DistributedTickScheduler();
				reloadWatcher = new DefReloadWatcher(OnDefReloadDetected);
				optionsReplacer = new WindowReplacer<Dialog_Options, Dialog_OptionsExtended>();
				languageInjector = new LanguageStringInjector();
				RegisterOwnSettings();
				LoadReloadInitialize();
			} catch (Exception e) {
				Logger.ReportException(e);
			}
		}

		internal void OnUpdate() {
			try {
				reloadWatcher.Update();
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
			try {
				if (mapLoadedPending && Current.ProgramState == ProgramState.MapPlaying) {
					mapLoadedPending = false;
					// Make sure we execute after MapDrawer.RegenerateEverythingNow
					LongEventHandler.ExecuteWhenFinished(OnMapLoaded);
				}
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
			try {
				optionsReplacer.OnGUI();
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

		internal void OnLevelLoaded(int level) {
			try {
				for (int i = 0; i < childMods.Count; i++) {
					try {
						childMods[i].LevelLoaded(level);
					} catch (Exception e) {
						Logger.ReportException(e, childMods[i].ModIdentifier);
					}
				}
				if (level != MapLevelIndex) return;
				for (int i = 0; i < childMods.Count; i++) {
					try {
						childMods[i].MapLoading();
					} catch (Exception e) {
						Logger.ReportException(e, childMods[i].ModIdentifier);
					}
				}
			} catch (Exception e) {
				Logger.ReportException(e);
			}
		}

		internal void OnMapComponentsIntializing() {
			try {
				mapLoadedPending = true;
				var currentTick = Find.TickManager.TicksGame;
				CallbackScheduler.Initialize(currentTick);
				DistributedTicker.Initialize(currentTick);
				Current.Game.tickManager.RegisterAllTickabilityFor(new HugsTickProxy{CreatedByController = true});
				for (int i = 0; i < childMods.Count; i++) {
					try {
						childMods[i].MapComponentsInitializing();
					} catch (Exception e) {
						Logger.ReportException(e, childMods[i].ModIdentifier);
					}
				}
			} catch (Exception e) {
				Logger.ReportException(e);
			}
		}

		private void OnMapLoaded(){
			try {
				for (int i = 0; i < childMods.Count; i++) {
					try {
						childMods[i].MapLoaded();
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
				RegisterOwnSettings();
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

		private void OnDefReloadDetected() {
			LoadReloadInitialize();
		}
		
		// executed both at startup and after a def reload
		private void LoadReloadInitialize() {
			try {
				EnumerateModAssemblies();
				EnumerateChildMods();
				languageInjector.InjectEmbeddedStrings();
				var initializationsThisRun = new List<string>();
				for (int i = 0; i < childMods.Count; i++) {
					var childMod = childMods[i];
					childMod.ModIsActive = assemblyContentPacks.ContainsKey(childMod.GetType().Assembly);
					if(initializedMods.Contains(childMod)) continue; // no need to reinitialize already loaded mods
					initializedMods.Add(childMod);
					var modId = childMod.ModIdentifier;
					try {
						childMod.Initalize();
					} catch (Exception e) {
						Logger.ReportException(e, modId);
					}
					initializationsThisRun.Add(modId);
				}
				if (initializationsThisRun.Count > 0) {
					Logger.Message("v{0} initialized {1}", AssemblyVersion, initializationsThisRun.ListElements());
				}
				OnDefsLoaded();
			} catch (Exception e) {
				Logger.ReportException(e);
			}
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
				if (modbase != null) UpdateFeatures.InspectActiveMod(modbase.ModIdentifier, subclass.Assembly.GetName().Version);
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
						Find.WindowStack.Add(new Dialog_Message("HugsLib_setting_allNews_fail".Translate()));
					}
				}
				return false;
			};
		}
	}
}