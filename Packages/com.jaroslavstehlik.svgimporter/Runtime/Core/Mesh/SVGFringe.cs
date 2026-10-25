// Copyright (C) 2026 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using System.Collections.Generic;
using UnityEngine;

namespace SVGImporter.Utils
{
    using Rendering;

    /// <summary>
    /// The antialiasing fringe (DESIGN.md, 7.2): a strip along each contour of a shape that the
    /// vertex shader widens to the antialiasing width on screen, centred on the outline, so shapes
    /// keep their size.
    ///
    /// Every fringe vertex lies on the outline. Its extrusion vector (SVGShape.angles) is the
    /// direction the shader moves it in, scaled so that both edges next to a corner move by the
    /// same distance: outward for the outer edge, which is transparent, and inward for the inner
    /// edge, which is opaque. Coverage is the vertex alpha interpolated across the strip. Corners
    /// are miters up to a miter limit of 2 (corners down to about 60 degrees) and bevels beyond it.
    /// The fill's vertices on the outline move inward by the same vector, so the fill and the
    /// fringe meet without a gap or an overlap.
    ///
    /// Used by the Legacy engine. The native core builds the same fringe itself
    /// (native/svgimporter-core/src/fringe.rs), so the Native engine uses no C# mesh code.
    /// </summary>
    public static class SVGFringe
    {
        public const float miterLimit = 2f;

        /// <summary>
        /// Builds the fringe for a shape's contours (closed, in any orientation, with holes), and
        /// the inward extrusion of each outline point for the fill's vertices.
        /// </summary>
        public static bool Build(List<List<Vector2>> contours, Color32 color, out SVGShape fringe, out Dictionary<Vector2, Vector2> boundary)
        {
            fringe = new SVGShape();
            boundary = new Dictionary<Vector2, Vector2>();
            if (contours == null) return false;

            var points = new List<Vector2[]>(contours.Count);
            foreach (List<Vector2> contour in contours)
            {
                Vector2[] cleaned = Clean(contour);
                if (cleaned != null) points.Add(cleaned);
            }

            var vertices = new List<Vector2>();
            var colors = new List<Color32>();
            var extrusions = new List<Vector2>();
            var triangles = new List<int>();
            Color32 transparent = new Color32(color.r, color.g, color.b, 0);

            for (int c = 0; c < points.Count; c++)
            {
                Vector2[] contour = points[c];
                float area = SignedArea(contour);
                if (area == 0f) continue;

                // Outward is away from the filled area: a contour inside an odd number of others is a
                // hole. The filled area is on the left of a contour with a positive area, on the right
                // of one with a negative area, and the other way round for holes.
                int containing = 0;
                for (int other = 0; other < points.Count; other++)
                    if (other != c && Contains(points[other], contour[0])) containing++;
                bool hole = containing % 2 == 1;
                float outward = (area > 0f) != hole ? -1f : 1f;

                int count = contour.Length;
                int firstVertex = vertices.Count;
                // Per corner: the inner and outer vertex for the edge before it (A) and after it (B).
                var innerA = new int[count];
                var outerA = new int[count];
                var innerB = new int[count];
                var outerB = new int[count];

                for (int i = 0; i < count; i++)
                {
                    Vector2 point = contour[i];
                    Vector2 before = (point - contour[(i + count - 1) % count]).normalized;
                    Vector2 after = (contour[(i + 1) % count] - point).normalized;
                    Vector2 normalBefore = Left(before) * outward;
                    Vector2 normalAfter = Left(after) * outward;

                    // The miter: the vector whose projection on both normals is 1.
                    float denominator = 1f + Vector2.Dot(normalBefore, normalAfter);
                    Vector2 miter = denominator > 1e-4f ? (normalBefore + normalAfter) / denominator : Vector2.zero;
                    float length = denominator > 1e-4f ? miter.magnitude : float.MaxValue;
                    // At sharp corners the miter is limited to its direction at the miter limit's length; a
                    // path that turns back on itself is limited along its direction.
                    Vector2 limited = length > miterLimit ? (denominator > 1e-4f ? miter / length : before) * miterLimit : miter;

                    if (length <= miterLimit)
                    {
                        innerA[i] = innerB[i] = Add(vertices, colors, extrusions, point, color, -miter);
                        outerA[i] = outerB[i] = Add(vertices, colors, extrusions, point, transparent, miter);
                    }
                    else if (Vector2.Dot(after, normalBefore) < 0f)
                    {
                        // The outline turns away from the outside: bevel the outer edge.
                        innerA[i] = innerB[i] = Add(vertices, colors, extrusions, point, color, -limited);
                        outerA[i] = Add(vertices, colors, extrusions, point, transparent, normalBefore);
                        outerB[i] = Add(vertices, colors, extrusions, point, transparent, normalAfter);
                        triangles.Add(innerA[i]); triangles.Add(outerA[i]); triangles.Add(outerB[i]);
                    }
                    else
                    {
                        // The outline turns towards the outside: bevel the inner edge, and fill the gap
                        // between the bevel and the fill's corner.
                        innerA[i] = Add(vertices, colors, extrusions, point, color, -normalBefore);
                        innerB[i] = Add(vertices, colors, extrusions, point, color, -normalAfter);
                        int corner = Add(vertices, colors, extrusions, point, color, -limited);
                        outerA[i] = outerB[i] = Add(vertices, colors, extrusions, point, transparent, limited);
                        triangles.Add(outerA[i]); triangles.Add(innerA[i]); triangles.Add(innerB[i]);
                        triangles.Add(innerA[i]); triangles.Add(corner); triangles.Add(innerB[i]);
                    }
                    boundary[point] = -limited;
                }

                for (int i = 0; i < count; i++)
                {
                    int next = (i + 1) % count;
                    triangles.Add(innerB[i]); triangles.Add(outerB[i]); triangles.Add(outerA[next]);
                    triangles.Add(innerB[i]); triangles.Add(outerA[next]); triangles.Add(innerA[next]);
                }
            }

            if (triangles.Count == 0) return false;
            fringe.vertices = vertices.ToArray();
            fringe.colors = colors.ToArray();
            fringe.angles = extrusions.ToArray();
            fringe.triangles = triangles.ToArray();
            fringe.RecalculateBounds();
            return true;
        }

