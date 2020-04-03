using System;

namespace HugsLib.Spotter {
	/// <summary>
	/// Allows to inject arbitrary timestamps into <see cref="ModSpottingManager"/> entries. Used in unit testing.
	/// </summary>
	internal interface ICurrentDateTimeSource {
		DateTime Now { get; }
	}

	internal class SystemDateTimeSource : ICurrentDateTimeSource {
		public DateTime Now {
			get { return DateTime.Now; }
		}
	}
}