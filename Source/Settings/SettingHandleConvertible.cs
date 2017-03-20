namespace HugsLib.Settings {
	/// <summary>
	/// Base type for all custom SettingHandle types.
	/// Allows complex data structures to be stored in setting values by converting them to and from their string representation.
	/// </summary>
	public abstract class SettingHandleConvertible {
		public abstract void FromString(string settingValue);
		public abstract override string ToString();
	}
}