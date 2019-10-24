// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace SVGImporter.Utils
{        
    public class SVGDebugPoints : MonoBehaviour
    {
#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 1f, 1f, 0.2f);
            RenderGizmos();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 1f, 1f, 1f);
            RenderGizmos();
        }

        void RenderGizmos()
        {
            int count = transform.childCount;
            for(int i = 0; i < count; i++)
            {
                Vector3 position = transform.GetChild(i).localPosition;
                Gizmos.DrawSphere(position, UnityEditor.HandleUtility.GetHandleSize(position) * 0.1f);
            }
            
            if(count > 1)
            {
                Vector3 lastPosition = transform.GetChild(0).transform.localPosition;
                for(int i = 1; i < count; i++)
                {
                    Vector3 currentPosition = transform.GetChild(i).localPosition;
                    Gizmos.DrawLine(lastPosition, currentPosition);
                    lastPosition = currentPosition;
                }
            }
        }
#endif
    }
}
