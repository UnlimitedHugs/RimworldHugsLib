// ModContentPackUtility.cs
// Copyright Karel Kroeze, 2020-2020

using System;
using System.Linq;
using HugsLib.Core;
using Verse;

namespace HugsLib.Utils
{
    public static class ModContentPackUtility
    {
        public static Version GetVersion( this ModContentPack pack, ModBase mod = null )
        {
            // get from version file
            var versionFile = VersionFile.TryParse( pack );
            if ( versionFile != null && versionFile.OverrideVersion != null ) return versionFile.OverrideVersion;

            // get from manifest file
            var manifest = ManifestFile.TryParse( pack );
            if ( manifest != null && manifest.Version != null ) return manifest.Version;

            if ( mod != null )
            {
                // get highest from mod assembly
                return mod.VersionInfo.HighestVersion;
            }
            else
            {
                // get highest from any assemblies inheriting modBase in pack
                var modBaseVersions = pack.assemblies.loadedAssemblies
                                          .Where( a => a.GetTypes().Any( t => typeof( ModBase ).IsAssignableFrom( t ) ) )
                                          .Select( a => AssemblyVersionInfo.ReadModAssembly( a, pack ).HighestVersion );
                if ( modBaseVersions.Any() ) return modBaseVersions.Max();
            }

            // get from last assembly in pack, the assumption is that libs come first
            if ( pack.assemblies.loadedAssemblies.Any() )
                return AssemblyVersionInfo.ReadModAssembly( pack.assemblies.loadedAssemblies.Last(), pack ).HighestVersion;

            // fail.
            return new Version( int.MinValue, int.MinValue );
        }
    }
}