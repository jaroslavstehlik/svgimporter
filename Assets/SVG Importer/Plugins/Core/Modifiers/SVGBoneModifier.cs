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
    [AddComponentMenu("Rendering/SVG Modifiers/Bone Modifier", 22)]
    public class SVGBoneModifier : SVGModifier
    {
        [System.Serializable]
        public struct Properties
        {
            public Vector2 start;
            public Vector2 end;

            public float startRadius;
            public float startRadiusFeather;

            public float endRadius;
            public float endRadiusFeather;
            
            public Properties(Vector2 start, Vector2 end, float startRadius, float startRadiusFeather, float endRadius, float endRadiusFeather)
            {
                this.start = start;
                this.end = end;
                this.startRadius = startRadius;
                this.startRadiusFeather = startRadiusFeather;

                this.endRadius = endRadius;
                this.endRadiusFeather = endRadiusFeather;
            }            
        }

        public Properties properties = new Properties(-Vector2.right, Vector2.right, 1f, 0.25f, 1f, 0.25f);
        public Transform target;

        protected override void PrepareForRendering (SVGLayer[] layers, SVGAsset svgAsset, bool force) 
        {
            if (!active) return;
            if (layers == null) return;
            if (target == null) return;
            int totalLayers = layers.Length;

            Vector2 offset = Vector2.Lerp(properties.start, properties.end, 0.5f);
            Vector2 vector = (properties.end - properties.start);
            Vector2 direction = Vector2.zero;
            float distance = vector.magnitude;
            if(distance > 0)
            {
                direction.x = vector.x / distance;
                direction.y = vector.y / distance;
            }
            
            Vector3 startPosition = -Vector3.right * distance * 0.5f;
            Vector3 endPosition = Vector3.right * distance * 0.5f;
            
            Matrix4x4 matrix = transform.worldToLocalMatrix * target.localToWorldMatrix;
            Matrix4x4 matrixInverse = matrix.inverse;

            Matrix4x4 localMatrix = Matrix4x4.TRS(offset, Quaternion.LookRotation(direction), Vector3.one);

            float startRadiusFeather = properties.startRadius + properties.startRadiusFeather;
            float endRadiusFeather = properties.endRadius + properties.endRadiusFeather;

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
                            Vector3 localPosition = localMatrix.MultiplyPoint3x4(layers[i].shapes[j].vertices[k]);
                            
                            float weight = 0;
                            if (localPosition.x < startPosition.x)
                            {
                                float vertexDistanceFromStart = Vector2.Distance(startPosition, localPosition);
                                weight = 1 - Mathf.InverseLerp(properties.startRadius, startRadiusFeather, vertexDistanceFromStart);
                            } else if (localPosition.x > endPosition.x)
                            {
                                float vertexDistanceFromEnd = Vector2.Distance(endPosition, localPosition);
                                weight = 1 - Mathf.InverseLerp(properties.endRadius, endRadiusFeather, vertexDistanceFromEnd);
                            } else
                            {
                                float progress = (1 + localPosition.x) * 0.5f;                                
                                weight = 1 - Mathf.InverseLerp(Mathf.Lerp(properties.startRadius, properties.endRadius, progress), Mathf.Lerp(startRadiusFeather, endRadiusFeather, progress), Mathf.Abs(localPosition.y));
                            }

                            //layers[i].shapes[j].vertices[k] = Vector3.Lerp(layers[i].shapes[j].vertices[k], matrix.MultiplyPoint3x4(layers[i].shapes[j].vertices[k]), weight);
                            layers[i].shapes[j].vertices[k] = Vector3.Lerp(layers[i].shapes[j].vertices[k], Vector3.zero, weight);
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
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (target != null)
            {
                Matrix4x4 gizmosMatrix = UnityEditor.Handles.matrix;
                UnityEditor.Handles.matrix = transform.localToWorldMatrix;
                
                UnityEditor.Handles.DrawWireDisc(properties.start, transform.forward, properties.startRadius);
                UnityEditor.Handles.DrawWireDisc(properties.start, transform.forward, properties.startRadius + properties.startRadiusFeather);

                UnityEditor.Handles.DrawWireDisc(properties.end, transform.forward, properties.endRadius);
                UnityEditor.Handles.DrawWireDisc(properties.end, transform.forward, properties.endRadius + properties.endRadiusFeather);

                Vector2 offset = Vector2.Lerp(properties.start, properties.end, 0.5f);
                Vector2 vector = (properties.end - properties.start);
                Vector2 direction = vector.normalized;
                Vector2 up = Vector3.Cross(direction, Vector3.forward);

                UnityEditor.Handles.DrawLine(properties.start + up * properties.startRadius, properties.end + up * properties.endRadius);
                UnityEditor.Handles.DrawLine(properties.start - up * properties.startRadius, properties.end - up * properties.endRadius);

                UnityEditor.Handles.DrawLine(properties.start + up * (properties.startRadius + properties.startRadiusFeather), properties.end + up * (properties.endRadius + properties.endRadiusFeather));
                UnityEditor.Handles.DrawLine(properties.start - up * (properties.startRadius + properties.startRadiusFeather), properties.end - up * (properties.endRadius + properties.endRadiusFeather));

                UnityEditor.Handles.matrix = gizmosMatrix;
            }
        }
#endif

        public static Vector2 GetClosestPointOnLineSegment(Vector2 A, Vector2 B, Vector2 P)
        {
            Vector2 AP = P - A;       //Vector from A to P   
            Vector2 AB = B - A;       //Vector from A to B  

            float magnitudeAB = AB.SqrMagnitude();     //Magnitude of AB vector (it's length squared)     
            float ABAPproduct = Vector2.Dot(AP, AB);    //The DOT product of a_to_p and a_to_b     
            float distance = ABAPproduct / magnitudeAB; //The normalized "distance" from a to your closest point  

            if (distance < 0)     //Check if P projection is over vectorAB     
            {
                return A;

            }
            else if (distance > 1)
            {
                return B;
            }
            else
            {
                return A + AB * distance;
            }
        }
    }
}
