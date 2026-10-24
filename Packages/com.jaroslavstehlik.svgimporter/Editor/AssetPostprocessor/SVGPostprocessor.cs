// Copyright (C) 2015 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SVGImporter
{
    /// <summary>
    /// Assigns SVGFileImporter to SVG files added to the project. Unity's own Vector Graphics
    /// importer is the default importer for .svg, so SVGFileImporter is an override that has
    /// to be set per file.
    /// </summary>
    public class SVGPostprocessor : AssetPostprocessor
    {
        // New .svg files, collected in OnPreprocessAsset: only there does Unity tell whether
        // a file has import settings yet. Files that already have settings (existing files, or
        // files someone switched back to Unity's importer) are left alone.
        static readonly HashSet<string> s_NewSVGFiles = new HashSet<string>();

        void OnPreprocessAsset()
        {
            if(assetImporter.importSettingsMissing && IsProjectSVGFile(assetPath) && !(assetImporter is SVGFileImporter))
            {
                s_NewSVGFiles.Add(assetPath);
            }
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if(s_NewSVGFiles.Count == 0)
                return;

            var newFiles = new List<string>(s_NewSVGFiles);
            s_NewSVGFiles.Clear();

            if(!SVGImporterEditor.settings.importNewSVGFiles)
                return;

            foreach(string path in newFiles)
            {
                UseSVGImporter(path);
            }
        }

        /// <summary>Only files in the project's Assets folder: packages ship read-only SVG files.</summary>
        internal static bool IsProjectSVGFile(string path)
        {
            return path.StartsWith("Assets/", StringComparison.Ordinal) && path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Switches an .svg file to SVG Importer, with the project's default import settings.</summary>
        internal static bool UseSVGImporter(string path)
        {
            if(AssetImporter.GetAtPath(path) is SVGFileImporter)
                return true;

            AssetDatabase.SetImporterOverride<SVGFileImporter>(path);
            var importer = AssetImporter.GetAtPath(path) as SVGFileImporter;
            if(importer == null)
            {
                Debug.LogWarning("SVG Importer: could not assign the importer to " + path);
                return false;
            }

            importer.ApplyDefaults(SVGImporterEditor.settings);
            // SaveAndReimport only writes the settings of an importer marked as changed.
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            return true;
        }

        const string k_UseSVGImporterMenu = "Assets/SVG Importer/Import with SVG Importer";

        [MenuItem(k_UseSVGImporterMenu, false, 1100)]
        static void UseSVGImporterForSelection()
        {
            foreach(string guid in Selection.assetGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if(IsProjectSVGFile(path))
                    UseSVGImporter(path);
            }
        }

        [MenuItem(k_UseSVGImporterMenu, true)]
        static bool UseSVGImporterForSelectionValidate()
        {
            foreach(string guid in Selection.assetGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if(IsProjectSVGFile(path) && !(AssetImporter.GetAtPath(path) is SVGFileImporter))
                    return true;
            }
            return false;
        }
    }
}
