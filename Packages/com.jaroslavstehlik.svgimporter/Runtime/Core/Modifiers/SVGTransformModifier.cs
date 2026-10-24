

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter 
{    
    [ExecuteInEditMode]
    [RequireComponent(typeof(ISVGRenderer))]
    [AddComponentMenu("Rendering/SVG Modifiers/Transform Modifier", 22)]
    public class SVGTransformModifier : SVGModifier
    {
        [System.Serializable]
        public struct Properties
        {
            public Vector3 positon;
            public Vector3 rotation;
            public Vector3 scale;

            public Properties(Vector3 positon, Vector3 rotation, Vector3 scale)
            {
                this.positon = positon;
                this.rotation = rotation;
                this.scale = scale;
            }

            public Matrix4x4 transformMatrix
            {
                get
                {
                    return Matrix4x4.TRS(positon, Quaternion.Euler(rotation), scale);
                }
            }
        }

        public Properties properties = new Properties(Vector3.zero, Vector3.zero, Vector3.one);
        public Transform target;
        protected override void PrepareForRendering (SVGLayer[] layers, SVGAsset svgAsset, bool force) 
        {
            if (!active) return;
            if (layers == null) return;
            int totalLayers = layers.Length;

            Matrix4x4 matrix = properties.transformMatrix;

            if (target != null)
            {
                matrix *= transform.worldToLocalMatrix * target.localToWorldMatrix;
            }

            if (!useSelection)
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
                            layers[i].shapes[j].vertices[k] = matrix.MultiplyPoint3x4(layers[i].shapes[j].vertices[k]);
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
                                layers[layerIndex].shapes[j].vertices[k] = matrix.MultiplyPoint3x4(layers[layerIndex].shapes[j].vertices[k]);
                            }
                        }
                    }
                }
            }
        }
    }
}
