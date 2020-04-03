using System;
using System.Globalization;
using System.Xml.Linq;
using Verse;

namespace HugsLib.Spotter {
	public partial class ModSpottingManager {
		/// <summary>
		/// Used by <see cref="ModSpottingManager"/> to track mod packageIds loaded from the XML file. 
		/// </summary>
		private class TrackingEntry {
			private const string PackageIdAttributeName = "packageId";
			private const string FirstSeenAttributeName = "firstSeen";
			private const string LastSeenAttributeName = "lastSeen";

			public static TrackingEntry FromXMLElement(XElement node) {
				var packageId = node.Attribute(PackageIdAttributeName)?.Value;
				if (packageId.NullOrEmpty()) throw new FormatException("packageId not defined");
				var firstSeen = TryParseDateTimeAttribute(node, FirstSeenAttributeName);
				var lastSeen = TryParseDateTimeAttribute(node, LastSeenAttributeName);
				return new TrackingEntry(packageId, firstSeen, lastSeen);
			}

			public string PackageId { get; }
			public DateTime? FirstSeen { get; }
			public DateTime? LastSeen { get; set; }
			public DateTime? PreviouslyLastSeen { get; set; }

			public TrackingEntry(string packageId, DateTime? firstSeen, DateTime? lastSeen) {
				FirstSeen = firstSeen;
				LastSeen = lastSeen;
				PackageId = packageId;
			}

			public XElement Serialize() {
				var element = new XElement("mod", new XAttribute(PackageIdAttributeName, PackageId));
				if (FirstSeen != null) element.Add(SerializeDateTime(FirstSeen, FirstSeenAttributeName));
				if (LastSeen != null) element.Add(SerializeDateTime(LastSeen, LastSeenAttributeName));
				return element;
			}

			private static DateTime? TryParseDateTimeAttribute(XElement node, string attributeName) {
				if (attributeName == null) throw new ArgumentNullException(nameof(attributeName));
				var attributeValue = node.Attribute(attributeName)?.Value;
				try {
					if (attributeValue != null) {
						return DateTime.Parse(attributeValue, CultureInfo.InvariantCulture);
					}
				} catch (Exception e) {
					throw new FormatException($"Failed to parse {attributeName}: {attributeValue.ToStringSafe()}", e);
				}
				return null;
			}

			private static XAttribute SerializeDateTime(DateTime? dateTime, string attributeName) {
				if (dateTime == null) throw new ArgumentNullException(nameof(dateTime));
				return new XAttribute(attributeName, dateTime.Value.ToString(CultureInfo.InvariantCulture));
			}

			public override string ToString() {
				return $"[{nameof(TrackingEntry)} {PackageId} fs:{FirstSeen?.Year} " +
						$"ls:{LastSeen?.Year} pls:{PreviouslyLastSeen?.Year}]";
			}
		}
	}
}