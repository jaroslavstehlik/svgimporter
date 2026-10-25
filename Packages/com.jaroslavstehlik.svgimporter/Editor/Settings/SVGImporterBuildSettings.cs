// Copyright (C) 2026 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SVGImporter
{
    /// <summary>
    /// Players read SVGImporterSettings.Get, which the Editor fills from the project settings. For a
    /// build, the settings players need are written to a temporary asset that is preloaded with the
    /// player, and removed again after the build.
    /// </summary>
    internal class SVGImporterBuildSettings : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        internal const string assetPath = "Assets/SVGImporterBuildSettings.asset";

        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            Remove();
            var settings = ScriptableObject.CreateInstance<SVGImporterSettings>();
            settings.defaultAntialiasingWidth = SVGImporterProjectSettings.instance.antialiasingWidth;
            settings._isPlayerSettings = true;
            AssetDatabase.CreateAsset(settings, assetPath);
            PlayerSettings.SetPreloadedAssets(PlayerSettings.GetPreloadedAssets().Where(a => a != null).Append(settings).ToArray());
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            Remove();
        }

        // A failed build doesn't call OnPostprocessBuild: remove the asset when the Editor loads.
        [InitializeOnLoadMethod]
        static void RemoveAfterFailedBuild()
        {
            EditorApplication.delayCall += Remove;
        }

        static void Remove()
        {
            var settings = AssetDatabase.LoadAssetAtPath<SVGImporterSettings>(assetPath);
            if (settings == null) return;
            PlayerSettings.SetPreloadedAssets(PlayerSettings.GetPreloadedAssets().Where(a => a != null && a != settings).ToArray());
            AssetDatabase.DeleteAsset(assetPath);
        }
    }
}
