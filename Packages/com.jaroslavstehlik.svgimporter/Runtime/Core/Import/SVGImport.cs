// Copyright (C) 2026 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace SVGImporter
{
    using Document;
    using Geometry;
    using Native;
    using Rendering;
    using Utils;

    /// <summary>
    /// Imports SVG text: the one import pipeline, used by the Editor importer, by SVGAsset.Load at
    /// runtime and by 1.x SVG Assets.
    ///
    /// Import depends only on its arguments. An engine turns the document into layers of shapes
    /// and outlines (the native core, or the legacy C# parser); the shared steps then place them
    /// on the canvas, build the mesh, choose the shaders and gather the gradients and collider.
    /// Nothing is written to an asset: SVGAsset.Apply does that with the result.
    /// </summary>
    public static class SVGImport
    {
        // Changing what an import produces changes this, and so every import hash.
        const string pipelineVersion = "3";

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
            result.engine = EffectiveEngine(options.engine, svgText);
            Mesh mesh = null;
            try
            {
                Rect viewport;
                if (!ParseDocument(svgText, options, context, result, out viewport))
                    return result;

                // The canvas and the shapes in mesh units, around the pivot.
                Rect canvas = new Rect(viewport.x * options.scale, viewport.y * options.scale, viewport.width * options.scale, viewport.height * options.scale);
                Vector2 offset;
                SVGGraphics.CorrectSVGLayers(context.layers, canvas, options.ignoreSVGCanvas, options.pivotPoint, out offset);
                SVGLayer[] layers = context.layers.ToArray();

                mesh = new Mesh();
                Shader[] shaders;
                SVGMesh.CombineMeshes(layers, mesh, out shaders, options.useGradients, options.format, options.compressDepth, options.antialiasing, options.depthOffset);
                bool hasGradients = ChooseShaders(shaders, options.useGradients);
                result.shaders = shaders.Where(s => s != null).Select(s => s.name).ToArray();
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

        /// <summary>The engine that imports: Native only where it is available and supports the document.</summary>
        public static SVGImportEngine EffectiveEngine(SVGImportEngine engine, string svgText)
        {
            bool native = engine == SVGImportEngine.Native && SVGNativeCore.isSupported && (svgText == null || SVGNativeImport.CanImport(svgText));
            return native ? SVGImportEngine.Native : SVGImportEngine.Legacy;
        }

        static bool ParseDocument(string svgText, SVGImportOptions options, SVGImportContext context, SVGImportResult result, out Rect viewport)
        {
            if (result.engine == SVGImportEngine.Native)
            {
                try
                {
                    return SVGNativeImport.Populate(svgText, options, context, out viewport);
                }
                catch (Exception exception) when (exception is DllNotFoundException || exception is EntryPointNotFoundException || exception is InvalidOperationException)
                {
                    // A missing or mismatched native library: the legacy engine still works.
                    result.messages.Add("The native import engine isn't available (" + exception.Message + "); imported with the legacy engine.");
                    result.engine = SVGImportEngine.Legacy;
                    context.Reset();
                }
            }
            return ParseLegacy(svgText, options, context, out viewport);
        }

        /// <summary>
        /// The legacy C# parser keeps its settings and output in static fields (SVGAssetImport,
        /// SVGGraphics, SVGParser). They are set here, copied into the context and cleared again,
        /// so nothing outside this method sees them.
        /// </summary>
        static bool ParseLegacy(string svgText, SVGImportOptions options, SVGImportContext context, out Rect viewport)
        {
            viewport = new Rect();
            SVGAssetImport.Begin(options, context.atlasData, context.errors);
            SVGDocument document = null;
            try
            {
                SVGElement root;
                try
                {
                    document = new SVGDocument(svgText, new SVGGraphics(options.verticesPerMeter, options.antialiasing));
                    root = document.rootElement;
                }
                catch (Exception exception)
                {
                    context.errors.Add(SVGError.Syntax);
                    context.messages.Add("The SVG document couldn't be parsed: " + exception.Message);
                    return false;
                }
                if (root == null)
                {
                    context.errors.Add(SVGError.Syntax);
                    context.messages.Add("The SVG document has no root element.");
                    return false;
                }
                root.Render();
                viewport = root.paintable.viewport;
                context.layers.AddRange(SVGGraphics.layers);
                context.paths.AddRange(SVGGraphics.paths);
                return true;
            }
            finally
            {
                if (document != null) document.Clear();
                SVGAssetImport.End();
            }
        }

        /// <summary>
        /// Gradient shaders when gradients are used (always, or because the document has some),
        /// solid colour shaders otherwise. Returns whether the mesh uses gradients.
        /// </summary>
        static bool ChooseShaders(Shader[] shaders, SVGUseGradients useGradients)
        {
            Func<Shader, Shader, bool> same = (a, b) => a != null && b != null && a.name == b.name;
            bool hasGradients = useGradients == SVGUseGradients.Always || shaders.Any(s =>
                same(s, SVGShader.GradientColorOpaque) || same(s, SVGShader.GradientColorAlphaBlended) || same(s, SVGShader.GradientColorAlphaBlendedAntialiased));
            var pairs = new[] {
                new[] { SVGShader.SolidColorOpaque, SVGShader.GradientColorOpaque },
                new[] { SVGShader.SolidColorAlphaBlended, SVGShader.GradientColorAlphaBlended },
                new[] { SVGShader.SolidColorAlphaBlendedAntialiased, SVGShader.GradientColorAlphaBlendedAntialiased },
            };
            for (int i = 0; i < shaders.Length; i++)
            {
                foreach (Shader[] pair in pairs)
                {
                    if (hasGradients && same(shaders[i], pair[0])) shaders[i] = pair[1];
                    else if (!hasGradients && same(shaders[i], pair[1])) shaders[i] = pair[0];
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
