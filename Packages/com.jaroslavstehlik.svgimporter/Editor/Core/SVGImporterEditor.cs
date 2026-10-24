// Copyright (C) 2015 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

namespace SVGImporter
{
    public class SVGImporterEditor
    {
        // Packages installed from git are read-only, so the settings live in the project.
        // Keeping them in a Resources folder makes them available in builds too.
        protected const string settingsPath = "Assets/SVG Importer/Resources/" + SVGImporterSettings.resourceName + ".asset";

        [MenuItem("Window/SVG Importer/Settings")]
        static public void ShowSettings () {
            Selection.activeObject = GetOrCreateSettingsAsset();
        }

        /// <summary>
        /// The project's settings asset, or the default settings when the project has none.
        /// </summary>
        public static SVGImporterSettings settings
        {
            get {
                SVGImporterSettings asset = FindSettingsAsset();
                return asset != null ? asset : SVGImporterSettings.Get;
            }
        }

        protected static SVGImporterSettings FindSettingsAsset()
        {
            string[] guids = AssetDatabase.FindAssets("t:SVGImporterSettings", new[] { "Assets" });
            if(guids == null || guids.Length == 0)
                return null;
            return AssetDatabase.LoadAssetAtPath<SVGImporterSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        protected static SVGImporterSettings GetOrCreateSettingsAsset()
        {
            SVGImporterSettings asset = FindSettingsAsset();
            if(asset != null)
                return asset;

            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
            AssetDatabase.Refresh();
            asset = ScriptableObject.CreateInstance<SVGImporterSettings>();
            AssetDatabase.CreateAsset(asset, settingsPath);
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}
