// Copyright (C) 2026 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SVGImporter.Native
{
    using Rendering;
    using Utils;

    /// <summary>
    /// Fills SVGGraphics with layers produced by the native core, in the same form the legacy C#
    /// parser produces them. Everything after that (render formats, depth, gradient atlas, layers
    /// for modifiers, colliders) runs unchanged. See DESIGN.md, section 5.
    /// </summary>
    internal static class SVGNativeImport
    {
        /// <summary>
        /// False for documents that use SVG Importer extensions the native core doesn't support yet
        /// (conicalGradient); those are imported by the legacy parser.
        /// </summary>
        public static bool CanImport(string svgText)
        {
            return !System.Text.RegularExpressions.Regex.IsMatch(svgText, @"<\s*conicalGradient\b");
        }

        /// <summary>
        /// Imports svgText into the context's layers and outlines, registering gradients in its
        /// atlas data. Returns false if the document couldn't be imported.
        /// </summary>
        public static bool Populate(string svgText, SVGImportOptions options, SVGImportContext context, out Rect viewport)
        {
            viewport = new Rect();
            float meshScale = options.scale;
            SVGNativeCore.Result result = SVGNativeCore.Import(Encoding.UTF8.GetBytes(svgText), Tolerance(options.verticesPerMeter), antialiasing: options.antialiasing);
            if (result.status != SVGNativeCore.Status.Ok)
            {
                context.errors.Add(result.status == SVGNativeCore.Status.ParseError ? SVGError.Syntax : SVGError.CorruptedFile);
                context.messages.Add(result.status + ": " + result.errorMessage);
                return false;
            }
            AddDiagnostics(result.diagnostics, context.errors);

            viewport = new Rect(0f, 0f, result.size.x, result.size.y);
            Rect scaledViewport = new Rect(0f, 0f, result.size.x * meshScale, result.size.y * meshScale);

            var shapes = new List<SVGShape>();
            int element = -1;
            string name = null;
            SVGFill previousFill = null;
            foreach (SVGNativeCore.Layer layer in result.layers)
            {
                // A path's fill and stroke (and their fringes) form one layer, as in the legacy importer.
                if (layer.element != element)
                {
                    Flush(shapes, name, context.layers);
                    element = layer.element;
                    name = layer.name;
                }

                SVGShape shape = CreateShape(result, layer, meshScale);
                if (layer.kind == SVGNativeCore.LayerKind.Fringe)
                {
                    // The antialiasing fringe of the previous layer, built by the native core.
                    shape.type = SVGShapeType.ANTIALIASING;
                    shape.fill = previousFill.Clone();
                    shape.fill.blend = FILL_BLEND.ALPHA_BLENDED;
                    shapes.Add(shape);
                    continue;
                }

                shape.fill = CreateFill(result.paints[layer.paint], layer.opacity, viewport, scaledViewport, meshScale, context.atlasData);
                previousFill = shape.fill;
                foreach (Vector2[] contour in layer.contours)
                    context.paths.Add(new SVGPath(contour));
                shapes.Add(shape);
            }
            Flush(shapes, name, context.layers);
            return true;
        }

        /// <summary>
        /// The flattening tolerance in SVG user units for the legacy Quality setting (vertices per
        /// meter): the legacy importer flattens curves with a tolerance of 1000 / vertexPerMeter
        /// user units, whatever the mesh scale.
        /// </summary>
        static float Tolerance(float verticesPerMeter)
        {
            float vpm = verticesPerMeter > 0f ? verticesPerMeter : 1000f;
            return Mathf.Clamp(1000f / vpm, 0.01f, 10f);
        }

        static void Flush(List<SVGShape> shapes, string name, List<SVGLayer> layers)
        {
            if (shapes.Count == 0) return;
            // The native core turns every shape into a path, so elements without an id get the
            // legacy name for paths.
            layers.Add(new SVGLayer { name = string.IsNullOrEmpty(name) ? "Path Element" : name, shapes = shapes.ToArray() });
            shapes.Clear();
        }

        static SVGShape CreateShape(SVGNativeCore.Result result, SVGNativeCore.Layer layer, float meshScale)
        {
            var vertices = new Vector2[layer.vertexCount];
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = result.vertices[layer.vertexOffset + i] * meshScale;
            var triangles = new int[layer.indexCount];
            System.Array.Copy(result.indices, layer.indexOffset, triangles, 0, layer.indexCount);

            var shape = new SVGShape {
                type = layer.kind == SVGNativeCore.LayerKind.Stroke ? SVGShapeType.STROKE : SVGShapeType.FILL,
                vertices = vertices,
                triangles = triangles,
            };
            // With antialiasing: the vertices' extrusion vectors, and fringe coverage as vertex alpha.
            if (result.extrusions.Length > 0)
            {
                shape.angles = new Vector2[layer.vertexCount];
                System.Array.Copy(result.extrusions, layer.vertexOffset, shape.angles, 0, layer.vertexCount);
                if (layer.kind == SVGNativeCore.LayerKind.Fringe)
                {
                    shape.colors = new Color32[layer.vertexCount];
                    for (int i = 0; i < layer.vertexCount; i++)
                        shape.colors[i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(result.coverage[layer.vertexOffset + i] * 255f));
                }
            }
            shape.RecalculateBounds();
            return shape;
        }

        static SVGFill CreateFill(SVGNativeCore.Paint paint, float opacity, Rect viewport, Rect scaledViewport, float meshScale, SVGAtlasData atlasData)
        {
            SVGFill fill;
            if (paint.kind == SVGNativeCore.PaintKind.Solid)
            {
                fill = new SVGFill(new Color(paint.color.r, paint.color.g, paint.color.b, 1f), FILL_BLEND.OPAQUE, FILL_TYPE.SOLID);
                fill.opacity = opacity;
                fill.transform = SVGMatrix.identity;
            }
            else
            {
                bool translucent = opacity < 1f;
                foreach (SVGNativeCore.Stop stop in paint.stops)
                    translucent |= stop.color.a < 1f;

                fill = new SVGFill(Color.white, FILL_BLEND.OPAQUE, FILL_TYPE.GRADIENT,
                    paint.kind == SVGNativeCore.PaintKind.Radial ? GRADIENT_TYPE.RADIAL : GRADIENT_TYPE.LINEAR);
                fill.opacity = opacity;
                fill.gradientColors = atlasData.AddGradient(CreateGradient(paint));
                fill.transform = LegacyGradientTransform(paint, viewport);
                if (translucent) fill.blend = FILL_BLEND.ALPHA_BLENDED;
            }
            if (opacity < 1f) fill.blend = FILL_BLEND.ALPHA_BLENDED;

            // As SVGSimplePath.CreatePolygon: the gradient transform and viewport in mesh units.
            SVGMatrix scale = SVGMatrix.identity.Scale(meshScale);
            fill.transform = scale.Multiply(fill.transform).Multiply(scale.Inverse());
            fill.viewport = scaledViewport;
            return fill;
        }

        /// <summary>
        /// The paint's gradient: its stops, with the end colours extended to 0 and 1 as the legacy
        /// brushes do, its spread method and, for radial gradients, the focal point in the space
        /// LegacyGradientTransform maps to (centre at 0.5, radius 0.5).
        /// </summary>
        static CCGradient CreateGradient(SVGNativeCore.Paint paint)
        {
            CCGradient gradient = CreateGradient(paint.stops);
            gradient.spreadMethod = paint.spread == SVGNativeCore.Spread.Reflect ? SVGSpreadMethod.Reflect
                : paint.spread == SVGNativeCore.Spread.Repeat ? SVGSpreadMethod.Repeat : SVGSpreadMethod.Pad;
            float[] g = paint.geometry;
            if (paint.kind == SVGNativeCore.PaintKind.Radial && g[2] > 0f)
            {
                float diameter = g[2] * 2f;
                gradient.focalOffset = new Vector2((g[3] - g[0]) / diameter, (g[4] - g[1]) / diameter);
                gradient.focalRadius = g[5] / diameter;
            }
            return gradient;
        }

        static CCGradient CreateGradient(SVGNativeCore.Stop[] stops)
        {
            var colorKeys = new List<CCGradientColorKey>();
            var alphaKeys = new List<CCGradientAlphaKey>();
            if (stops.Length == 0)
            {
                colorKeys.Add(new CCGradientColorKey(Color.black, 0f));
                alphaKeys.Add(new CCGradientAlphaKey(1f, 0f));
            }
            else
            {
                if (stops[0].offset > 0f)
                {
                    colorKeys.Add(new CCGradientColorKey(stops[0].color, 0f));
                    alphaKeys.Add(new CCGradientAlphaKey(stops[0].color.a, 0f));
                }
                foreach (SVGNativeCore.Stop stop in stops)
                {
                    colorKeys.Add(new CCGradientColorKey(stop.color, stop.offset));
                    alphaKeys.Add(new CCGradientAlphaKey(stop.color.a, stop.offset));
                }
                SVGNativeCore.Stop last = stops[stops.Length - 1];
                if (last.offset < 1f)
                {
                    colorKeys.Add(new CCGradientColorKey(last.color, 1f));
                    alphaKeys.Add(new CCGradientAlphaKey(last.color.a, 1f));
                }
            }
            return new CCGradient(colorKeys.ToArray(), alphaKeys.ToArray());
        }

        /// <summary>
        /// The legacy gradient transform (SVGSimplePath.GetFillTransform): it maps document space
        /// to a space where the gradient spans the viewport. The native paint already combines
        /// units, gradientTransform and the element transform into one gradient-to-document matrix.
        /// </summary>
        static SVGMatrix LegacyGradientTransform(SVGNativeCore.Paint paint, Rect viewport)
        {
            Matrix4x4 t = paint.transform;
            var gradientToDocument = new SVGMatrix(t.m00, t.m10, t.m01, t.m11, t.m03, t.m13);
            SVGMatrix documentToGradient;
            try {
                documentToGradient = gradientToDocument.Inverse();
            } catch (Document.SVGException) {
                return SVGMatrix.identity;
            }

            float[] g = paint.geometry;
            SVGMatrix transform = SVGMatrix.identity.Translate(viewport.center);
            if (paint.kind == SVGNativeCore.PaintKind.Linear)
            {
                Vector2 start = new Vector2(g[0], g[1]), end = new Vector2(g[2], g[3]);
                Vector2 vector = end - start;
                float length = vector.magnitude;
                if (length > 0f) transform = transform.Scale(viewport.width / length, viewport.height / length);
                transform = transform.Rotate(-Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg);
                transform = transform.Translate(-Vector2.Lerp(start, end, 0.5f));
            }
            else
            {
                Vector2 center = new Vector2(g[0], g[1]);
                float diameter = g[2] * 2f;
                if (diameter > 0f) transform = transform.Scale(viewport.width / diameter, viewport.height / diameter);
                transform = transform.Translate(-center);
            }
            return transform.Multiply(documentToGradient);
        }

        static void AddDiagnostics(SVGNativeCore.Diagnostics diagnostics, List<SVGError> errors)
        {
            if ((diagnostics & SVGNativeCore.Diagnostics.ClipPath) != 0) errors.Add(SVGError.ClipPath);
            if ((diagnostics & SVGNativeCore.Diagnostics.Mask) != 0) errors.Add(SVGError.Mask);
            if ((diagnostics & SVGNativeCore.Diagnostics.Image) != 0) errors.Add(SVGError.Image);
            if ((diagnostics & SVGNativeCore.Diagnostics.Filter) != 0) errors.Add(SVGError.Filter);
            if ((diagnostics & SVGNativeCore.Diagnostics.Pattern) != 0) errors.Add(SVGError.Pattern);
            if ((diagnostics & SVGNativeCore.Diagnostics.BlendMode) != 0) errors.Add(SVGError.BlendMode);
            if ((diagnostics & SVGNativeCore.Diagnostics.GroupOpacity) != 0) errors.Add(SVGError.GroupOpacity);
            if ((diagnostics & SVGNativeCore.Diagnostics.TextSkipped) != 0) errors.Add(SVGError.TextSkipped);
        }
    }
}
