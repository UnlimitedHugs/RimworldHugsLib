using System.Reflection;
using UnityEngine;
using Verse;

// suppress unassigned field warning
#pragma warning disable 649 

namespace HugsLib.Core {
	/// <summary>
	/// Loads and stores textures from the HugsLib /Textures folder
	/// </summary>
	[StaticConstructorOnStartup]
	internal static class HugsLibTextures {
		public static Texture2D quickstartIcon = null!;
		public static Texture2D HLMenuIcon = null!;
		public static Texture2D HLMenuIconPlus = null!;
		public static Texture2D HLInfoIcon = null!;
		
		static HugsLibTextures() {
			foreach (var fieldInfo in typeof(HugsLibTextures).GetFields(BindingFlags.Public | BindingFlags.Static)) {
				fieldInfo.SetValue(null, ContentFinder<Texture2D>.Get(fieldInfo.Name));
			}
		}
	}
}