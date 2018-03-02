using System;
using System.Collections.Generic;
using Verse;

namespace HugsLib.Utils {
	/// <summary>
	/// A way to schedule single-use callbacks for an upcoming event.
	/// Useful to break the stack and ensure code is run in the main thread.
	/// Access via HugsLibController.Instance.DoLater
	/// </summary>
	public class DoLaterScheduler {
		private readonly Queue<Action> nextTick = new Queue<Action>(0);
		private readonly Queue<Action> nextUpdate = new Queue<Action>(0);
		private readonly Queue<Action> nextOnGUI = new Queue<Action>(0);
		private readonly Queue<Action<Map>> nextMapLoaded = new Queue<Action<Map>>(0);

		internal DoLaterScheduler() {
		}

		/// <summary>
		/// Schedule a callback to be executed at the start of the next tick
		/// </summary>
		public void DoNextTick(Action action) {
			nextTick.Enqueue(action);
		}
		
		/// <summary>
		/// Schedule a callback to be executed at the start of the next frame
		/// </summary>
		public void DoNextUpdate(Action action) {
			nextUpdate.Enqueue(action);
		}

		/// <summary>
		/// Schedule a callback to be executed at the start of the next OnGUI
		/// </summary>
		public void DoNextOnGUI(Action action) {
			nextOnGUI.Enqueue(action);
		}
		
		/// <summary>
		/// Schedule a callback to be executed the next time a map has finished loading
		/// </summary>
		/// <param name="action">The callback receives the map that has finished loading</param>
		public void DoNextMapLoaded(Action<Map> action) {
			nextMapLoaded.Enqueue(action);
		}


		internal void OnTick() {
			if(nextTick.Count>0) InvokeCallbacks(nextTick);
		}

		internal void OnUpdate() {
			if (nextUpdate.Count > 0) InvokeCallbacks(nextUpdate);
		}

		internal void OnGUI() {
			if (nextOnGUI.Count > 0) InvokeCallbacks(nextOnGUI);
		}

		internal void OnMapLoaded(Map map) {
			while (nextMapLoaded.Count > 0) {
				var callback = nextMapLoaded.Dequeue();
				try {
					callback(map);
				} catch (Exception e) {
					HugsLibUtility.BlameCallbackException("DoLaterScheduler", callback, e);
				}
			}
		}

		private void InvokeCallbacks(Queue<Action> queue) {
			while (queue.Count>0) {
				var callback = queue.Dequeue();
				try {
					callback();
				} catch (Exception e) {
					HugsLibUtility.BlameCallbackException("DoLaterScheduler", callback, e);
				}
			}
		}
	}
}