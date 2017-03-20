using System;

namespace HugsLib.Source.Attrib {
	/// <summary>
	/// Apply this to a static method with a MemberInfo and an Attribute parameter to make it a handler for types or members with a certain attribute.
	/// At load time the method will be called as many times as there are types or members marked with the attribute specified as the argument.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	public class DetectableAttributeHandler : Attribute, IDetectableAttribute {
		public const string ExpectedSignature = "void(MemberInfo memberOrType, Attribute attrib)";
		
		public readonly Type detectedAttributeType;

		public DetectableAttributeHandler(Type attributeType) {
			VerifyType(attributeType);
			detectedAttributeType = attributeType;
		}

		private void VerifyType(Type type) {
			if (type == null
			    || !typeof (IDetectableAttribute).IsAssignableFrom(type)
			    || !typeof (Attribute).IsAssignableFrom(type)) {
					throw new Exception("DetectableAttributeHandler argument must be a type that extends Attribute and implements IDetectableAttribute");
			}
		}
	}
}