using System;
using System.Collections.Generic;
using System.Reflection;

namespace HugsLib.Source.Detour {
	/**
	 * A tool to detour calls form one method to another. Will use Community Core Library detouring, if available, and its own equivalent otherwise.
	 */
	public class DetourProvider {
		/**
        * keep track of performed detours
        */
		private static readonly Dictionary<MethodInfo, MethodInfo> detours = new Dictionary<MethodInfo, MethodInfo>();

		/**
		 * Same as TryCompatibleDetour, but writes an error to the console on failure
		 */
		public static void CompatibleDetour(MethodInfo source, MethodInfo destination) {
			bool result = false;
			Exception failureException = null;
			try {
				result = CompatibleDetourWithExceptions(source, destination);
			} catch (Exception e) {
				result = false;
				failureException = e;
			} finally {
				if (!result) ThrowClearerDetourException(failureException, source, destination, "method");
			}
		}

		/**
		 * Checks if CCL is available, and uses it to detour a method. Otherwise, TryIndepentDetour is used.
		 */
		public static bool TryCompatibleDetour(MethodInfo source, MethodInfo destination) {
			if (source == null || destination == null) return false;
			return TryIndepentDetour(source, destination);
		}

		/**
		 * Performs the actual detour. Code borrowed from the CCL.
		 **/
		public static unsafe bool TryIndepentDetour(MethodInfo source, MethodInfo destination) {
			// check if already detoured, if so - error out.
			if (detours.ContainsKey(source)) {
				return false;
			}

			// check for destination type fields, return type and argument compatibility
			DetourValidator.IsValidDetourPair(source, destination);
			// show detouring errors as warnings in the interest of compatibility
			// TODO: convert warnings to errors on next Rimworld release
			var warning = DetourValidator.GetLastError();
			if (warning != null) {
				HugsLibController.Logger.Warning(warning);
			}

			// do the detour, and add it to the list 
			detours.Add(source, destination);

			if (IntPtr.Size == sizeof (Int64)) {
				// 64-bit systems use 64-bit absolute address and jumps
				// 12 byte destructive

				// Get function pointers
				long Source_Base = source.MethodHandle.GetFunctionPointer().ToInt64();
				long Destination_Base = destination.MethodHandle.GetFunctionPointer().ToInt64();

				// Native source address
				byte* Pointer_Raw_Source = (byte*) Source_Base;

				// Pointer to insert jump address into native code
				long* Pointer_Raw_Address = (long*) (Pointer_Raw_Source + 0x02);

				// Insert 64-bit absolute jump into native code (address in rax)
				// mov rax, immediate64
				// jmp [rax]
				*(Pointer_Raw_Source + 0x00) = 0x48;
				*(Pointer_Raw_Source + 0x01) = 0xB8;
				*Pointer_Raw_Address = Destination_Base; // ( Pointer_Raw_Source + 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 )
				*(Pointer_Raw_Source + 0x0A) = 0xFF;
				*(Pointer_Raw_Source + 0x0B) = 0xE0;
			} else {
				// 32-bit systems use 32-bit relative offset and jump
				// 5 byte destructive

				// Get function pointers
				int Source_Base = source.MethodHandle.GetFunctionPointer().ToInt32();
				int Destination_Base = destination.MethodHandle.GetFunctionPointer().ToInt32();

				// Native source address
				byte* Pointer_Raw_Source = (byte*) Source_Base;

				// Pointer to insert jump address into native code
				int* Pointer_Raw_Address = (int*) (Pointer_Raw_Source + 1);

				// Jump offset (less instruction size)
				int offset = (Destination_Base - Source_Base) - 5;

				// Insert 32-bit relative jump into native code
				*Pointer_Raw_Source = 0xE9;
				*Pointer_Raw_Address = offset;
			}

			// done!
			return true;
		}

		internal static bool CompatibleDetourWithExceptions(MethodInfo source, MethodInfo destination) {
			if (detours.ContainsKey(source)) {
				throw new Exception(String.Format("{0} was already detoured to {1}.", source.FullName(), destination.FullName()));
			}
			return TryCompatibleDetour(source, destination);
		}

		internal static void ThrowClearerDetourException(Exception e, MemberInfo sourceInfo, MemberInfo targetInfo, string detourMode) {
			// do a proper breakdown of the cause of the exception, including source, target, and target assembly
			var message = string.Format("Failed to detour {0} {1}", detourMode, DetourPairToString(sourceInfo, targetInfo));
			
			throw new Exception(message, e);
		}

		internal static string DetourPairToString(MemberInfo sourceInfo, MemberInfo targetInfo) {
			const string nullRefLabel = "[not found]";
			var sourceDeclaringType = sourceInfo != null && sourceInfo.DeclaringType != null ? sourceInfo.DeclaringType.Name : "null";
			var targetDeclaringType = targetInfo != null && targetInfo.DeclaringType != null ? targetInfo.DeclaringType.Name : "null";
			var result = string.Format("{0} to {1}",
				sourceInfo != null ? sourceDeclaringType + "." + sourceInfo.Name : nullRefLabel,
				targetInfo != null ? targetDeclaringType + "." + targetInfo.Name : nullRefLabel);
			if (targetInfo != null && targetInfo.DeclaringType != null) {
				result += string.Format(" (assembly: {0})", targetInfo.DeclaringType.Assembly.GetName().Name);
			}
			return result;
		}
	}
}