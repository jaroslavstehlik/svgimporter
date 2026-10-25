// Copyright (C) 2026 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using System.Collections.Generic;
using UnityEngine;

namespace SVGImporter
{
    using Rendering;

    /// <summary>Places imported layers in mesh space: y-up, around the pivot.</summary>
    internal static class SVGLayerPlacement
    {
        /// <summary>
        /// Flips the shapes to y-up and moves them so the pivot (of the content, or of the canvas
        /// when ignoreSVGCanvas is off) is at the origin. viewport is the canvas in mesh units.
        /// </summary>
        public static void Place(List<SVGLayer> layers, Rect viewport, bool ignoreSVGCanvas, Vector2 pivotPoint, out Vector2 offset)
        {
            offset = Vector2.zero;
            if(layers == null) return;
            int layersCount = layers.Count, layersShapesLength = 0, layersShapesVerticesLength = 0;
            for(int i = 0; i < layersCount; i++)
            {
                CorrectSVGLayerShape(layers[i].shapes);
            }

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            // calculate bounds
            for(int i = 0; i < layersCount; i++)
            {
                if(layers[i].shapes == null) continue;
                layersShapesLength = layers[i].shapes.Length;
                for(int j = 0; j < layersShapesLength; j++)
                {
                    Vector2 min = layers[i].shapes[j].bounds.min;
                    Vector2 max = layers[i].shapes[j].bounds.max;
                    
                    if(min.x < minX) minX = min.x;
                    if(max.x > maxX) maxX = max.x;
                    if(min.y < minY) minY = min.y;
                    if(max.y > maxY) maxY = max.y;
                }
            }

            Rect bounds = new Rect(minX, minY, maxX - minX, maxY - minY);
            if(ignoreSVGCanvas)
            {
                offset = new Vector2(bounds.min.x + bounds.size.x * pivotPoint.x,
                                     (bounds.max.y - bounds.size.y * pivotPoint.y));
            } else {
                // The vertices are flipped to y-up above, the viewport isn't: its top edge is at
                // -viewport.min.y.
                offset = new Vector2(viewport.min.x + viewport.size.x * pivotPoint.x,
                                     -(viewport.min.y + viewport.size.y * pivotPoint.y));
            }

            // update vertices
            for(int i = 0; i < layersCount; i++)
            {
                if(layers[i].shapes == null) continue;
                layersShapesLength = layers[i].shapes.Length;
                for(int j = 0; j < layersShapesLength; j++)
                {
                    if(layers[i].shapes[j].vertices == null) continue;
                    layersShapesVerticesLength = layers[i].shapes[j].vertices.Length;
                    for( int k = 0; k < layersShapesVerticesLength; k++)
                    {
                        layers[i].shapes[j].vertices[k] -= offset;
                    }
                    layers[i].shapes[j].bounds.center -= offset;

                    if(layers[i].shapes[j].fill != null)
                    {
                        layers[i].shapes[j].fill.transform = layers[i].shapes[j].fill.transform.Translate(offset);
                    }
                }
            }
        }

        static void CorrectSVGLayerShape(SVGShape[] shapes)
        {

            for(int i = 0; i < shapes.Length; i++)
            {
                int vertexCount = shapes[i].vertexCount;
                if(vertexCount == 0) continue;
                if(shapes[i].fill != null)
                {
                    shapes[i].fill.transform = shapes[i].fill.transform.Scale(1f, -1f);
                }

                for(int j = 0; j < vertexCount; j++)
                {
                    shapes[i].vertices[j].y *= -1f;
                }

                // The antialiasing extrusion vectors are directions in the same space.
                if(shapes[i].angles != null)
                {
                    for(int j = 0; j < shapes[i].angles.Length; j++)
                    {
                        shapes[i].angles[j].y *= -1f;
                    }
                }

                shapes[i].bounds.center = new Vector2(shapes[i].bounds.center.x, shapes[i].bounds.center.y * -1f);
            }
        }
    }
}
