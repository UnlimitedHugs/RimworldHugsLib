using System;

namespace HugsLib.Source.Detour {
	/**
	 * Apply this to a static method to have it called when a detour fails.
	 * You can provide method names as arguments- those will be the methods covered, provided they are part of the same class.
	 * When used without arguments, the method becomes the fallback for all detour targets in the same class.
	 * The method must have the following signature: void (MemberInfo attemptedDestination, MethodInfo existingDestination, Exception e)
	 * Only the fist matching handler will be called.
	 */
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	public class DetourFallbackAttribute : Attribute {
		public readonly string[] targetMemberNames;

		// disable default constructor
		private DetourFallbackAttribute() { }

		public DetourFallbackAttribute(params string[] targetMemberName) {
			if (targetMemberName == null) throw new Exception("DetourFallbackAttribute argument must not be null");
			targetMemberNames = targetMemberName;
		}
	}
}