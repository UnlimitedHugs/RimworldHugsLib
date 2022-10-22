using System;
using System.Xml.Serialization;
using HugsLib.Settings;

namespace HugsLib.Logs {
	public interface ILogPublisherOptions {
		bool UseCustomOptions { get; set; }
		bool UseUrlShortener { get; set; }
		bool IncludePlatformInfo { get; set; }
		bool AllowUnlimitedLogSize { get; set; }
		string AuthToken { get; set; }
	}

	[Serializable]
	public class LogPublisherOptions : SettingHandleConvertible, IEquatable<LogPublisherOptions>, ILogPublisherOptions {
		[XmlElement]
		public bool UseCustomOptions { get; set; }
		[XmlElement]
		public bool UseUrlShortener { get; set; }
		[XmlElement]
		public bool IncludePlatformInfo { get; set; }
		[XmlElement]
		public bool AllowUnlimitedLogSize { get; set; }
		[XmlElement]
		public string AuthToken { get; set; }

		public override void FromString(string settingValue) {
			SettingHandleConvertibleUtility.DeserializeValuesFromString(settingValue, this);
		}

		public override string ToString() {
			return SettingHandleConvertibleUtility.SerializeValuesToString(this);
		}

		public override bool ShouldBeSaved {
			get { return !Equals(this, new LogPublisherOptions()); }
		}

		public bool Equals(LogPublisherOptions other) {
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return UseCustomOptions == other.UseCustomOptions 
				&& UseUrlShortener == other.UseUrlShortener 
				&& IncludePlatformInfo == other.IncludePlatformInfo 
				&& AllowUnlimitedLogSize == other.AllowUnlimitedLogSize
				&& AuthToken == other.AuthToken;
		}
	}
}