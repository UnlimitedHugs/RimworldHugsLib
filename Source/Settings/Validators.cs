using System;

namespace HugsLib.Settings {
	/// <summary>
	/// A set of useful value constraints for use with SettingHandle
	/// </summary>
	public static class Validators {
		public static bool EnumValidator<T>(string value) where T: struct {
			return Enum.IsDefined(typeof(T), value);
		}

		public static SettingHandle.ValueIsValid IntRangeValidator(int min, int max) {
			return str => {
				int parsed;
				if (!int.TryParse(str, out parsed)) return false;
				return parsed >= min && parsed <= max;
			};
		}

		public static SettingHandle.ValueIsValid FloatRangeValidator(float min, float max) {
			return str => {
				float parsed;
				if (!float.TryParse(str, out parsed)) return false;
				return parsed >= min && parsed <= max;
			};
		}
	}
}