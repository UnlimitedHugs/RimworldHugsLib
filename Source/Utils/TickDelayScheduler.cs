using System;
using System.Collections.Generic;
using Verse;

namespace HugsLib.Utils {
	/// <summary>
	/// A performance-friendly way to execute code at arbitrary tick intervals.
	/// Optimized for one-off timed callbacks with variable callback delay. 
	/// Use DistributedTickScheduler instead if you have many recipients with recurring callbacks & constant time.
	/// Callbacks are called at tick time, which means a game must be loaded and running for them to be processed.
	/// </summary>
	public class TickDelayScheduler {
		private readonly LinkedList<SchedulerEntry> entries = new LinkedList<SchedulerEntry>();
		private int lastProcessedTick = -1;

		internal TickDelayScheduler() {
		}

		internal void Initialize(int currentTick) {
			lastProcessedTick = currentTick;
			entries.Clear();
		}

		internal void Tick(int currentTick) {
			if (lastProcessedTick < 0) throw new Exception("Ticking not initialized TickDelayScheduler");
			lastProcessedTick = currentTick;
			while (entries.First != null) {
				var entry = entries.First.Value;
				if (entry.dueAtTick > currentTick) return;
				entries.RemoveFirst();
				var allowReschedule = DoCallback(entry);
				if (entry.repeat && allowReschedule) {
					entry.dueAtTick = currentTick + entry.interval;
					ScheduleEntry(entry);
				}
			}
		}

		/// <summary>
		/// Registers a delegate to be called in a given number of ticks.
		/// </summary>
		/// <param name="callback">The delegate to be called</param>
		/// <param name="dueInTicks">The delay in ticks before the delegate is called</param>
		/// <param name="owner">Optional owner of the delegate. Callback will not fire if the Thing is not spawned at call time.</param>
		/// <param name="repeat">If true, the callback will be rescheduled after each call until manually unscheduled</param>
		public void ScheduleCallback(Action callback, int dueInTicks, Thing owner = null, bool repeat = false) {
			if (lastProcessedTick < 0) throw new Exception("Adding callback to not initialized TickDelayScheduler");
			if (callback == null) throw new NullReferenceException("callback cannot be null");
			if (dueInTicks < 0) throw new Exception("invalid dueInTicks value: " + dueInTicks);
			if (dueInTicks == 0 && repeat) throw new Exception("Cannot schedule repeating callback with 0 delay");
			var entry = new SchedulerEntry(callback, dueInTicks, owner, lastProcessedTick + dueInTicks, repeat);
			if (dueInTicks == 0) {
				DoCallback(entry);
			} else {
				ScheduleEntry(entry);	
			}
		}

		/// <summary>
		/// Manually remove a callback to abort a delay or clear a recurring callback.
		/// Silently fails if the callback is not found.
		/// </summary>
		/// <param name="callback">The scheduled callback</param>
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

		/// <summary>
		/// Only for debug purposes
		/// </summary>
		public IEnumerable<SchedulerEntry> GetAllPendingCallbacks() {
			return entries;
		}

		private bool DoCallback(SchedulerEntry entry) {
			if (entry.owner == null || entry.owner.Spawned) {
				try {
					entry.callback();
					return true;
				} catch (Exception e) {
					HugsLibController.Logger.Error("TickDelayScheduler caught an exception while calling {0} registered by {1}: {2}",
						HugsLibUtility.DescribeDelegate(entry.callback), entry.owner == null ? "[null]" : entry.owner.ToString(), e);
				}
			}
			return false;
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

		public class SchedulerEntry {
			public readonly Action callback;
			public readonly int interval;
			public readonly bool repeat;
			public readonly Thing owner;
			public int dueAtTick;
			
			public SchedulerEntry(Action callback, int interval, Thing owner, int dueAtTick, bool repeat) {
				this.callback = callback;
				this.interval = interval;
				this.owner = owner;
				this.repeat = repeat;
				this.dueAtTick = dueAtTick;
			}
		}
	}
}