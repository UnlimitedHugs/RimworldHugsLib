﻿﻿using System.Collections.Generic;
using System.Linq;
using HugsLib.Utils;
using NUnit.Framework;

namespace HugsLibTests {
	[TestFixture]
	public class CallbackSchedulerTests {
		private TickDelayScheduler scheduler;
		private List<int> MethodsCalled;
		private List<int> CallTimestamps;
		private int currentTick;

		[SetUp]
		public void PrepareRun() {
			currentTick = 0;
			scheduler = new TickDelayScheduler();
			scheduler.Initialize(currentTick);
			MethodsCalled = new List<int>();
			CallTimestamps = new List<int>();
		}

		[Test]
		public void SingleRun() {
			scheduler.ScheduleCallback(Callback1, 3);
			TickScheduler(10);
			AssertCalls("1");
			AssertTimestamps("3");
		}

		[Test]
		public void RepeatSingle() {
			scheduler.ScheduleCallback(Callback1, 3, null, true);
			TickScheduler(10);
			AssertCalls("1,1,1");
			AssertTimestamps("3,6,9");
		}

		[Test]
		public void MultipleOnce() {
			scheduler.ScheduleCallback(Callback1, 3);
			scheduler.ScheduleCallback(Callback2, 6);
			scheduler.ScheduleCallback(Callback3, 9);
			TickScheduler(10);
			AssertCalls("1,2,3");
			AssertTimestamps("3,6,9");
		}

		[Test]
		public void MultipleRepeat() {
			scheduler.ScheduleCallback(Callback1, 2, null, true);
			scheduler.ScheduleCallback(Callback2, 3, null, true);
			TickScheduler(10);
			AssertCalls("1,2,1,2,1,1,2,1");
			AssertTimestamps("2,3,4,6,6,8,9,10");
		}

		[Test]
		public void RepeatAndSingle() {
			scheduler.ScheduleCallback(Callback1, 4, null, true);
			scheduler.ScheduleCallback(Callback2, 5);
			TickScheduler(10);
			scheduler.ScheduleCallback(Callback3, 3);
			TickScheduler(10);
			AssertCalls("1,2,1,1,3,1,1");
			AssertTimestamps("4,5,8,12,13,16,20");
		}

		[Test]
		public void Unregister() {
			scheduler.ScheduleCallback(Callback1, 2, null, true);
			TickScheduler(1);
			scheduler.ScheduleCallback(Callback2, 2, null, true);
			TickScheduler(4);
			scheduler.TryUnscheduleCallback(Callback2);
			TickScheduler(5);
			AssertCalls(     "1,2,1,2,1,1,1");
			AssertTimestamps("2,3,4,5,6,8,10");
		}

		[Test]
		public void Reinitialize() {
			scheduler.ScheduleCallback(Callback1, 1, null, true);
			TickScheduler(3);
			scheduler.Initialize(10);
			TickScheduler(7);
			AssertCalls("1,1,1");
			AssertTimestamps("1,2,3");
		}

		private void AssertCalls(string sample) {
			var joined = string.Join(",", MethodsCalled.Select(i => i.ToString()).ToArray());
			Assert.That(joined, Is.EqualTo(sample), "order");
		}
		private void AssertTimestamps(string sample) {
			var joined = string.Join(",", CallTimestamps.Select(i => i.ToString()).ToArray());
			Assert.That(joined, Is.EqualTo(sample), "timestamps");
		}

		private void Callback1() { StampCallback(1); }
		private void Callback2() { StampCallback(2); }
		private void Callback3() { StampCallback(3); }

		private void StampCallback(int callbackId) {
			MethodsCalled.Add(callbackId);
			CallTimestamps.Add(currentTick);
		}

		private void TickScheduler(int numTicks) {
			while (numTicks>0) {
				numTicks--;
				currentTick++;
				scheduler.Tick(currentTick);
			}
		}
	}
}