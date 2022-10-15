using System;
using System.Collections.Generic;
using System.Linq;

namespace HugsLib.News {
	/// <summary>
	/// Filters <see cref="UpdateFeatureDef"/>s by their mod identifier.
	/// </summary>
	internal class UpdateFeatureDefFilteringProvider {
		private readonly FilteringEntry[] filteringEntries;

		private string? currentFilterModId;
		public string? CurrentFilterModIdentifier {
			get { return currentFilterModId; }
			set {
				var matchingEntry = value != null ? TryGetFilteringEntry(value) : null;
				currentFilterModId = matchingEntry?.FilterModId;
				CurrentFilterModNameReadable = matchingEntry?.ModNameReadable;
			}
		}
		
		public string? CurrentFilterModNameReadable { get; private set; }

		public UpdateFeatureDefFilteringProvider(IEnumerable<UpdateFeatureDef> newsDefs) {
			filteringEntries = GenerateFilteringEntriesFromDefs(newsDefs);
		}

		public IEnumerable<(string id, string label, int defCount)> GetAvailableFilters() {
			return filteringEntries.Select(e => (e.FilterModId, e.ModNameReadable, e.NewsDefCount));
		}

		public IEnumerable<UpdateFeatureDef> MatchingDefsOf(IEnumerable<UpdateFeatureDef> defs) {
			return currentFilterModId == null ? defs 
				: defs.Where(d => d?.modIdentifier == currentFilterModId);
		}

		private static FilteringEntry[] GenerateFilteringEntriesFromDefs(IEnumerable<UpdateFeatureDef> defs) {
			var seenIdentifiers = new Dictionary<string, (string name, int count)>();
			foreach (var def in defs) {
				var defModIdentifier = def.OwningModId;
				if (defModIdentifier != null!) {
					if (!seenIdentifiers.TryGetValue(defModIdentifier, out var seenIdentifier)) {
						seenIdentifiers.Add(defModIdentifier, (def.modNameReadable, 1));
					} else {
						seenIdentifiers[defModIdentifier] = (seenIdentifier.name, seenIdentifier.count + 1);
					}
				}
			}
			return seenIdentifiers
				.Select(i => new FilteringEntry(i.Key, i.Value.name, i.Value.count))
				.OrderBy(i => i.ModNameReadable, StringComparer.InvariantCultureIgnoreCase)
				.ToArray();
		}

		private FilteringEntry? TryGetFilteringEntry(string modIdentifier) {
			return filteringEntries.FirstOrDefault(e => e.FilterModId == modIdentifier);
		}
		
		private struct FilteringEntry {
			public string FilterModId { get; }
			public string ModNameReadable { get; }
			public int NewsDefCount { get; }
			public FilteringEntry(string filterModId, string modNameReadable, int newsDefCount) {
				FilterModId = filterModId;
				ModNameReadable = modNameReadable;
				NewsDefCount = newsDefCount;
			}
		}
	}
}