// Copyright (C) 2026 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using UnityEngine;
using UnityEngine.Serialization;

namespace SVGImporter
{
    /// <summary>
    /// How SVG text is imported: the settings of an SVG file's importer (SVGFileImporter.importOptions),
    /// of an imported asset (SVGAsset.importOptions) and of runtime imports (SVGAsset.Load). An import
    /// depends only on the SVG text and these options, so equal inputs give the same asset.
    /// </summary>
    [System.Serializable]
    public struct SVGImportOptions
    {
        /// <summary>How the shapes are drawn: transparent, opaque with depth, or on a Canvas.</summary>
        public SVGAssetFormat format;
        /// <summary>Whether the mesh and its shaders support gradients.</summary>
        public SVGUseGradients useGradients;
        /// <summary>Adds a soft one-pixel edge to every shape. Off: edges are sharp unless the camera uses MSAA.</summary>
        [FormerlySerializedAs("antialiasing")]
        public bool softEdges;
        /// <summary>Quality: curves are flattened to within 1000 / verticesPerMeter SVG units.</summary>
        public float verticesPerMeter;
        /// <summary>World units per SVG unit.</summary>
        public float scale;
        /// <summary>Distance along z between overlapping shapes in the Opaque + Transparent format.</summary>
        public float depthOffset;
        /// <summary>Shapes that don't overlap share a depth in the Opaque + Transparent format.</summary>
        public bool compressDepth;
        /// <summary>Whether pivotPoint is a custom position rather than one of the nine presets.</summary>
        public bool customPivotPoint;
        /// <summary>The mesh's origin, from (0, 0) at the top left to (1, 1) at the bottom right.</summary>
        public Vector2 pivotPoint;
        /// <summary>The slicing border, as fractions of the size: left, bottom, right, top.</summary>
        public Vector4 border;
        public bool sliceMesh;
        /// <summary>Places the mesh around its content instead of the SVG canvas.</summary>
        public bool ignoreSVGCanvas;
        /// <summary>
        /// Stops shapes in a semi-transparent group from showing through each other, by removing the
        /// parts of shapes that later opaque shapes of the group cover. Adds vertices.
        /// </summary>
        public bool exactGroupOpacity;
        /// <summary>Stores the outline for SVG Collider 2D.</summary>
        public bool generateCollider;
        /// <summary>Keeps the layers, for modifiers and scripts, instead of one mesh.</summary>
        public bool useLayers;
        public bool generateNormals;
        /// <summary>Needs generateNormals.</summary>
        public bool generateTangents;
        /// <summary>Applied by the Editor importer: Unity's mesh compression is Editor only.</summary>
        public SVGMeshCompression meshCompression;
        public bool optimizeMesh;
        /// <summary>Keeps the SVG text in the asset (SVGAsset.svgFile), also in builds.</summary>
        public bool keepSVGFile;

        /// <summary>The settings a new SVG file gets, unless the project's defaults differ.</summary>
        public static SVGImportOptions Default
        {
            get {
                return new SVGImportOptions {
                    format = SVGAssetFormat.Transparent,
                    useGradients = SVGUseGradients.Auto,
                    softEdges = true,
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
    }
}
