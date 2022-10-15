﻿using UnityEngine;
using UnityEngine.SceneManagement;

namespace HugsLib.Core {
	/// <summary>
	/// This is added as a component to the GameObject on scene to forward events to the controller.
	/// </summary>
	public class UnityProxyComponent : MonoBehaviour {
		public HugsLibController controllerInstance = null!;

		public void Start() {
			controllerInstance = HugsLibController.Instance;
		}

		public void OnEnable() {
			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		public void OnDisable() {
			SceneManager.sceneLoaded -= OnSceneLoaded;
		}

		public void FixedUpdate() {
			controllerInstance.OnFixedUpdate();
		}

		private void OnApplicationQuit() {
			controllerInstance.OnApplicationQuit();
		}

		public void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode) {
			controllerInstance.OnSceneLoaded(scene);
		}
	}
}