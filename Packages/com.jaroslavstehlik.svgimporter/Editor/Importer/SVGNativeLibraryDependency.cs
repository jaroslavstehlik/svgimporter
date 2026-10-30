// Copyright (C) 2026 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using System.IO;
using UnityEditor;
using UnityEngine;

namespace SVGImporter
{
    /// <summary>
    /// A custom asset database dependency on the native library that imports SVG files:
    /// SVGFileImporter depends on it, so every .svg file reimports when the library changes.
    /// </summary>
    [InitializeOnLoad]
    internal static class SVGNativeLibraryDependency
    {
        // The name of earlier versions, so that the dependency doesn't change.
        public const string name = "SVGImporter/ImportEngine";

        static SVGNativeLibraryDependency()
        {
            Register();
        }

        /// <summary>Records the current library. Call AssetDatabase.Refresh() afterwards to reimport.</summary>
        public static void Register()
        {
            AssetDatabase.RegisterCustomDependency(name, Hash128.Compute(NativeLibraryHash()));
        }

        static string NativeLibraryHash()
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(SVGAsset).Assembly);
            if (package == null) return "";
#if UNITY_EDITOR_OSX
            string library = Path.Combine(package.resolvedPath, "Runtime/Plugins/macOS/libsvgimporter_core.dylib");
#elif UNITY_EDITOR_WIN
            string library = Path.Combine(package.resolvedPath, "Runtime/Plugins/Windows/x86_64/svgimporter_core.dll");
#else
            string library = Path.Combine(package.resolvedPath, "Runtime/Plugins/Linux/x86_64/libsvgimporter_core.so");
#endif
            return library != null && File.Exists(library) ? Hash128.Compute(File.ReadAllBytes(library)).ToString() : "";
        }
    }
}
