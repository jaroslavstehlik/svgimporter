// Copyright (C) 2015 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using UnityEngine;
using System.Collections;

namespace SVGImporter
{
    public class SVGImporterSettings : ScriptableObject 
    {
        public const string resourceName = "svgImporterSettings";

        protected static SVGImporterSettings _instance;
        /// <summary>
        /// The project's settings asset (any Resources/svgImporterSettings.asset),
        /// or the default settings when the project has none.
        /// </summary>
        public static SVGImporterSettings Get
        {
            get {
                if(_instance == null)
                {
                    _instance = Resources.Load<SVGImporterSettings>(resourceName);
                    if(_instance == null)
                    {
                        _instance = ScriptableObject.CreateInstance<SVGImporterSettings>();
                        _instance.hideFlags = HideFlags.DontSave;
                    }
                }

                return _instance;
            }
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
        public bool defaultOptimizeMesh = true;
        public bool defaultGenerateNormals = false;
        public bool defaultGenerateTangents = false;
        public Texture2D defaultSVGIcon;

        public bool ignoreImportExceptions = true;
    }

}