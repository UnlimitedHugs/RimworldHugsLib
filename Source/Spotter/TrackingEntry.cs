using System;
using System.Xml.Linq;
using Verse;

namespace HugsLib.Spotter {
	public partial class ModSpottingManager {
		/// <summary>
		/// Used by <see cref="ModSpottingManager"/> to track mod packageIds loaded from the XML file. 
		/// </summary>
		private class TrackingEntry {
			private const string PackageIdAttributeName = "packageId";

			public static TrackingEntry FromXMLElement(XElement node) {
				var packageId = node.Attribute(PackageIdAttributeName)?.Value;
				if (packageId.NullOrEmpty()) throw new FormatException("packageId not defined");
				return new TrackingEntry(packageId);
			}

			public string PackageId { get; }
			public bool FirstTimeSeen { get; set; }

			public TrackingEntry(string packageId) {
				PackageId = packageId;
			}

			public XElement Serialize() {
				return new XElement("mod", new XAttribute(PackageIdAttributeName, PackageId));
			}

			public override string ToString() {
				return $"[{nameof(TrackingEntry)} {nameof(PackageId)}:{PackageId} " +
					$"{nameof(FirstTimeSeen)}:{FirstTimeSeen}]";
			}
		}
	}
}