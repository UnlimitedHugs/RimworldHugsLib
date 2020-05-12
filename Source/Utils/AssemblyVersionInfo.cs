using System;
using System.Diagnostics;
using System.Reflection;
using Verse;

namespace HugsLib.Utils {
	/// <summary>
	/// Provides a convenient way to read, compare and print out the assembly version and file version of assemblies.
	/// </summary>
	public class AssemblyVersionInfo {
		/// <summary>
		/// Tries to read the file assembly version in addition to the already known assembly version.
		/// </summary>
		/// <param name="assembly">The assembly to read</param>
		/// <param name="overrideLocation">The full path to the assembly file, if <see cref="Assembly.Location"/> is not set</param>
		/// <returns>An <see cref="AssemblyVersionInfo"/> with only AssemblyVersion set if an exception was encountered</returns>
		public static AssemblyVersionInfo ReadAssembly(Assembly assembly, string overrideLocation = null) {
			if (assembly == null) throw new ArgumentNullException(nameof(assembly));
			try {
				var assemblyFilePath = overrideLocation ?? assembly.Location;
				var fileInfo = FileVersionInfo.GetVersionInfo(assemblyFilePath);
				return new AssemblyVersionInfo(
					assembly.GetName().Version,
					new Version(fileInfo.FileVersion)
				);
			} catch (Exception) {
				return new AssemblyVersionInfo(assembly.GetName().Version, null);
			}
		}

		/// <summary>
		/// Reads assembly version information for a mod assembly.
		/// Mod assemblies require special treatment, since they are loaded from byte arrays and their <see cref="Assembly.Location"/> is null.
		/// </summary>
		/// <param name="assembly">The assembly to read</param>
		/// <param name="contentPack">The content pack the assembly was loaded from</param>
		/// <returns>See <see cref="ReadAssembly"/></returns>
		public static AssemblyVersionInfo ReadModAssembly(Assembly assembly, ModContentPack contentPack) {
			if (assembly == null) throw new ArgumentNullException(nameof(assembly));
			var fileHandle = HugsLibUtility.TryGetModAssemblyFileInfo(assembly.GetName().Name, contentPack);
			var fullFilePath = fileHandle?.FullName;
			return ReadAssembly(assembly, fullFilePath);
		}

		public readonly Version AssemblyVersion;
		public readonly Version AssemblyFileVersion;

		public Version HighestVersion {
			get { return AssemblyFileVersion != null && AssemblyFileVersion > AssemblyVersion ? AssemblyFileVersion : AssemblyVersion; }
		}

		public AssemblyVersionInfo(Version assemblyVersion, Version assemblyFileVersion) {
			AssemblyVersion = assemblyVersion ?? throw new ArgumentNullException(nameof(assemblyVersion));
			AssemblyFileVersion = assemblyFileVersion;
		}

		public override string ToString() {
			if (AssemblyFileVersion == null) {
				return $"{AssemblyVersion.ToSemanticString()} [no FileVersionInfo]";
			} else if(AssemblyFileVersion == AssemblyVersion) {
				return AssemblyVersion.ToSemanticString();
			} else {
				return $"av:{AssemblyVersion.ToSemanticString()},fv:{AssemblyFileVersion.ToSemanticString()}";
			}
		}
	}
}