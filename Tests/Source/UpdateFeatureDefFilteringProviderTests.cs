using System.Linq;
using HugsLib;
using HugsLib.News;
using NUnit.Framework;

namespace HugsLibTests {
	[TestFixture]
	public class UpdateFeatureDefFilteringProviderTests {
		private readonly UpdateFeatureDef[] defs = {
			new UpdateFeatureDef {
				defName = "1",
				modIdentifier = "one",
				modNameReadable = "One"
			},
			new UpdateFeatureDef {
				defName = "2",
				modIdentifier = "one",
				modNameReadable = "One"
			},
			new UpdateFeatureDef {
				defName = "3",
				modIdentifier = "two",
				modNameReadable = "Two"
			},
			new UpdateFeatureDef {
				defName = "4",
				modIdentifier = "two",
				modNameReadable = "Eleven"
			},
			new UpdateFeatureDef {
				defName = "4",
				modIdentifier = "two",
				modNameReadable = "Six"
			},
			new UpdateFeatureDef {
				defName = "5",
				modIdentifier = "zeta",
				modNameReadable = "Z"
			},
		};
		
		private UpdateFeatureDefFilteringProvider filter;

		[SetUp]
		public void Setup() {
			filter = new UpdateFeatureDefFilteringProvider(defs);
		}

		[Test]
		public void NoFilterAllowsAll() {
			Assert.That(filter.MatchingDefsOf(defs), Is.EquivalentTo(defs));
		}

		[Test]
		public void ModIdentifierDefinesFilter() {
			Assert.That(filter.GetAvailableFilters().Select(f => f.id), 
				Is.EquivalentTo(new []{"one", "two", "zeta"}));
		}

		[Test]
		public void ValidFilterAssignment() {
			filter.CurrentFilterModIdentifier = "one";
			Assert.That(filter.MatchingDefsOf(defs).Select(d => d.defName), 
				Is.EquivalentTo(new []{"1", "2"}));
		}
		
		[Test]
		public void InvalidFilterAssignment() {
			filter.CurrentFilterModIdentifier = "noms";
			Assert.That(filter.CurrentFilterModIdentifier, Is.Null);
		}
		
		[Test]
		public void FilterRetainsFirstSeenModNameReadable() {
			Assert.That(filter.GetAvailableFilters().Select(f => f.label),
				Is.EquivalentTo(new[] {"One", "Two", "Z"})
			);
		}
		
		[Test]
		public void ModNameReadableReflectsCurrentFilter() {
			filter.CurrentFilterModIdentifier = "zeta";
			Assert.That(filter.CurrentFilterModNameReadable, Is.EqualTo("Z"));
			filter.CurrentFilterModIdentifier = null;
			Assert.That(filter.CurrentFilterModNameReadable, Is.Null);
		}
		
		[Test]
		public void DefCounts() {
			Assert.That(filter.GetAvailableFilters().Select(f => f.defCount), 
				Is.EquivalentTo(new []{2, 3, 1}));
		}
	}
}