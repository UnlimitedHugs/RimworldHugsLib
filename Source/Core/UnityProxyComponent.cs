using UnityEngine;
using UnityEngine.SceneManagement;
using Verse;

namespace HugsLib.Core {
	/**
	 * This is added as a component to the GameObject on scene to forward events to the controller.
	 */
	public class UnityProxyComponent : MonoBehaviour {
		public HugsLibController controllerInstance;

		public void Start() {
			controllerInstance = HugsLibController.Instance;
			if (controllerInstance != null) controllerInstance.Initalize();
		}

		public void OnEnable() {
			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		public void OnDisable() {
			SceneManager.sceneLoaded -= OnSceneLoaded;
		}

		public void Update() {
			controllerInstance.OnUpdate();
		}

		public void FixedUpdate() {
			controllerInstance.OnFixedUpdate();
		}

		public void OnGUI() {
			controllerInstance.OnGUI();
		}

		public void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode) {
			controllerInstance.OnSceneLoaded(scene);
		}
	}
}