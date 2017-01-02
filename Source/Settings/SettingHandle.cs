using System;
using UnityEngine;

// ReSharper disable UnusedAutoPropertyAccessor.Global

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
		// return true to make this setting visible in the menu. Optional.
		public ShouldDisplay VisibilityPredicate { get; set; }
		// draw a custom control for the settings menu entry. Entry name is already drawn when this is called. Optional. Return value indicates if the control changed the setting.
		public DrawCustomControl CustomDrawer { get; set; }
		// when true, setting will never appear in the menu. For serialized data.
		public bool NeverVisible { get; set; }
		// when true, will not save this setting to the xml file. Useful in conjunction with CustomDrawer for placing buttons in the settings menu.
		public bool Unsaved { get; set; }
		// specifies by how much the + and - buttons should change a numeric setting.
		public int SpinnerIncrement { get; set; }
		// when CustomDrawer is used, specifies the height of the row for the handle. Leave at 0 for default height.
		public float CustomDrawerHeight { get; set; }

		public abstract string StringValue { get; set; }
		public abstract Type ValueType { get; }
		public abstract void ResetToDefault();
		public abstract bool HasDefaultValue();

		protected SettingHandle() {
			SpinnerIncrement = 1;
		}
	}

	public class SettingHandle<T> : SettingHandle {
		public delegate void ValueChanged(T newValue);

		// implicitly cast settings to its value type
		public static implicit operator T(SettingHandle<T> handle) {
			return handle.Value;
		}

		// called when the value of the handle changes. Optional.
		public ValueChanged OnValueChanged { get; set; }

		private T _value;
		public T Value {
			get { return _value; }
			set {
				var prevValue = _value;
				_value = value;
				if (OnValueChanged != null && !SafeEquals(prevValue, _value)) {
					try {
						OnValueChanged(_value);
					} catch (Exception e) {
						HugsLibController.Logger.Error("Exception while calling SettingHandle.OnValueChanged. Handle name was: \"{0}\" Value was: \"{1}\". Exception was: {2}", Name, StringValue, e);
						throw;
					}
				}
			}
		}
		public T DefaultValue { get; set; }

		public override string StringValue {
			get {
				try {
					return Value != null ? Value.ToString() : "";
				} catch(Exception e) {
					HugsLibController.Logger.Error("Failed to serialize setting \"{0}\" of type {1}. Setting value was reset. Exception was: {2}", Name, typeof(T).FullName, e);
					try {
						Value = DefaultValue;
						return DefaultValue != null ? DefaultValue.ToString() : "";
					} catch {
						return "";
					}
				}
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
					} else if (typeof(SettingHandleConvertible).IsAssignableFrom(typeof(T))) {
						Value = CustomValueFromString(value);
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
			return SafeEquals(Value, DefaultValue);
		}

		public override string ToString() {
			return StringValue;
		}

		private T CustomValueFromString(string stringValue) {
			try {
				var val = (T) Activator.CreateInstance(typeof (T), true);
				var convertible = val as SettingHandleConvertible;
				if (convertible != null) convertible.FromString(stringValue);
				return val;
			} catch (Exception e) {
				HugsLibController.Logger.Error("Failed to parse setting \"{0}\" as custom type {1}. Setting value was reset. Value was: \"{2}\". Exception was: {3}", Name, typeof(T).FullName, stringValue, e);
				throw;
			}
		}

		// Equals comparison with null support
		private bool SafeEquals(T valueOne, T valueTwo) {
			if (valueOne != null) return valueOne.Equals(valueTwo);
			return valueTwo == null;
		}
	}


}