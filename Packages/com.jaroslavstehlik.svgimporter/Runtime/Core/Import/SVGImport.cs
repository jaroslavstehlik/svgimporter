// Copyright (C) 2026 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace SVGImporter
{
    using Geometry;
    using Native;
    using Rendering;
    using Utils;

    /// <summary>
    /// Imports SVG text: the one import pipeline, used by the Editor importer, by SVGAsset.Load at
    /// runtime and by 1.x SVG Assets.
    ///
    /// Import depends only on its arguments. The native core turns the document into layers of
    /// shapes, fringes and outlines; the steps here then place them on the canvas, build the mesh,
    /// choose the shaders and gather the gradients and collider.
    /// Nothing is written to an asset: SVGAsset.Apply does that with the result.
    /// </summary>
    public static class SVGImport
    {
        // Changing what an import produces changes this, and so every import hash.
        const string pipelineVersion = "9";

        public static SVGImportResult Import(string svgText, SVGImportOptions options)
        {
            var result = new SVGImportResult();
            result.importHash = Hash(svgText, options);
            if (string.IsNullOrEmpty(svgText))
            {
                result.errors.Add(SVGError.CorruptedFile);
                result.messages.Add("The SVG document is empty.");
                return result;
            }

            var context = new SVGImportContext(result.errors, result.messages);
            result.engine = SVGImportEngine.Native;
            Mesh mesh = null;
            try
            {
                Rect viewport;
                if (!ParseDocument(svgText, options, context, result, out viewport))
                    return result;

                // The canvas and the shapes in mesh units, around the pivot.
                Rect canvas = new Rect(viewport.x * options.scale, viewport.y * options.scale, viewport.width * options.scale, viewport.height * options.scale);
                Vector2 offset;
                SVGLayerPlacement.Place(context.layers, canvas, options.ignoreSVGCanvas, options.pivotPoint, out offset);
                SVGLayer[] layers = context.layers.ToArray();

                mesh = new Mesh();
                string[] shaders;
                SVGMesh.CombineMeshes(layers, mesh, out shaders, options.useGradients, options.format, options.compressDepth, options.antialiasing, options.depthOffset);
                bool hasGradients = ChooseShaders(shaders, options.useGradients);
                result.shaders = shaders;
                if (options.generateNormals)
                    AddNormals(mesh, options);

                result.canvasRectangle = canvas;
                if (options.generateCollider)
                    result.colliderShape = ColliderShape(context.paths, options.scale, offset);
                if (hasGradients)
                {
                    CCGradient[] gradients = context.atlasData.registeredGradients;
                    if (gradients.Length > 0) result.gradients = gradients;
                }
                if (options.useLayers)
                {
                    result.layers = layers;
                    DestroyObject(mesh);
                }
                else
                {
                    result.mesh = mesh;
                }
                result.succeeded = true;
            }
            catch (Exception exception)
            {
                result.errors.Add(SVGError.CorruptedFile);
                result.messages.Add("The SVG document couldn't be imported: " + exception);
                DestroyObject(mesh);
                result.mesh = null;
                result.layers = null;
                result.gradients = null;
                result.colliderShape = null;
            }
            return result;
        }

        /// <summary>There is one import engine, the native core; kept for compatibility.</summary>
        [Obsolete("SVG Importer has one import engine, the native core.")]
        public static SVGImportEngine EffectiveEngine(SVGImportEngine engine, string svgText)
        {
            return SVGImportEngine.Native;
        }

        static bool ParseDocument(string svgText, SVGImportOptions options, SVGImportContext context, SVGImportResult result, out Rect viewport)
        {
            try
            {
                return SVGNativeImport.Populate(svgText, options, context, out viewport);
            }
            catch (Exception exception) when (exception is DllNotFoundException || exception is EntryPointNotFoundException || exception is InvalidOperationException)
            {
                // A missing or mismatched native library, for example on a platform it isn't built for.
                viewport = new Rect();
                context.errors.Add(SVGError.CorruptedFile);
                context.messages.Add("The SVG import library isn't available on this platform: " + exception.Message);
                return false;
            }
        }

        /// <summary>
        /// Gradient shaders when gradients are used (always, or because the document has some),
        /// solid colour shaders otherwise. Returns whether the mesh uses gradients. Shaders are
        /// chosen by name: on a project's first import Unity may not have imported them yet.
        /// </summary>
        static bool ChooseShaders(string[] shaders, SVGUseGradients useGradients)
        {
            var pairs = new[] {
                new[] { SVGShader.SolidColorOpaqueName, SVGShader.GradientColorOpaqueName },
                new[] { SVGShader.SolidColorAlphaBlendedName, SVGShader.GradientColorAlphaBlendedName },
                new[] { SVGShader.SolidColorAlphaBlendedAntialiasedName, SVGShader.GradientColorAlphaBlendedAntialiasedName },
            };
            bool hasGradients = useGradients == SVGUseGradients.Always || shaders.Any(s => pairs.Any(pair => s == pair[1]));
            for (int i = 0; i < shaders.Length; i++)
            {
                foreach (string[] pair in pairs)
                {
                    if (hasGradients && shaders[i] == pair[0]) shaders[i] = pair[1];
                    else if (!hasGradients && shaders[i] == pair[1]) shaders[i] = pair[0];
                }
            }
            return hasGradients;
        }

        /// <summary>
        /// Normals facing the camera, and tangents, for lit shaders. With antialiasing the normals
        /// carry the fringe's extrusion and are left as they are.
        /// </summary>
        static void AddNormals(Mesh mesh, SVGImportOptions options)
        {
            int vertexCount = mesh.vertexCount;
            if (vertexCount == 0) return;
            if (!options.antialiasing)
                mesh.normals = Enumerable.Repeat(-Vector3.forward, vertexCount).ToArray();
            if (options.generateTangents)
                mesh.tangents = Enumerable.Repeat(new Vector4(-1f, 0f, 0f, -1f), vertexCount).ToArray();
        }

        /// <summary>The union of the shapes' outlines, in mesh units around the pivot.</summary>
        static SVGPath[] ColliderShape(List<SVGPath> outlines, float scale, Vector2 offset)
        {
            if (outlines.Count == 0) return null;
            var polygons = new List<List<Vector2>>(outlines.Count);
            foreach (SVGPath outline in outlines)
            {
                var points = new List<Vector2>(outline.points.Length);
                foreach (Vector2 point in outline.points)
                    points.Add(new Vector2(point.x * scale - offset.x, -(point.y * scale + offset.y)));
                polygons.Add(points);
            }
            polygons = SVGGeom.MergePolygon(polygons);
            return polygons.Count == 0 ? null : polygons.Select(p => new SVGPath(p.ToArray())).ToArray();
        }

        static long Hash(string svgText, SVGImportOptions options)
        {
            Hash128 hash = Hash128.Compute(svgText ?? "");
            hash.Append(JsonUtility.ToJson(options));
            hash.Append(pipelineVersion);
            return unchecked((long)ulong.Parse(hash.ToString().Substring(0, 16), NumberStyles.HexNumber));
        }

        static void DestroyObject(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(obj);
            else UnityEngine.Object.DestroyImmediate(obj);
        }
    }
}
