using System;
using System.Collections.Generic;

namespace HugsLib {
	/**
	* A performance-friendly way to execute code at arbitrary tick intervals.
	* Optimized for one-off timed callbacks with variable callback delay. 
	* Use DistributedTickScheduler instead if you have many recepients with recurring callbacks & constant time.
	*/
	public class CallbackScheduler {
		private readonly LinkedList<SchedulerEntry> entries = new LinkedList<SchedulerEntry>();
		private int lastProcessedTick = -1;

		internal CallbackScheduler() {
		}

		public void Initialize(int currentTick) {
			lastProcessedTick = currentTick;
			entries.Clear();
		}

		public void Tick(int currentTick) {

			if (lastProcessedTick < 0) throw new Exception("Ticking not initalized CallbackScheduler");
			lastProcessedTick = currentTick;
			while (entries.First != null) {
				var entry = entries.First.Value;
				if (entry.dueAtTick > currentTick) return;
				entries.RemoveFirst();
				if (entry.repeat) {
					entry.dueAtTick = currentTick + entry.interval;
					ScheduleEntry(entry);
				}
				DoCallback(entry.callback);
			}
		}

		// It's recommended to pass member function rather than anonymous functions, since it's easier to find the culprit when an exception occurs.
		public void ScheduleCallback(Action callback, int dueInTicks, bool repeat = false) {
			if (lastProcessedTick < 0) throw new Exception("Adding callback to not initialized CallbackScheduler");
			if (callback == null) throw new NullReferenceException("callback cannot be null");
			if (dueInTicks < 0) throw new Exception("invalid dueInTicks value: " + dueInTicks);
			if (dueInTicks == 0) {
				DoCallback(callback);
			} else {
				var entry = new SchedulerEntry(callback, dueInTicks, lastProcessedTick + dueInTicks, repeat);
				ScheduleEntry(entry);
			}
		}

		public void TryUnscheduleCallback(Action callback) {
			var entry = entries.First;
			while (entry != null) {
				if (entry.Value.callback == callback) {
					entries.Remove(entry);
					return;
				}
				entry = entry.Next;
			}
		}

		private void DoCallback(Action callback) {
			try {
				callback();
			} catch (Exception e) {
				HugsLibUtility.BlameCallbackException("CallbackScheduler", callback, e);
			}
		}

		// inserts the new entry, maintaining the list sorted in ascending order
		private void ScheduleEntry(SchedulerEntry newEntry) {
			// iterate tail-first for best performance when reinserting
			var lastEntry = entries.Last;
			while (lastEntry != null) {
				if (lastEntry.Value.dueAtTick <= newEntry.dueAtTick) {
					entries.AddAfter(lastEntry, newEntry);
					return;
				}
				lastEntry = lastEntry.Previous;
			}
			// on empty list
			entries.AddFirst(newEntry);
		}

		private class SchedulerEntry {
			public readonly Action callback;
			public readonly int interval;
			public readonly bool repeat;
			public int dueAtTick;

			public SchedulerEntry(Action callback, int interval, int dueAtTick, bool repeat) {
				this.callback = callback;
				this.interval = interval;
				this.repeat = repeat;
				this.dueAtTick = dueAtTick;
			}
		}
	}
}