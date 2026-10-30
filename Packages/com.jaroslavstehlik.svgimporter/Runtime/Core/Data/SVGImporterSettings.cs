// Copyright (C) 2015 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using UnityEngine;
using UnityEngine.Serialization;

namespace SVGImporter
{
    /// <summary>
    /// The project settings players use at runtime: the soft edge width.
    ///
    /// The project's settings are edited in Edit > Project Settings > SVG Importer and stored in
    /// ProjectSettings (SVGImporterProjectSettings, Editor only). In the Editor, Get is kept up to
    /// date from them; builds preload a copy (SVGImporterBuildSettings).
    /// </summary>
    public sealed class SVGImporterSettings : ScriptableObject
    {
        // True for the copy of the project settings a build preloads.
        [SerializeField, HideInInspector] internal bool _isPlayerSettings;

        /// <summary>Width of the soft edge in half pixels, the unit of the shaders.</summary>
        [FormerlySerializedAs("defaultAntialiasingWidth")]
        [SerializeField] internal float softEdgeWidth = 2f;

        static SVGImporterSettings s_Instance;

        internal static SVGImporterSettings Get
        {
            get {
                if(s_Instance == null)
                {
                    s_Instance = CreateInstance<SVGImporterSettings>();
                    s_Instance.hideFlags = HideFlags.DontSave;
                }
                return s_Instance;
            }
        }

        void OnEnable()
        {
            // Preloaded assets are loaded when the player starts.
            if(_isPlayerSettings) s_Instance = this;
        }

        internal static void UpdateAntialiasing()
        {
            Shader.SetGlobalFloat("SVG_ANTIALIASING_WIDTH", Get.softEdgeWidth);
        }

        // Settings of SVG Importer 1.x and 2.0 previews, which kept them in an asset in a Resources
        // folder. Read once by the Editor to move them to the project settings.
        [SerializeField, HideInInspector] internal bool importNewSVGFiles = true;
        [SerializeField, HideInInspector] internal SVGAssetFormat defaultSVGFormat = SVGAssetFormat.Transparent;
        [SerializeField, HideInInspector] internal SVGUseGradients defaultUseGradients = SVGUseGradients.Auto;
        [SerializeField, HideInInspector] internal bool defaultAntialiasing = true;
        [SerializeField, HideInInspector] internal SVGMeshCompression defaultMeshCompression = SVGMeshCompression.Off;
        [SerializeField, HideInInspector] internal int defaultVerticesPerMeter = 1000;
        [SerializeField, HideInInspector] internal float defaultScale = 0.01f;
        [SerializeField, HideInInspector] internal float defaultDepthOffset = 0.01f;
        [SerializeField, HideInInspector] internal bool defaultCompressDepth = true;
        [SerializeField, HideInInspector] internal bool defaultCustomPivotPoint = false;
        [SerializeField, HideInInspector] internal Vector2 defaultPivotPoint = new Vector2(0.5f, 0.5f);
        [SerializeField, HideInInspector] internal bool defaultGenerateCollider = false;
        [SerializeField, HideInInspector] internal bool defaultKeepSVGFile = false;
        [SerializeField, HideInInspector] internal bool defaultUseLayers = false;
        [SerializeField, HideInInspector] internal bool defaultIgnoreSVGCanvas = true;
        [SerializeField, HideInInspector] internal bool defaultOptimizeMesh = true;
        [SerializeField, HideInInspector] internal bool defaultGenerateNormals = false;
        [SerializeField, HideInInspector] internal bool defaultGenerateTangents = false;
    }
}
