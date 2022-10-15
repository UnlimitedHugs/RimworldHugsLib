﻿﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HugsLib.Utils;
using NUnit.Framework;
using RimWorld.Planet;
using Verse;

namespace HugsLibTests {
	[TestFixture]
	public class DistributedTickSchedulerTests {
		private DistributedTickScheduler scheduler;
		private List<int> callsPerCallback;
		private List<int> calledReceiverIndexes;
		private int totalCallbackCalls;
		private int currentTick;
		private int callbackCreationIndex;
		private List<Thing> registeredReceivers;

		[SetUp]
		public void PrepareRun() {
			currentTick = 0;
			scheduler = new DistributedTickScheduler();
			scheduler.Initialize(currentTick);
			totalCallbackCalls = 0;
			callsPerCallback = new List<int>();
			callbackCreationIndex = 0;
			registeredReceivers = new List<Thing>();
			calledReceiverIndexes = new List<int>();
			SetUpMockGame();
		}

		[Test]
		public void LessCallbacksThanInterval() {
			foreach (var (callback, owner) in PrepareReceivers(3)) {
				scheduler.RegisterTickability(callback, 10, owner);
			}
			TickScheduler(30);
			AssertTotalCalls(9);
			AssertNumCallsPerCallback(3);
		}

		[Test]
		public void AsManyCallbacksAsInterval() {
			foreach (var (callback, owner) in PrepareReceivers(5)) {
				scheduler.RegisterTickability(callback, 5, owner);
			}
			TickScheduler(15);
			AssertNumCallsPerCallback(3);
			AssertTotalCalls(15);
		}

		[Test]
		public void MoreCallbacksThanInterval() {
			foreach (var (callback, owner) in PrepareReceivers(7)) {
				scheduler.RegisterTickability(callback, 5, owner);
			}
			TickScheduler(15);
			AssertNumCallsPerCallback(3);
			AssertTotalCalls(21);
		}

		[Test]
		public void OneTickInterval() {
			foreach (var (callback, owner) in PrepareReceivers(5)) {
				scheduler.RegisterTickability(callback, 1, owner);
			}
			TickScheduler(5);
			AssertTotalCalls(25);
			AssertNumCallsPerCallback(5);
		}

		[Test]
		public void CallbackInfluxMidCycle() {
			foreach (var (callback, owner) in PrepareReceivers(5)) {
				scheduler.RegisterTickability(callback, 5, owner);
			}
			TickScheduler(3);
			foreach (var (callback, owner) in PrepareReceivers(5)) {
				scheduler.RegisterTickability(callback, 5, owner);
			}
			TickScheduler(7);
			AssertReceiverCalls(true, 0, 1, 2, 3, 4);
			AssertReceiverCalls(true, 5, 6, 7, 8, 9);
			TickScheduler(5);
			AssertTotalCalls(3 + (7 + 5) * 2);
		}

		[Test]
		public void MultipleIntervals() {
			foreach (var (callback, owner) in PrepareReceivers(3)) {
				scheduler.RegisterTickability(callback, 3, owner);
			}
			foreach (var (callback, owner) in PrepareReceivers(3)) {
				scheduler.RegisterTickability(callback, 6, owner);
			}
			TickScheduler(3);
			AssertReceiverCalls(true, 0, 1, 2);
			TickScheduler(3);
			AssertTotalCalls(3 + 3 + 3);
		}

		[Test]
		public void Reinitialize() {
			foreach (var (callback, owner) in PrepareReceivers(10)) {
				scheduler.RegisterTickability(callback, 5, owner);
			}
			TickScheduler(5);
			AssertTotalCalls(10);
			AssertNumCallsPerCallback(1);
			scheduler.Initialize(11);
			TickScheduler(3);
			AssertTotalCalls(10);
		}

