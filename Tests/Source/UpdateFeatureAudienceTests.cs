using System;
using System.Collections.Generic;
using System.Linq;
using HugsLib;
using HugsLib.News;
using NUnit.Framework;

namespace HugsLibTests {
	public class UpdateFeatureAudienceTests {
		private readonly UpdateFeatureDef[] defs = {
			new UpdateFeatureDef {
				defName = "1",
				OverridePackageId = "mod1"
			},
			new UpdateFeatureDef {
				defName = "2",
				OverridePackageId = "mod1",
				targetAudience = UpdateFeatureTargetAudience.NewPlayers
			},
			new UpdateFeatureDef {
				defName = "3",
				OverridePackageId = "mod1",
				targetAudience = UpdateFeatureTargetAudience.ReturningPlayers
			},
			new UpdateFeatureDef {
				defName = "4",
				OverridePackageId = "mod2",
				targetAudience = UpdateFeatureTargetAudience.AllPlayers
			},
			new UpdateFeatureDef {
				defName = "5",
				OverridePackageId = "mod2",
				targetAudience = UpdateFeatureTargetAudience.NewPlayers
			}
		};

		[Test]
		public void MultipleDefsTest() {
			void AssertFilteredDefs(IEnumerable<string> firstTimeSeenPackageIds,
				IEnumerable<string> expectedDefNames, string assertionMessage) {
				var results = UpdateFeatureManager.FilterFeatureDefsByMatchingAudience(
					defs, firstTimeSeenPackageIds.Contains, null
				).Select(d => d.defName);
				Assert.That(results, Is.EquivalentTo(expectedDefNames), assertionMessage);
			}

			AssertFilteredDefs(
				new[] {"mod1", "mod2"},
				new[] {"2", "4", "5"},
				"both first seen"
			);
			AssertFilteredDefs(
				new[] {"mod1"},
				new[] {"2", "4"},
				"mod1 first seen"
			);
			AssertFilteredDefs(
				new[] {"mod2"},
				new[] {"1", "3", "4", "5"},
				"mod2 first seen"
			);
			AssertFilteredDefs(
				Enumerable.Empty<string>(),
				new[] {"1", "3", "4"},
				"none first seen"
			);
		}

		[Test]
		public void NullPackageIdTest() {
			var noPackageDef = new UpdateFeatureDef();
			Assert.IsNull(noPackageDef.modContentPack);
			Assert.IsNull(noPackageDef.OverridePackageId);
			Assert.Throws<InvalidOperationException>(() => {
					var _ = UpdateFeatureManager.FilterFeatureDefsByMatchingAudience(
						new[] {noPackageDef}, s => false, ex => throw ex
					).ToArray();
				}
			);
		}
	}
}