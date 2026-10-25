// Copyright (C) 2015 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using UnityEngine;
using System.Collections.Generic;

namespace SVGImporter 
{
    using Document;
    using Rendering;

    /// <summary>
    /// The legacy C# parser's import state. The parser reads its settings from these static fields
    /// and writes gradients and errors to them, so SVGImport sets them for the length of one legacy
    /// import (Begin, End); nothing else reads them. They go away with the legacy parser.
    /// Imports use SVGImport.Import.
    /// </summary>
    public class SVGAssetImport
    {    
        public static SVGAtlasData atlasData;
        public static List<SVGError> errors;	

        public static SVGUseGradients useGradients;
        public static bool antialiasing;
        public static float vpm;
        public static Vector2 pivotPoint;
        public static bool ignoreSVGCanvas;
        public static float meshScale = 1f;        
        public static Vector4 border;
        public static bool sliceMesh = false;
        public static float minDepthOffset = 0.001f;
        public static SVGAssetFormat format = SVGAssetFormat.Opaque;
        public static bool compressDepth = true;

        internal static void Begin(SVGImportOptions options, SVGAtlasData atlas, List<SVGError> errorList)
        {
            atlasData = atlas;
            errors = errorList;
            useGradients = options.useGradients;
            antialiasing = options.antialiasing;
            vpm = options.verticesPerMeter;
            pivotPoint = options.pivotPoint;
            ignoreSVGCanvas = options.ignoreSVGCanvas;
            meshScale = options.scale;
            border = options.border;
            sliceMesh = options.sliceMesh;
            minDepthOffset = options.depthOffset;
            format = options.format;
            compressDepth = options.compressDepth;
            SVGParser.Init();
            SVGGraphics.Init();
        }

        internal static void End()
        {
            atlasData = null;
            errors = null;
            SVGParser.Clear();
            SVGGraphics.Clear();
        }

        [System.Obsolete("Imports keep no state between them; see SVGImport.Import.")]
        public static void Clear()
        {
            End();
        }
    }
}
