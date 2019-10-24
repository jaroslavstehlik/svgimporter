// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter 
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(ISVGRenderer))]
    [AddComponentMenu("Rendering/SVG Modifiers/Sphere Modifier", 22)]
    public class SVGSphereModifier : SVGModifier {

        public Transform center;
        public float radius;
        public float intensity;

        protected override void PrepareForRendering (SVGLayer[] layers, SVGAsset svgAsset, bool force) 
        {
            if (!active) return;
            if (center == null) return;
            Vector2 position = center.position;
            Vector2 direction, directionNormalized;
            float distance;

            if(layers == null) return;
            int totalLayers = layers.Length;
            if(!useSelection)
            {
                for(int i = 0; i < totalLayers; i++)
                {
                    if(layers[i].shapes == null) continue;
                    int shapesLength = layers[i].shapes.Length;
                    for(int j = 0; j < shapesLength; j++)
                    {
                        int vertexCount = layers[i].shapes[j].vertexCount;
                        for(int k = 0; k < vertexCount; k++)
                        {
                            direction = position - layers[i].shapes[j].vertices[k];
                            distance = Mathf.Sqrt(direction.x * direction.x + direction.y * direction.y);
                            directionNormalized = Vector2.zero;
                            if(distance > 0f)
                            {
                                directionNormalized.x = direction.x / distance;
                                directionNormalized.y = direction.y / distance;
                            }

                            layers[i].shapes[j].vertices[k] += directionNormalized * (1f - Mathf.Clamp01(distance / radius)) * intensity;
                        }
                    }
                }
            } else
            {
                if (layerSelection.layers != null)
                {
                    int selectionCount = layerSelection.layers.Count;
                    for (int i = 0; i < selectionCount; i++)
                    {
                        int layerIndex = layerSelection.layers[i];
                        if (layerIndex < 0 || layerIndex >= totalLayers) continue;
                        if (layers[layerIndex].shapes == null) continue;
                        int shapesLength = layers[layerIndex].shapes.Length;
                        for (int j = 0; j < shapesLength; j++)
                        {
                            int vertexCount = layers[layerIndex].shapes[j].vertexCount;
                            for (int k = 0; k < vertexCount; k++)
                            {
                                direction = position - layers[layerIndex].shapes[j].vertices[k];
                                distance = Mathf.Sqrt(direction.x * direction.x + direction.y * direction.y);
                                directionNormalized = Vector2.zero;
                                if (distance > 0f)
                                {
                                    directionNormalized.x = direction.x / distance;
                                    directionNormalized.y = direction.y / distance;
                                }

                                layers[layerIndex].shapes[j].vertices[k] += directionNormalized * (1f - Mathf.Clamp01(distance / radius)) * intensity;
                            }
                        }
                    }
                }
            }
        }
    }
}