		[Test]
		public void Unregister() {
			foreach (var (callback, owner) in PrepareReceivers(3)) {
				scheduler.RegisterTickability(callback, 3, owner);
			}
			foreach (var (callback, owner) in PrepareReceivers(3)) {
				scheduler.RegisterTickability(callback, 6, owner);
			}
			TickScheduler(3);
			AssertReceiverCalls(true, 0, 1, 2);
			for (int i = 0; i < 3; i++) {
				scheduler.UnregisterTickability(registeredReceivers[i]);
			}
			TickScheduler(3);
			AssertReceiverCalls(true, 3, 4, 5);
			for (int i = 3; i < 6; i++) {
				scheduler.UnregisterTickability(registeredReceivers[i]);
			}
			TickScheduler(50);
			AssertTotalCalls(3 + 3);
		}

		private IEnumerable<(Action, Thing)> PrepareReceivers(int amount) {
			while (amount>0) {
				callsPerCallback.Add(0);
				var index = callbackCreationIndex;
				Action action = () => {
					totalCallbackCalls++;
					callsPerCallback[index]++;
					calledReceiverIndexes.Add(index);
				};
				var owner = MakeMockSpawnedThing();
				registeredReceivers.Add(owner);
				callbackCreationIndex++;
				amount--;
				yield return (action, owner);
			}
		}

		private Thing MakeMockSpawnedThing() {
			var spawnedThing = new Thing();
			var mapIndexField = typeof(Thing)
					.GetField("mapIndexOrState", BindingFlags.NonPublic | BindingFlags.Instance)
				?? throw new NullReferenceException();
			mapIndexField.SetValue(spawnedThing, (sbyte)0);
			return spawnedThing;
		}

		private static void SetUpMockGame() {
			var uninitializedGame = (Game)FormatterServices.GetUninitializedObject(typeof(Game));
			Current.Game = uninitializedGame;
			uninitializedGame.World = new World();
			var privateMapsField = typeof(Game).GetField("maps", BindingFlags.NonPublic | BindingFlags.Instance)
				?? throw new NullReferenceException();
			privateMapsField.SetValue(Current.Game, new List<Map> {
					new Map()
				}
			);
		}
		
		private void AssertTotalCalls(int expectedCalls) {
			Assert.That(totalCallbackCalls, Is.EqualTo(expectedCalls), "total calls");
		}

		private void AssertNumCallsPerCallback(int expectedNumCalls) {
			var pass = callsPerCallback.All(num => num == expectedNumCalls);
			if (!pass) {
				Assert.Fail("Num calls per callback failed. Expected {0}, min: {1}, max: {2}\nlisting: {3}", 
					expectedNumCalls, callsPerCallback.Min(), callsPerCallback.Max(), 
					string.Join("", callsPerCallback.Select(i => i.ToString()).ToArray()));
			}
		}

		private void AssertReceiverCalls(bool allowOtherReceivers, params int[] receiverIndexes) {
			try {
				var pendingIndexes = new Queue<int>(receiverIndexes);
				int? currentExpectedIndex = null;
				for (var i = 0; i < calledReceiverIndexes.Count; i++) {
					if (pendingIndexes.Count > 0 || currentExpectedIndex != null) {
						if (currentExpectedIndex == null) {
							currentExpectedIndex = pendingIndexes.Dequeue(); 
						}
						if (calledReceiverIndexes[i] == currentExpectedIndex) {
							currentExpectedIndex = null;
						} else if (!allowOtherReceivers) {
							throw new Exception("unexpected receiver call: " +
								$"expected: {currentExpectedIndex} actual: {calledReceiverIndexes[i]}");
						}
					} else if(!allowOtherReceivers) {
						throw new Exception("more calls than expected");
					}
				}
				if (pendingIndexes.Count > 0 || currentExpectedIndex != null) {
					throw new Exception("fewer calls than expected");
				}
			} catch (Exception e) {
				throw new Exception($"AssertReceiverCalls failed: expected: [{receiverIndexes.Join(",")}]" +
					$"{(allowOtherReceivers?" (permissive)":"")} actual:[{calledReceiverIndexes.Join(",")}] " +
					$"exception: {e}");
			}
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