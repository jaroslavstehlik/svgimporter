// Copyright (C) 2026 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SVGImporter
{
    /// <summary>
    /// SVG Importer's project settings, in ProjectSettings/SVGImporterSettings.asset (Edit > Project
    /// Settings > SVG Importer). Editor only: the one value players need, the antialiasing width,
    /// goes into builds through SVGImporterBuildSettings.
    ///
    /// The file is read here and nowhere else. Changes, including ones made outside Unity (a version
    /// control update), are picked up when the Editor regains focus.
    /// </summary>
    internal sealed class SVGImporterProjectSettings : ScriptableObject
    {
        internal const string path = "ProjectSettings/SVGImporterSettings.asset";

        [SerializeField] bool _importNewSVGFiles = true;
        [SerializeField] float _antialiasingWidth = 2f;
        [SerializeField] SVGImportOptions _defaults = SVGImportOptions.Default;

        /// <summary>
        /// Import SVG files added to the project with SVG Importer. Off: they keep Unity's own SVG
        /// importer, for example for UI Toolkit.
        /// </summary>
        public bool importNewSVGFiles
        {
            get { return _importNewSVGFiles; }
            set { _importNewSVGFiles = value; Save(); }
        }

        /// <summary>Width of the antialiasing fringe in pixels, in the Editor and in builds.</summary>
        public float antialiasingWidth
        {
            get { return _antialiasingWidth; }
            set { _antialiasingWidth = value; Save(); }
        }

        /// <summary>Settings for SVG files that SVG Importer takes over.</summary>
        public SVGImportOptions defaults
        {
            get { return _defaults; }
            set { _defaults = value; Save(); }
        }

        static SVGImporterProjectSettings s_Instance;
        static DateTime s_FileTime;

        public static SVGImporterProjectSettings instance
        {
            get {
                if (s_Instance == null) Load();
                return s_Instance;
            }
        }

        /// <summary>Raised after the settings are saved or reloaded.</summary>
        public static event Action changed;

        /// <summary>Writes the settings to ProjectSettings and applies them.</summary>
        public void Save()
        {
            if (this != s_Instance) return;
            InternalEditorUtility.SaveToSerializedFileAndForget(new UnityEngine.Object[] { this }, path, true);
            s_FileTime = File.GetLastWriteTimeUtc(path);
            Apply();
        }

        static void Load()
        {
            // The settings object survives script reloads (DontSave); reuse it.
            s_Instance = Resources.FindObjectsOfTypeAll<SVGImporterProjectSettings>().FirstOrDefault();
            if (s_Instance == null)
            {
                s_Instance = CreateInstance<SVGImporterProjectSettings>();
                s_Instance.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            }

            if (File.Exists(path))
            {
                ReadFile();
            }
            else if (!MigrateFromAsset())
            {
                // No settings yet: the defaults, written on the first change.
                s_FileTime = default(DateTime);
            }
            Apply();
        }

        static void ReadFile()
        {
            UnityEngine.Object[] loaded = InternalEditorUtility.LoadSerializedFileAndForget(path);
            var settings = loaded.OfType<SVGImporterProjectSettings>().FirstOrDefault();
            if (settings != null) EditorUtility.CopySerialized(settings, s_Instance);
            foreach (UnityEngine.Object obj in loaded) DestroyImmediate(obj);
            s_FileTime = File.GetLastWriteTimeUtc(path);
        }

        /// <summary>
        /// SVG Importer 2.0 kept its settings in an asset in a Resources folder. Copies them here
        /// once; the asset is left in place, and the Console says it can be deleted.
        /// </summary>
        static bool MigrateFromAsset()
        {
            string assetPath = AssetDatabase.FindAssets("t:SVGImporterSettings", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => p != SVGImporterBuildSettings.assetPath);
            SVGImporterSettings old = assetPath == null ? null : AssetDatabase.LoadAssetAtPath<SVGImporterSettings>(assetPath);
            if (old == null) return false;

            s_Instance._importNewSVGFiles = old.importNewSVGFiles;
            s_Instance._antialiasingWidth = old.softEdgeWidth;
            s_Instance._defaults = new SVGImportOptions {
                format = old.defaultSVGFormat, useGradients = old.defaultUseGradients, softEdges = old.defaultAntialiasing,
                meshCompression = old.defaultMeshCompression, optimizeMesh = old.defaultOptimizeMesh, scale = old.defaultScale,
                verticesPerMeter = old.defaultVerticesPerMeter, depthOffset = old.defaultDepthOffset, compressDepth = old.defaultCompressDepth,
                customPivotPoint = old.defaultCustomPivotPoint, pivotPoint = old.defaultPivotPoint, generateCollider = old.defaultGenerateCollider,
                generateNormals = old.defaultGenerateNormals, generateTangents = old.defaultGenerateTangents, keepSVGFile = old.defaultKeepSVGFile,
                useLayers = old.defaultUseLayers, ignoreSVGCanvas = old.defaultIgnoreSVGCanvas,
            };
            InternalEditorUtility.SaveToSerializedFileAndForget(new UnityEngine.Object[] { s_Instance }, path, true);
            s_FileTime = File.GetLastWriteTimeUtc(path);
            Debug.Log("SVG Importer: the settings in " + assetPath + " now live in Project Settings > SVG Importer ("
                + path + "). The old asset isn't used any more and can be deleted.");
            return true;
        }

        static void Apply()
        {
            SVGNativeLibraryDependency.Register();
            SVGImporterSettings.Get.softEdgeWidth = s_Instance._antialiasingWidth;
            if (changed != null) changed();
        }

        [InitializeOnLoadMethod]
        static void WatchFile()
        {
            EditorApplication.focusChanged += focused => { if (focused) ReloadIfChanged(); };
        }

        /// <summary>
        /// Reads the file again if it changed outside Unity, for example by a version control update.
        /// </summary>
        internal static void ReloadIfChanged()
        {
            if (s_Instance == null) return;
            DateTime fileTime = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : default(DateTime);
            if (fileTime == s_FileTime) return;
            if (File.Exists(path)) ReadFile();
            Apply();
        }

        /// <summary>Forgets the loaded settings, so the next access loads them again (tests).</summary>
        internal static void Unload()
        {
            if (s_Instance != null) DestroyImmediate(s_Instance);
            s_Instance = null;
        }
    }
}
