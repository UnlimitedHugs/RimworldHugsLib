using System;
using Verse;

// ReSharper disable once CheckNamespace
namespace HugsLib {
	/// <summary>
	/// Used to indicate that a <see cref="ModBase"/> type should be instantiated at the earliest moment possible.
	/// Specifically, when <see cref="Mod"/> classes are instantiated (see <see cref="PlayDataLoader"/>.DoPlayLoad()).
	/// If <see cref="ModBase.HarmonyAutoPatch"/> is true, Harmony patching will also happen at that time.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class EarlyInitAttribute : Attribute {
	}
}