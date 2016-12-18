namespace HugsLib.Settings {
	public abstract class SettingHandleConvertible {
		public abstract void FromString(string settingValue);
		public abstract override string ToString();
	}
}