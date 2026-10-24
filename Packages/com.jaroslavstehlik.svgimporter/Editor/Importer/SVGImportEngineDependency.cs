// Copyright (C) 2026 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using UnityEditor;
using UnityEngine;

namespace SVGImporter
{
    /// <summary>
    /// A custom asset database dependency on the Import Engine setting: SVGFileImporter depends on
    /// it, so changing the engine reimports every .svg file.
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
            AssetDatabase.RegisterCustomDependency(name, Hash128.Compute(SVGImporterEditor.settings.importEngine.ToString()));
        }
    }
}
