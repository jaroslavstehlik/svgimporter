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
            return svgText.IndexOf("conicalGradient", System.StringComparison.Ordinal) < 0;
        }

        /// <summary>
        /// Imports svgText into SVGGraphics.layers and SVGGraphics.paths, registering gradients in
        /// SVGAssetImport.atlasData. Returns false if the document couldn't be imported.
        /// </summary>
        public static bool Populate(string svgText, out Rect viewport)
        {
            viewport = new Rect();
            float meshScale = SVGAssetImport.meshScale;
            SVGNativeCore.Result result = SVGNativeCore.Import(Encoding.UTF8.GetBytes(svgText), Tolerance());
            if (result.status != SVGNativeCore.Status.Ok)
            {
                SVGAssetImport.errors.Add(result.status == SVGNativeCore.Status.ParseError ? SVGError.Syntax : SVGError.CorruptedFile);
                Debug.LogError("SVG Importer: " + result.status + ": " + result.errorMessage);
                return false;
            }
            AddDiagnostics(result.diagnostics);

            viewport = new Rect(0f, 0f, result.size.x, result.size.y);
            Rect scaledViewport = new Rect(0f, 0f, result.size.x * meshScale, result.size.y * meshScale);

            var shapes = new List<SVGShape>();
            int element = -1;
            string name = null;
            foreach (SVGNativeCore.Layer layer in result.layers)
            {
                // A path's fill and stroke form one layer, as in the legacy importer.
                if (layer.element != element)
                {
                    Flush(shapes, name);
                    element = layer.element;
                    name = layer.name;
                }

                SVGShape shape = CreateShape(result, layer, meshScale);
                shape.fill = CreateFill(result.paints[layer.paint], layer.opacity, viewport, scaledViewport, meshScale);
                shapes.Add(shape);

                var contours = new List<List<Vector2>>(layer.contours.Length);
                foreach (Vector2[] contour in layer.contours)
                {
                    contours.Add(new List<Vector2>(contour));
                    SVGGraphics.paths.Add(new SVGPath(contour));
                }

                if (SVGAssetImport.antialiasing)
                {
                    SVGShape fringe;
                    if (SVGSimplePath.CreateAntialiasing(contours, out fringe, Color.white, -1f, ClosePathRule.ALWAYS))
                    {
                        for (int i = 0; i < fringe.vertices.Length; i++)
                            fringe.vertices[i] *= meshScale;
                        fringe.type = SVGShapeType.ANTIALIASING;
                        fringe.RecalculateBounds();
                        fringe.fill = shape.fill.Clone();
                        fringe.fill.blend = FILL_BLEND.ALPHA_BLENDED;
                        shapes.Add(fringe);
                    }
                }
            }
            Flush(shapes, name);
            return true;
        }

        /// <summary>
        /// The flattening tolerance in SVG user units for the legacy Quality setting (vertices per
        /// meter): the legacy importer flattens curves with a tolerance of 1000 / vertexPerMeter
        /// user units, whatever the mesh scale.
        /// </summary>
        static float Tolerance()
        {
            float vpm = SVGAssetImport.vpm > 0f ? SVGAssetImport.vpm : 1000f;
            return Mathf.Clamp(1000f / vpm, 0.01f, 10f);
        }

        static void Flush(List<SVGShape> shapes, string name)
        {
            if (shapes.Count == 0) return;
            // The native core turns every shape into a path, so elements without an id get the
            // legacy name for paths.
            SVGGraphics.AddLayer(new SVGLayer { name = string.IsNullOrEmpty(name) ? "Path Element" : name, shapes = shapes.ToArray() });
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
            shape.RecalculateBounds();
            return shape;
        }

        static SVGFill CreateFill(SVGNativeCore.Paint paint, float opacity, Rect viewport, Rect scaledViewport, float meshScale)
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
                fill.gradientColors = SVGAssetImport.atlasData.AddGradient(CreateGradient(paint.stops));
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

        /// <summary>Gradient stops as a CCGradient, with the end colours extended to 0 and 1 as the legacy brushes do.</summary>
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

        static void AddDiagnostics(SVGNativeCore.Diagnostics diagnostics)
        {
            if ((diagnostics & SVGNativeCore.Diagnostics.ClipPath) != 0) SVGAssetImport.errors.Add(SVGError.ClipPath);
            if ((diagnostics & SVGNativeCore.Diagnostics.Mask) != 0) SVGAssetImport.errors.Add(SVGError.Mask);
            if ((diagnostics & SVGNativeCore.Diagnostics.Image) != 0) SVGAssetImport.errors.Add(SVGError.Image);
        }
    }
}
