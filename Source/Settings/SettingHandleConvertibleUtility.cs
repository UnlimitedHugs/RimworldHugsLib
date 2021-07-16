using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using HugsLib.Utils;
using Verse;

namespace HugsLib.Settings {
	/// <summary>
	/// Utility methods for SettingHandleConvertible data objects.
	/// These are useful for packing and unpacking your custom fields into a string without bothering with manual serialization.
	/// </summary>
	public static class SettingHandleConvertibleUtility {
		/// <summary>
		/// Deserializes an XML string into an existing object instance.
		/// </summary>
		/// <param name="serializedValues">The serialized values to fill the object with</param>
		/// <param name="targetObject">The object to receive the deserialized values</param>
		public static void DeserializeValuesFromString(string serializedValues, object targetObject) {
			try {
				if(serializedValues.NullOrEmpty()) return;
				DoSerializationChecks(targetObject);
				var serializer = new XmlSerializer(targetObject.GetType());
				using (var reader = new StringReader(serializedValues)) {
					var source = serializer.Deserialize(reader);
					CopySerializedMembersFromObject(source, targetObject);	
				}
			} catch (Exception e) {
				throw new SerializationException(string.Format("Exception while serializing {0}: {1}", targetObject != null ? targetObject.GetType() : null, e));
			}
		}

		/// <summary>
		/// Serializes an object into a compact XML string.
		/// Whitespace and namespace declarations are omitted.
		/// Make sure the object is annotated with SerializableAttribute and the fields to serialize with XmlElementAttribute.
		/// </summary>
		/// <param name="targetObject">The object to serialize</param>
		public static string SerializeValuesToString(object targetObject) {
			try {
				DoSerializationChecks(targetObject);
				var serializer = new XmlSerializer(targetObject.GetType());
				var writerSettings = new XmlWriterSettings {Indent = false, NewLineHandling = NewLineHandling.Entitize, OmitXmlDeclaration = true};
				var noNamespaces = new XmlSerializerNamespaces(new[] {XmlQualifiedName.Empty});
				using (var stream = new StringWriter()) {
					var writer = XmlWriter.Create(stream, writerSettings);
					serializer.Serialize(writer, targetObject, noNamespaces);
					return stream.ToString();
				}
			} catch (Exception e) {
				throw new SerializationException(string.Format("Exception while deserializing {0}: {1}", targetObject != null ? targetObject.GetType() : null, e));
			}
		}

		private static void DoSerializationChecks(object targetObject) {
			if (targetObject == null) throw new NullReferenceException("targetObject must be set");
			if (targetObject.GetType().TryGetAttributeSafely<SerializableAttribute>() == null) throw new SerializationException("targetObject must have the Serializable attribute");
		}

		private static void CopySerializedMembersFromObject(object source, object destination) {
			if (source.GetType() != destination.GetType()) throw new Exception(string.Format("Mismatched types: {0} vs {1}", source.GetType(), destination.GetType()));
			// fields
			foreach (var field in source.GetType().GetFields(HugsLibUtility.AllBindingFlags)) {
				if (field.TryGetAttributeSafely<XmlElementAttribute>() != null) {
					field.SetValue(destination, field.GetValue(source));
				}
			}
			// properties
			foreach (var prop in source.GetType().GetProperties(HugsLibUtility.AllBindingFlags)) {
				if (prop.TryGetAttributeSafely<XmlElementAttribute>() != null) {
					prop.SetValue(destination, prop.GetValue(source, null), null);
				}
			}
		}
	}
}