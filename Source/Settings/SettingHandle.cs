using System;
using UnityEngine;

namespace HugsLib.Settings {
	/**
	 * An individual persistent setting owned by a mod.
	 * The extra layer of inheritance allows for type abstraction and storing SettingHandles in lists.
	 */
	public abstract class SettingHandle {
		public delegate bool ValueIsValid(string value);
		public delegate bool ShouldDisplay();
		public delegate bool DrawCustomControl(Rect rect);

		// unique id
		public string Name { get; protected set; }
		// name displayed in the settings menu
		public string Title { get; set; }
		// displayed as a tooltip in the settings menu
		public string Description { get; set; }
		// should return true if the passed value is valid for this setting, optional
		public ValueIsValid Validator { get; set; }
		// the string identifier prefix used to display enum values in the settings menu (e.g. "prefix_" for "prefix_EnumValue")
		public string EnumStringPrefix { get; set; }
		// return true to make this setting visible in the menu. Optional. Still occupies scroll space when invisible.
		public ShouldDisplay VisibilityPredicate { get; set; }
		// draw a custom control for the settings menu entry. Entry name is already drawn when this is called. Optional. Return value indicates if the control changed the setting.
		public DrawCustomControl CustomDrawer { get; set; }
		// when true, setting will never appear in the menu. For serialized data.
		public bool NeverVisible { get; set; }
		// when true, will not save this setting to the xml file. Useful in conjunction with CustomDrawer for placing buttons in the settings menu.
		public bool Unsaved { get; set; }

		public abstract string StringValue { get; set; }
		public abstract Type ValueType { get; }
		public abstract void ResetToDefault();
		public abstract bool HasDefaultValue();
	}

	public class SettingHandle<T> : SettingHandle {
		// implicitly cast settings to its value type
		public static implicit operator T(SettingHandle<T> handle) {
			return handle.Value;
		}

		public T Value { get; set; }
		public T DefaultValue { get; set; }

		public override string StringValue {
			get {
				return Value != null ? Value.ToString() : "";
			}
			set {
				if (Validator != null && !Validator(value)) {
					// validation failed, reset to default
					Value = DefaultValue;
					return;
				}
				try {
					if (Value is Enum) {
						Value = (T) Enum.Parse(typeof (T), value);
					} else {
						Value = (T) Convert.ChangeType(value, typeof (T));
					}
				} catch (Exception) {
					// fallback to default value on bad data
					Value = DefaultValue;
				}

			}
		}

		public override Type ValueType {
			get { return typeof (T); }
		}

		internal SettingHandle(string name) {
			Name = name;
		}

		public override void ResetToDefault() {
			Value = DefaultValue;
		}

		public override bool HasDefaultValue() {
			return Value.Equals(DefaultValue);
		}

		public override string ToString() {
			return StringValue;
		}
	}


}