// Copyright (C) 2026 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using UnityEngine;

namespace SVGImporter
{
    /// <summary>
    /// Everything an import depends on besides the SVG text. SVGImport.Import reads nothing else, so
    /// equal text and options always give the same result.
    /// </summary>
    [System.Serializable]
    public struct SVGImportOptions
    {
        /// <summary>Ignored: SVG Importer has one import engine, the native core.</summary>
        public SVGImportEngine engine;
        public SVGAssetFormat format;
        public SVGUseGradients useGradients;
        public bool antialiasing;
        /// <summary>Quality: curves are flattened to within 1000 / verticesPerMeter SVG units.</summary>
        public float verticesPerMeter;
        /// <summary>World units per SVG unit.</summary>
        public float scale;
        /// <summary>Depth between overlapping shapes in the Opaque format.</summary>
        public float depthOffset;
        public bool compressDepth;
        public bool customPivotPoint;
        public Vector2 pivotPoint;
        public Vector4 border;
        public bool sliceMesh;
        /// <summary>Centre the mesh on its content instead of the SVG canvas.</summary>
        public bool ignoreSVGCanvas;
        public bool generateCollider;
        /// <summary>Keep layers for modifiers instead of building one mesh.</summary>
        public bool useLayers;
        public bool generateNormals;
        public bool generateTangents;
        /// <summary>Applied by the Editor importer (Unity's mesh compression is Editor only).</summary>
        public SVGMeshCompression meshCompression;
        public bool optimizeMesh;
        public bool keepSVGFile;

        /// <summary>The options SVGAsset.Load uses without settings.</summary>
        public static SVGImportOptions Default
        {
            get {
                return new SVGImportOptions {
                    engine = SVGImportEngine.Native,
                    format = SVGAssetFormat.Transparent,
                    useGradients = SVGUseGradients.Always,
                    antialiasing = false,
                    verticesPerMeter = 1000f,
                    scale = 0.01f,
                    depthOffset = 0.01f,
                    compressDepth = true,
                    pivotPoint = new Vector2(0.5f, 0.5f),
                    ignoreSVGCanvas = true,
                    optimizeMesh = true,
                };
            }
        }

        /// <summary>Options from an SVGImporterSettings' defaults.</summary>
        public static SVGImportOptions FromSettings(SVGImporterSettings settings)
        {
            return new SVGImportOptions {
                engine = SVGImportEngine.Native,
                format = settings.defaultSVGFormat,
                useGradients = settings.defaultUseGradients,
                antialiasing = settings.defaultAntialiasing,
                verticesPerMeter = settings.defaultVerticesPerMeter,
                scale = settings.defaultScale,
                depthOffset = settings.defaultDepthOffset,
                compressDepth = settings.defaultCompressDepth,
                customPivotPoint = settings.defaultCustomPivotPoint,
                pivotPoint = settings.defaultPivotPoint,
                ignoreSVGCanvas = settings.defaultIgnoreSVGCanvas,
                generateCollider = settings.defaultGenerateCollider,
                useLayers = settings.defaultUseLayers,
                generateNormals = settings.defaultGenerateNormals,
                generateTangents = settings.defaultGenerateTangents,
                meshCompression = settings.defaultMeshCompression,
                optimizeMesh = settings.defaultOptimizeMesh,
                keepSVGFile = settings.defaultKeepSVGFile,
            };
        }

        /// <summary>The options an SVG Asset was imported with.</summary>
        public static SVGImportOptions FromAsset(SVGAsset asset)
        {
            return asset.importOptions;
        }
    }
}
