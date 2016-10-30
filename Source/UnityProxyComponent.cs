using UnityEngine;

namespace HugsLib {
	/**
	 * This is added as a component to the GameObject on scene to forward events to the controller.
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