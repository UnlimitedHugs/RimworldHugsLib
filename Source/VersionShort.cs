using System;

namespace HugsLib {
	/**
	 * A shorter, invariable alternative to System.Version in the format of major.minor.patch
	 * System.Version can be implicitly cast to this type.
	 */
	public class VersionShort : IComparable, IComparable<VersionShort>, IEquatable<VersionShort> {
		public const char Separator = '.';

		public readonly int major;
		public readonly int minor;
		public readonly int patch;

		public static VersionShort Parse(string version) {
			var parts = StringToParts(version);
			var ver = new VersionShort(parts[0], parts[1], parts[2]);
			return ver;
		}

		public static VersionShort TryParse(string version) {
			try {
				return Parse(version);
			} catch (Exception) {
				return null;
			}
		}

		public static implicit operator VersionShort(Version version) {
			return new VersionShort(version.Major, version.Minor, version.Build);
		}

		public static bool operator ==(VersionShort v1, VersionShort v2) {
			if (ReferenceEquals(v1, null)) return ReferenceEquals(v2, null);
			return v1.Equals(v2);
		}
		public static bool operator !=(VersionShort v1, VersionShort v2) {
			return !(v1 == v2);
		}
		public static bool operator <(VersionShort v1, VersionShort v2) {
			if (v1 == null) throw new ArgumentNullException("v1");
			return v1.CompareTo(v2) < 0;
		}
		public static bool operator <=(VersionShort v1, VersionShort v2) {
			if (v1 == null) throw new ArgumentNullException("v1");
			return v1.CompareTo(v2) <= 0;
		}
		public static bool operator >(VersionShort v1, VersionShort v2) {
			return v2 < v1;
		}
		public static bool operator >=(VersionShort v1, VersionShort v2) {
			return v2 <= v1;
		}

		public static int[] StringToParts(string version) {
			if (string.IsNullOrEmpty(version)) throw new FormatException("Parameter is empty");
			var parts = version.Split(Separator);
			if (parts.Length < 2 || parts.Length > 3) throw new FormatException("Version string requires at least 2 and at most 4 parts");
			var result = new int[3];
			for (int i = 0; i < parts.Length; i++) {
				int parsed;
				if (!int.TryParse(parts[i], out parsed) || parsed < 0) throw new FormatException("Version contains invalid number");
				if (i > 2) break;
				result[i] = parsed;
			}
			return result;
		}
		
		public VersionShort(int major = 0, int minor = 0, int patch = 0) {
			this.major = major;
			this.minor = minor;
			this.patch = patch;
			EnsureValuesAreValid();
		}

		public override string ToString() {
			return string.Concat(major, Separator, minor, Separator, patch);
		}

		public override int GetHashCode() {
			const int HashSeed = 1009;
			const int HashFactor = 9176;
			unchecked {
				int hash = HashSeed;
				hash = hash * HashFactor + major;
				hash = hash * HashFactor + minor;
				hash = hash * HashFactor + patch;
				return hash;
			}
		}

		public override bool Equals(object obj) {
			var other = obj as VersionShort;
			return Equals(other);
		}

		public bool Equals(VersionShort other) {
			if (other == null) return false;
			return major == other.major && minor == other.minor && patch == other.patch;
		}

		public int CompareTo(VersionShort ver) {
			if (major != ver.major) return major > ver.major ? 1 : -1;
			if (minor != ver.minor) return minor > ver.minor ? 1 : -1;
			if (patch == ver.patch) return 0;
			return patch > ver.patch ? 1 : -1;
		}

		public int CompareTo(object obj) {
			if (obj == null) return 1;
			var ver = obj as VersionShort;
			if (ver == null) throw new ArgumentException("Argument must be VersionShort");
			return CompareTo(ver);
		}

		private void EnsureValuesAreValid() {
			if(major < 0 | minor < 0 | patch < 0) throw new FormatException("Invalid version value: "+this);
		}

		
	}
}