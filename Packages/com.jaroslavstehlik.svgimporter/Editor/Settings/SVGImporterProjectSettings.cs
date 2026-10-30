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
    /// <summary>Import settings given to SVG files when SVG Importer takes them over.</summary>
    [Serializable]
    public class SVGImportDefaults
    {
        public SVGAssetFormat format = SVGAssetFormat.Transparent;
        public SVGUseGradients useGradients = SVGUseGradients.Auto;
        public bool antialiasing = true;
        public SVGMeshCompression meshCompression = SVGMeshCompression.Off;
        public bool optimizeMesh = true;
        public float scale = 0.01f;
        public int verticesPerMeter = 1000;
        public float depthOffset = 0.01f;
        public bool compressDepth = true;
        public bool customPivotPoint = false;
        public Vector2 pivotPoint = new Vector2(0.5f, 0.5f);
        public bool generateCollider = false;
        public bool generateNormals = false;
        public bool generateTangents = false;
        public bool keepSVGFile = false;
        public bool useLayers = false;
        public bool ignoreSVGCanvas = true;
        public bool exactGroupOpacity = true;
    }

    /// <summary>
    /// SVG Importer's project settings, in ProjectSettings/SVGImporterSettings.asset (Edit > Project
    /// Settings > SVG Importer). Editor only: the one value players need, the antialiasing width,
    /// goes into builds through SVGImporterBuildSettings.
    ///
    /// The file is read here and nowhere else. Changes, including ones made outside Unity (a version
    /// control update), are picked up when the Editor regains focus, and update the import
    /// dependency so that SVG files reimport when the import engine changes.
    /// </summary>
    public sealed class SVGImporterProjectSettings : ScriptableObject
    {
        internal const string path = "ProjectSettings/SVGImporterSettings.asset";

        [SerializeField] bool _importNewSVGFiles = true;
        // Settings files of 2.0 previews have an engine; there is one engine now.
#pragma warning disable CS0414
        [SerializeField, HideInInspector] SVGImportEngine _importEngine = SVGImportEngine.Native;
#pragma warning restore CS0414
        [SerializeField] float _antialiasingWidth = 2f;
        [SerializeField] SVGImportDefaults _defaults = new SVGImportDefaults();

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

        /// <summary>Settings for SVG files that SVG Importer takes over. Call Save() after changing them.</summary>
        public SVGImportDefaults defaults
        {
            get { return _defaults; }
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
#pragma warning disable 618 // importNewSVGFiles is obsolete.
            string assetPath = AssetDatabase.FindAssets("t:SVGImporterSettings", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => p != SVGImporterBuildSettings.assetPath);
            SVGImporterSettings old = assetPath == null ? null : AssetDatabase.LoadAssetAtPath<SVGImporterSettings>(assetPath);
            if (old == null) return false;

            s_Instance._importNewSVGFiles = old.importNewSVGFiles;
            s_Instance._antialiasingWidth = old.defaultAntialiasingWidth;
            s_Instance._defaults = new SVGImportDefaults {
                format = old.defaultSVGFormat, useGradients = old.defaultUseGradients, antialiasing = old.defaultAntialiasing,
                meshCompression = old.defaultMeshCompression, optimizeMesh = old.defaultOptimizeMesh, scale = old.defaultScale,
                verticesPerMeter = old.defaultVerticesPerMeter, depthOffset = old.defaultDepthOffset, compressDepth = old.defaultCompressDepth,
                customPivotPoint = old.defaultCustomPivotPoint, pivotPoint = old.defaultPivotPoint, generateCollider = old.defaultGenerateCollider,
                generateNormals = old.defaultGenerateNormals, generateTangents = old.defaultGenerateTangents, keepSVGFile = old.defaultKeepSVGFile,
                useLayers = old.defaultUseLayers, ignoreSVGCanvas = old.defaultIgnoreSVGCanvas,
            };
#pragma warning restore 618
            InternalEditorUtility.SaveToSerializedFileAndForget(new UnityEngine.Object[] { s_Instance }, path, true);
            s_FileTime = File.GetLastWriteTimeUtc(path);
            Debug.Log("SVG Importer: the settings in " + assetPath + " now live in Project Settings > SVG Importer ("
                + path + "). The old asset isn't used any more and can be deleted.");
            return true;
        }

        static void Apply()
        {
            SVGImportEngineDependency.Register();
            SVGImporterSettings.Get.defaultAntialiasingWidth = s_Instance._antialiasingWidth;
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
