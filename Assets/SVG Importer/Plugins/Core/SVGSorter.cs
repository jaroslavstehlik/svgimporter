// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;
using System.Collections;

namespace SVGImporter
{
    using Utils;
    using Rendering;

    [ExecuteInEditMode]
    [AddComponentMenu("Rendering/SVG Sorter", 20)]
    public class SVGSorter : MonoBehaviour {

        public float depthOffset = 0.01f;
        public int layerIndex = 0;
        public bool sort = true;

        float zOffsetStart;
        int layerIndexStart;

        #if UNITY_EDITOR
    	void LateUpdate()
        {
            if(sort) Sort();
        }
        #endif

        public void Sort()
        {
            zOffsetStart = transform.position.z;
            SortRecursive(transform, ref zOffsetStart, ref layerIndexStart);
        }

        void SortRecursive(Transform transform, ref float zOffset, ref int layerIndex)
        {
            int childCount = transform.childCount;
            Transform child;
            SVGRenderer renderer;
            SVGAsset vectorGraphics;
            Bounds bounds;
            Vector3 position;
            for(int i = 0; i < childCount; i++)
            {
                child = transform.GetChild(i);
                renderer = child.GetComponent<SVGRenderer>();
                if(renderer != null)
                {
                    if(!renderer.overrideSorter)
                    {
                        vectorGraphics = renderer.vectorGraphics;
                        if(vectorGraphics != null)
                        {
                            bounds = vectorGraphics.bounds;
                            position = renderer.transform.position;
                            zOffset += bounds.size.z * Mathf.Sign(depthOffset);
                            position.z = zOffset;
                            renderer.transform.position = position;
                            zOffset += depthOffset;
                            renderer.sortingOrder = layerIndex++;
                        }
                    } else {
                        if(renderer.overrideSorterChildren) continue;
                    }
                }

                SortRecursive(child, ref zOffset, ref layerIndex);
            }
        }
    }
}
