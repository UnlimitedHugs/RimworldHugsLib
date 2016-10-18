using UnityEngine;
using Verse;

namespace HugsLib {
	/**
	 * This is added as a component to the GameObject on scene to forward events to the controller.
	 * There is an issue here: when multiple versions of the library are loaded, Unity's AddComponent will 
	 * instantiate only the UnityProxyComponent from the latest version loaded, no matter which assembly the passed
	 * type actually belongs to. I guess the type is looked up by name internally, or something.
	 * To work around this we create a dynamic type using TypeBuilder that extends this class. The dynamic class
	 * is then fed to AddComponent.
	 * If you know a more elegant solution to this issue, please let me know.
	 */
	public class UnityProxyComponent : MonoBehaviour {
		public HugsLibController controllerInstance;

		public void Start() {
			controllerInstance = HugsLibController.Instance;
			if (controllerInstance != null) controllerInstance.Initalize();
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

		public void OnLevelWasLoaded(int level) {
			controllerInstance.OnLevelLoaded(level);
		}
	}
}