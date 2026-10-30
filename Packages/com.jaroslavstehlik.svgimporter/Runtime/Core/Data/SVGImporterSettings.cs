// Copyright (C) 2015 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using UnityEngine;
using System.Collections;

namespace SVGImporter
{
    /// <summary>
    /// Which implementation parses and tessellates SVG files. There is one, the native core (usvg
    /// and lyon, see DESIGN.md); the C# parser of 1.x and early 2.0 previews was removed. Kept so
    /// that assets and settings that recorded an engine still load.
    /// </summary>
    public enum SVGImportEngine
    {
        Legacy,
        Native,
    }

    /// <summary>
    /// Import options for SVGAsset.Load, and the settings players use at runtime.
    ///
    /// The project's settings are edited in Edit > Project Settings > SVG Importer and stored in
    /// ProjectSettings (SVGImporterProjectSettings, Editor only). Get returns the runtime part of
    /// them: in the Editor it is kept up to date from the project settings, and builds include it
    /// as a preloaded asset (SVGImporterBuildSettings).
    /// </summary>
    public class SVGImporterSettings : ScriptableObject 
    {
        [System.Obsolete("Settings are no longer loaded from Resources; see Edit > Project Settings > SVG Importer.")]
        public const string resourceName = "svgImporterSettings";

        // True for the copy of the project settings a build preloads.
        [SerializeField, HideInInspector] internal bool _isPlayerSettings;

        protected static SVGImporterSettings _instance;

        /// <summary>The project's runtime settings.</summary>
        public static SVGImporterSettings Get
        {
            get {
                if(_instance == null)
                {
                    _instance = ScriptableObject.CreateInstance<SVGImporterSettings>();
                    _instance.hideFlags = HideFlags.DontSave;
                }

                return _instance;
            }
        }

        void OnEnable()
        {
            // Preloaded assets are loaded when the player starts.
            if(_isPlayerSettings) _instance = this;
        }

        public static void UpdateAntialiasing()
        {
            Shader.SetGlobalFloat("SVG_ANTIALIASING_WIDTH", Get.defaultAntialiasingWidth);
        }

        protected static string _version = "2.0.0";
        public static string version
        {
            get {
                return _version;
            }
        }

        [System.Obsolete("Use SVGImporterProjectSettings.instance.importNewSVGFiles (Editor).")]
        [HideInInspector] public bool importNewSVGFiles = true;

        [System.Obsolete("SVG Importer has one import engine, the native core.")]
        [HideInInspector] public SVGImportEngine importEngine = SVGImportEngine.Native;

        public SVGAssetFormat defaultSVGFormat = SVGAssetFormat.Transparent;
        public SVGUseGradients defaultUseGradients = SVGUseGradients.Auto;
        public bool defaultAntialiasing = true;
        public float defaultAntialiasingWidth = 2f;
        public SVGMeshCompression defaultMeshCompression = SVGMeshCompression.Off;
        public int defaultVerticesPerMeter = 1000;
        public float defaultScale = 0.01f;
        public float defaultDepthOffset = 0.01f;
        public bool defaultCompressDepth = true;
        public bool defaultCustomPivotPoint = false;
        public Vector2 defaultPivotPoint = new Vector2(0.5f, 0.5f);
        public bool defaultGenerateCollider = false;
        public bool defaultKeepSVGFile = false;
        public bool defaultUseLayers = false;
        public bool defaultIgnoreSVGCanvas = true;
        public bool defaultExactGroupOpacity = false;
        public bool defaultOptimizeMesh = true;
        public bool defaultGenerateNormals = false;
        public bool defaultGenerateTangents = false;
        [System.Obsolete("Not used: the asset icon is SVG Importer's own.")]
        [HideInInspector] public Texture2D defaultSVGIcon;

        public bool ignoreImportExceptions = true;
    }

}