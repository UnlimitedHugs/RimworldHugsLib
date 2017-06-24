using System;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace HugsLib.Utils {
	/// <summary>
	/// Adds a hash to a manually instantiated def to avoid def collisions.
	/// </summary>
	public static class InjectedDefHasher {
        private static Action<Def, Type> GiveShortHash;
				
		internal static void PrepareReflection() {
            Type[] parametersSignature = new[] {typeof(Def), typeof(Type)};
						
			MethodInfo giveShortHashMethod = typeof(ShortHashGiver).GetMethod("GiveShortHash", BindingFlags.NonPublic | BindingFlags.Static, null, parametersSignature, null);
			if (giveShortHashMethod == null) {
				HugsLibController.Logger.Error("Failed to reflect ShortHashGiver.GiveShortHash");
			    return;
			}
			
			DynamicMethod dm = new DynamicMethod("__GiveShortHash_Dynamic", null, parametersSignature, typeof(ShortHashGiver));		// logically associate new method with 'ShortHashGiver' type
            ILGenerator IL = dm.GetILGenerator();
            IL.Emit(OpCodes.Ldarg_0);                               // 'Def' argument
            IL.Emit(OpCodes.Ldarg_1);                               // 'Type' argument
            IL.Emit(OpCodes.Call, giveShortHashMethod);             // call 'ShortHashGiver.GiveShortHash'
            IL.Emit(OpCodes.Ret);                                   // return
			
			GiveShortHash = (Action<Def, Type>) dm.CreateDelegate(typeof(Action<Def, Type>));	
		}			
		

		/// <summary>
		/// Give a short hash to a def created at runtime.
		/// Short hashes are used for proper saving of defs in compressed maps within a save file.
		/// </summary>
		/// <param name="newDef"></param>
		/// <param name="defType">The type of defs your def will be saved with. For example, use typeof(ThingDef) if your def extends ThingDef.</param>
		public static void GiveShortHasToDef(Def newDef, Type defType) {
			if(GiveShortHash == null) throw new Exception("Hasher not initalized");
		    GiveShortHash(newDef, defType);
		}
	}
}