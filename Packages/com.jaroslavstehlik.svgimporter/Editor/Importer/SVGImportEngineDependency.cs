// Copyright (C) 2026 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using System.IO;
using UnityEditor;
using UnityEngine;

namespace SVGImporter
{
    /// <summary>
    /// A custom asset database dependency on the engine that imports SVG files: SVGFileImporter
    /// depends on it, so every .svg file reimports when it changes. It records the engine actually
    /// used on this Editor (Native falls back to Legacy where the native core isn't available) and,
    /// for Native, the native library's content, so a new library reimports too.
    /// </summary>
    [InitializeOnLoad]
    internal static class SVGImportEngineDependency
    {
        public const string name = "SVGImporter/ImportEngine";

        static SVGImportEngineDependency()
        {
            Register();
        }

        /// <summary>Records the current engine. Call AssetDatabase.Refresh() afterwards to reimport.</summary>
        public static void Register()
        {
            SVGImportEngine engine = SVGImporterProjectSettings.instance.effectiveImportEngine;
            string key = engine.ToString();
            if (engine == SVGImportEngine.Native)
                key += " " + NativeLibraryHash();
            AssetDatabase.RegisterCustomDependency(name, Hash128.Compute(key));
        }

        static string NativeLibraryHash()
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(SVGAsset).Assembly);
            if (package == null) return "";
#if UNITY_EDITOR_OSX
            string library = Path.Combine(package.resolvedPath, "Runtime/Plugins/macOS/libsvgimporter_core.dylib");
#else
            string library = null;
#endif
            return library != null && File.Exists(library) ? Hash128.Compute(File.ReadAllBytes(library)).ToString() : "";
        }
    }
}
