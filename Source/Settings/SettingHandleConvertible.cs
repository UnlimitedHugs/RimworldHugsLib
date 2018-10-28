using HugsLib.Source.Settings;

namespace HugsLib.Settings {
	/// <summary>
	/// Base type for all custom SettingHandle types.
	/// Allows complex data structures to be stored in setting values by converting them to and from their string representation.
	/// See <see cref="SettingHandleConvertibleUtility"/> for an easy way to serialize complex types to XML.
	/// </summary>
	public abstract class SettingHandleConvertible {
		/// <summary>
		/// Return false to prevent this object from serializing and being written to file.
		/// </summary>
		public virtual bool ShouldBeSaved {
			get { return true; }
		}
		/// <summary>
		/// Called when settings handles of this type load an existing value.
		/// Should deserialize and restore the state of the object using the provided string.
		/// </summary>
		public abstract void FromString(string settingValue);
		/// <summary>
		/// Called when handles of this type are being saved, and only if <see cref="ShouldBeSaved"/> return true.
		/// Should serialize the state of the object into a string so it can be restored later.
		/// </summary>
		public abstract override string ToString();
	}
}