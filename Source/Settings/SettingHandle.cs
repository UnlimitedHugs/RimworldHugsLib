using System;
using UnityEngine;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace HugsLib.Settings {
	/// <summary>
	/// An individual persistent setting owned by a mod.
	/// The extra layer of inheritance allows for type abstraction and storing SettingHandles in lists.
	/// </summary>
	public abstract class SettingHandle {
		public delegate bool ValueIsValid(string value);
		public delegate bool ShouldDisplay();
		public delegate bool DrawCustomControl(Rect rect);

		/// <summary>
		/// Unique idenfifier of the setting.
		/// </summary>
		public string Name { get; protected set; }
		/// <summary>
		/// Name displayed in the settings menu.
		/// </summary>
		public string Title { get; set; }
		/// <summary>
		/// Displayed as a tooltip in the settings menu.
		/// </summary>
		public string Description { get; set; }
		/// <summary>
		/// Should return true if the passed value is valid for this setting. Optional.
		/// </summary>
		public ValueIsValid Validator { get; set; }
		/// <summary>
		/// The string identifier prefix used to display enum values in the settings menu (e.g. "prefix_" for "prefix_EnumValue")
		/// </summary>
		public string EnumStringPrefix { get; set; }
		/// <summary>
		/// Return true to make this setting visible in the menu. Optional.
		/// An invisible setting can still be reset to default using the Reset All button.
		/// </summary>
		public ShouldDisplay VisibilityPredicate { get; set; }
		/// <summary>
		/// Draw a custom control for the settings menu entry. Entry name is already drawn when this is called. Optional. Return value indicates if the control changed the setting.
		/// </summary>
		public DrawCustomControl CustomDrawer { get; set; }
		/// <summary>
		/// When true, setting will never appear in the menu and can not be reset to default by the player. For serialized data.
		/// </summary>
		public bool NeverVisible { get; set; }
		/// <summary>
		/// When true, will not save this setting to the xml file. Useful in conjunction with CustomDrawer for placing buttons in the settings menu.
		/// </summary>
		public bool Unsaved { get; set; }
		/// <summary>
		/// Specifies by how much the + and - buttons should change a numeric setting.
		/// </summary>
		public int SpinnerIncrement { get; set; }
		/// <summary>
		/// When CustomDrawer is used, specifies the height of the row for the handle. Leave at 0 for default height.
		/// </summary>
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

		/// <summary>
		/// Implicitly cast handles to the Value they carry.
		/// </summary>
		public static implicit operator T(SettingHandle<T> handle) {
			return handle.Value;
		}

		/// <summary>
		/// Called when the Value of the handle changes. Optional.
		/// </summary>
		public ValueChanged OnValueChanged { get; set; }

		private T _value;
		/// <summary>
		/// The actual value of the setting. 
		/// This is converted to its string representation when settings are saved.
		/// Assigning a new value will trigger the OnValueChanged delegate.
		/// </summary>
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

		/// <summary>
		/// The value the setting assumes when initially created or reset.
		/// </summary>
		public T DefaultValue { get; set; }

		/// <summary>
		/// Retrieves the string representation of the setting or assigns a new setting value using a string.
		/// Will trigger the Validator delegate if assigned and change the Value property if the validation passes.
		/// </summary>
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

		/// <summary>
		/// Returns the type of the handle Value property.
		/// </summary>
		public override Type ValueType {
			get { return typeof (T); }
		}

		internal SettingHandle(string name) {
			Name = name;
		}

		/// <summary>
		/// Assings the default value to the Value property.
		/// </summary>
		public override void ResetToDefault() {
			Value = DefaultValue;
		}

		/// <summary>
		/// Returns true if the handle is set to its default value.
		/// </summary>
		/// <returns></returns>
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