        /// <summary>Moves the fill's vertices that lie on the outline inward, as the fringe's inner edge.</summary>
        public static void ExtrudeFill(ref SVGShape fill, Dictionary<Vector2, Vector2> boundary)
        {
            if (fill.vertices == null || boundary == null || boundary.Count == 0) return;
            var extrusions = new Vector2[fill.vertices.Length];
            bool any = false;
            for (int i = 0; i < extrusions.Length; i++)
            {
                Vector2 extrusion;
                if (boundary.TryGetValue(fill.vertices[i], out extrusion))
                {
                    extrusions[i] = extrusion;
                    any = true;
                }
            }
            if (any) fill.angles = extrusions;
        }

        static int Add(List<Vector2> vertices, List<Color32> colors, List<Vector2> extrusions, Vector2 point, Color32 color, Vector2 extrusion)
        {
            vertices.Add(point);
            colors.Add(color);
            extrusions.Add(extrusion);
            return vertices.Count - 1;
        }

        // The contour without repeated points (including a closing point equal to the first), or
        // null if fewer than three points remain.
        static Vector2[] Clean(List<Vector2> contour)
        {
            if (contour == null || contour.Count < 3) return null;
            var points = new List<Vector2>(contour.Count);
            foreach (Vector2 point in contour)
                if (points.Count == 0 || point != points[points.Count - 1]) points.Add(point);
            while (points.Count > 1 && points[points.Count - 1] == points[0]) points.RemoveAt(points.Count - 1);
            return points.Count < 3 ? null : points.ToArray();
        }

        static Vector2 Left(Vector2 direction)
        {
            return new Vector2(-direction.y, direction.x);
        }

        static float SignedArea(Vector2[] contour)
        {
            double area = 0;
            for (int i = 0, j = contour.Length - 1; i < contour.Length; j = i++)
                area += (double)contour[j].x * contour[i].y - (double)contour[i].x * contour[j].y;
            return (float)(area * 0.5);
        }

        static bool Contains(Vector2[] polygon, Vector2 point)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if ((polygon[i].y > point.y) != (polygon[j].y > point.y)
                    && point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
                    inside = !inside;
            }
            return inside;
        }
    }
}
