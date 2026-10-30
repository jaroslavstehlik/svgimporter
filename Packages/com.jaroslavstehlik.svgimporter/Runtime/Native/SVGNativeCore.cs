// Copyright (C) 2026 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using System;
using System.Text;
using UnityEngine;

namespace SVGImporter.Native
{
    /// <summary>
    /// Safe wrapper around the native import core (svgimporter-core, see DESIGN.md). Copies the
    /// native result into managed arrays and always releases the native memory.
    /// </summary>
    public static class SVGNativeCore
    {
        /// <summary>Whether the native core is available on this platform.</summary>
        public static bool isSupported
        {
            get {
#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_IOS || UNITY_ANDROID
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>The ABI version this C# code was generated for.</summary>
        public const uint expectedAbiVersion = NativeMethods.SVGI_ABI_VERSION;

        /// <summary>The ABI version of the loaded library.</summary>
        public static uint abiVersion
        {
            get { return NativeMethods.svgi_version(); }
        }

        public enum Status
        {
            Ok = NativeMethods.SVGI_OK,
            ParseError = NativeMethods.SVGI_ERROR_PARSE,
            ArgumentError = NativeMethods.SVGI_ERROR_ARGUMENT,
            InternalError = NativeMethods.SVGI_ERROR_INTERNAL,
        }

        [Flags]
        public enum Diagnostics : uint
        {
            None = 0,
            Mask = NativeMethods.SVGI_DIAG_MASK,
            Filter = NativeMethods.SVGI_DIAG_FILTER,
            ClipPath = NativeMethods.SVGI_DIAG_CLIP_PATH,
            Image = NativeMethods.SVGI_DIAG_IMAGE,
            Pattern = NativeMethods.SVGI_DIAG_PATTERN,
            BlendMode = NativeMethods.SVGI_DIAG_BLEND_MODE,
            TextSkipped = NativeMethods.SVGI_DIAG_TEXT_SKIPPED,
            GroupOpacity = NativeMethods.SVGI_DIAG_GROUP_OPACITY,
        }

        public enum LayerKind : byte { Fill = NativeMethods.SVGI_LAYER_FILL, Stroke = NativeMethods.SVGI_LAYER_STROKE, Fringe = NativeMethods.SVGI_LAYER_FRINGE }
        public enum PaintKind : byte { Solid = NativeMethods.SVGI_PAINT_SOLID, Linear = NativeMethods.SVGI_PAINT_LINEAR, Radial = NativeMethods.SVGI_PAINT_RADIAL, Conical = NativeMethods.SVGI_PAINT_CONICAL }
        public enum Spread : byte { Pad = NativeMethods.SVGI_SPREAD_PAD, Reflect = NativeMethods.SVGI_SPREAD_REFLECT, Repeat = NativeMethods.SVGI_SPREAD_REPEAT }

        public struct Layer
        {
            public string name;
            public LayerKind kind;
            public int paint;
            public float opacity;
            /// <summary>Range in <see cref="Result.vertices"/>.</summary>
            public int vertexOffset, vertexCount;
            /// <summary>Range in <see cref="Result.indices"/>; indices are relative to <see cref="vertexOffset"/>.</summary>
            public int indexOffset, indexCount;
            /// <summary>Index of the source element in painting order; a path's fill and stroke share it.</summary>
            public int element;
            /// <summary>Closed outlines in SVG user units. Outer boundaries have a positive signed area, holes a negative one.</summary>
            public Vector2[][] contours;
        }

        public struct Stop
        {
            public float offset;
            public Color color;
        }

        public struct Paint
        {
            public PaintKind kind;
            public Color color;
            /// <summary>Gradient space to SVG user space (a, b, c, d, e, f).</summary>
            public Matrix4x4 transform;
            /// <summary>Linear: x1, y1, x2, y2. Radial: cx, cy, r, fx, fy, fr.</summary>
            public float[] geometry;
            public Spread spread;
            public Stop[] stops;
        }

        public class Result
        {
            public Status status;
            public string errorMessage;
            public Diagnostics diagnostics;
            /// <summary>Document size in SVG user units.</summary>
            public Vector2 size;
            public Layer[] layers;
            /// <summary>Positions in SVG user units, y pointing down.</summary>
            public Vector2[] vertices;
            /// <summary>With antialiasing, per vertex: the antialiasing extrusion vector and the coverage. Empty otherwise.</summary>
            public Vector2[] extrusions;
            public float[] coverage;
            public int[] indices;
            public Paint[] paints;
        }

        /// <summary>Imports an SVG document.</summary>
        /// <param name="tolerance">Maximum distance in SVG user units between a curve and its line segments.</param>
        /// <param name="convertText">Convert text to paths with the system fonts.</param>
        /// <param name="antialiasing">Add an antialiasing fringe after every fill and stroke layer.</param>
        /// <param name="groupOpacity">Remove what later opaque children of a group with opacity cover.</param>
        public static unsafe Result Import(byte[] svg, float tolerance = 0.25f, bool convertText = true, float dpi = 96f, bool antialiasing = false, bool groupOpacity = true)
        {
            if (svg == null) throw new ArgumentNullException("svg");
            if (!isSupported)
                throw new InvalidOperationException("SVG Importer can't import at runtime on " + Application.platform + ", which has no native library; import the files in the Editor.");
            if (abiVersion != expectedAbiVersion)
                throw new InvalidOperationException("svgimporter_core ABI version " + abiVersion + " doesn't match the expected " + expectedAbiVersion + ". Reinstall the package.");

            var options = new SvgiOptions { tolerance = tolerance, dpi = dpi, convert_text = (byte)(convertText ? 1 : 0), antialiasing = (byte)(antialiasing ? 1 : 0), group_opacity = (byte)(groupOpacity ? 1 : 0) };
            SvgiResult* native = null;
            int code;
            fixed (byte* data = svg)
            {
                code = NativeMethods.svgi_import(data, (nuint)svg.Length, &options, &native);
            }

            var result = new Result { status = (Status)code };
            if (native == null) return result;
            try
            {
                Copy(native, result);
            }
            finally
            {
                NativeMethods.svgi_free(native);
            }
            return result;
        }

        static unsafe void Copy(SvgiResult* r, Result result)
        {
            result.errorMessage = r->error_message_length > 0 ? Encoding.UTF8.GetString(r->error_message, (int)r->error_message_length) : "";
            result.diagnostics = (Diagnostics)r->diagnostics;
            result.size = new Vector2(r->width, r->height);

            int vertexCount = (int)r->vertex_float_count / 2;
            result.vertices = new Vector2[vertexCount];
            for (int i = 0; i < vertexCount; i++)
                result.vertices[i] = new Vector2(r->vertices[i * 2], r->vertices[i * 2 + 1]);

            int extrusionCount = (int)r->extrusion_float_count / 2;
            result.extrusions = new Vector2[extrusionCount];
            for (int i = 0; i < extrusionCount; i++)
                result.extrusions[i] = new Vector2(r->extrusions[i * 2], r->extrusions[i * 2 + 1]);
            result.coverage = new float[r->coverage_count];
            for (int i = 0; i < result.coverage.Length; i++)
                result.coverage[i] = r->coverage[i];
            result.indices = new int[r->index_count];
            for (int i = 0; i < result.indices.Length; i++)
                result.indices[i] = (int)r->indices[i];

            result.layers = new Layer[r->layer_count];
            for (int i = 0; i < result.layers.Length; i++)
            {
                SvgiLayer l = r->layers[i];
                result.layers[i] = new Layer {
                    name = l.name_length > 0 ? Encoding.UTF8.GetString(r->strings + l.name_offset, (int)l.name_length) : "",
                    kind = (LayerKind)l.kind,
                    paint = (int)l.paint,
                    opacity = l.opacity,
                    vertexOffset = (int)l.vertex_offset,
                    vertexCount = (int)l.vertex_count,
                    indexOffset = (int)l.index_offset,
                    indexCount = (int)l.index_count,
                    element = (int)l.element,
                    contours = new Vector2[l.contour_count][],
                };
                for (int c = 0; c < l.contour_count; c++)
                {
                    SvgiContour contour = r->contours[l.contour_offset + c];
                    var points = new Vector2[contour.point_count];
                    for (int p = 0; p < points.Length; p++)
                    {
                        uint k = (contour.point_offset + (uint)p) * 2;
                        points[p] = new Vector2(r->contour_points[k], r->contour_points[k + 1]);
                    }
                    result.layers[i].contours[c] = points;
                }
            }

            result.paints = new Paint[r->paint_count];
            for (int i = 0; i < result.paints.Length; i++)
            {
                SvgiPaint p = r->paints[i];
                var stops = new Stop[p.stop_count];
                for (int s = 0; s < stops.Length; s++)
                {
                    SvgiStop stop = r->stops[p.stop_offset + s];
                    stops[s] = new Stop { offset = stop.offset, color = new Color(stop.color[0], stop.color[1], stop.color[2], stop.color[3]) };
                }
                var transform = Matrix4x4.identity;
                transform.m00 = p.transform[0]; transform.m10 = p.transform[1];
                transform.m01 = p.transform[2]; transform.m11 = p.transform[3];
                transform.m03 = p.transform[4]; transform.m13 = p.transform[5];
                var geometry = new float[6];
                for (int g = 0; g < 6; g++) geometry[g] = p.geometry[g];
                result.paints[i] = new Paint {
                    kind = (PaintKind)p.kind,
                    color = new Color(p.color[0], p.color[1], p.color[2], p.color[3]),
                    transform = transform,
                    geometry = geometry,
                    spread = (Spread)p.spread,
                    stops = stops,
                };
            }
        }
    }
}
