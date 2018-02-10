// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using System.Collections;

namespace SVGImporter
{
    public class SVGImporterSettings : ScriptableObject 
    {
        protected static SVGImporterSettings _instance;
        public static SVGImporterSettings Get
        {
            get {
                if(_instance == null)
                {
                    _instance = Resources.Load<SVGImporterSettings>("svgImporterSettings");
                    if(_instance == null)
                    {
                        Debug.LogError("Cannot Load SVG Importer Settings! Please Move SVG Importer Settings in to Resource folder.");
                        _instance = ScriptableObject.CreateInstance<SVGImporterSettings>();
                        _instance.defaultSVGFormat = SVGAssetFormat.Transparent;
                        _instance.defaultUseGradients = SVGUseGradients.Never;
                        _instance.defaultAntialiasing = true;
                        _instance.defaultAntialiasingWidth = 2f;
                        _instance.defaultMeshCompression = SVGMeshCompression.High;
                        _instance.defaultVerticesPerMeter = 1000;
                        _instance.defaultScale = 0.001f;
                        _instance.defaultDepthOffset = 0.01f;
                        _instance.defaultCompressDepth = true;
                        _instance.defaultCustomPivotPoint = false;
                        _instance.defaultPivotPoint = new Vector2(0.5f, 0.5f);
                        _instance.defaultGenerateCollider = false;
                        _instance.defaultKeepSVGFile = false;
                        _instance.defaultUseLayers = false;
                        _instance.defaultIgnoreSVGCanvas = true;
                        _instance.defaultOptimizeMesh = true;
                        _instance.defaultGenerateNormals = false;
                        _instance.defaultGenerateTangents = false;
                        _instance.defaultSVGIcon = null;
                        _instance.ignoreImportExceptions = true;
                    }
                }

                return _instance;
            }
        }

        public static void UpdateAntialiasing()
        {
            Shader.SetGlobalFloat("SVG_ANTIALIASING_WIDTH", Get.defaultAntialiasingWidth);
        }

        protected static string _version = "1.1.3";
        public static string version
        {
            get {
                return _version;
            }
        }

        public SVGAssetFormat defaultSVGFormat = SVGAssetFormat.Transparent;
        public SVGUseGradients defaultUseGradients = SVGUseGradients.Always;
        public bool defaultAntialiasing = false;
        public float defaultAntialiasingWidth = 2f;
        public SVGMeshCompression defaultMeshCompression = SVGMeshCompression.Off;
        public int defaultVerticesPerMeter = 1000;
        public float defaultScale = 0.01f;
        public float defaultDepthOffset = 0.01f;
        public bool defaultCompressDepth = true;
        public bool defaultCustomPivotPoint = false;
        public Vector2 defaultPivotPoint = new Vector2(0.5f, 0.5f);
        public bool defaultGenerateCollider = false;
        public bool defaultKeepSVGFile = true;
        public bool defaultUseLayers = false;
        public bool defaultIgnoreSVGCanvas = true;
        public bool defaultOptimizeMesh = true;
        public bool defaultGenerateNormals = false;
        public bool defaultGenerateTangents = false;
        public Texture2D defaultSVGIcon;

        public bool ignoreImportExceptions = true;
    }

}