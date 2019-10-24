// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter 
{
    using Rendering;
    using Utils;

    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class StrokeRendererLegacy : MonoBehaviour {

        [System.Serializable]
        public struct StrokePoint
        {
            public Vector2 position;
            public Transform transform;

            public Vector2 GetPosition()
            {
                if(transform == null) return position;
                return transform.position;
            }
        }

        public StrokePoint[] points;

        [Header("Line Style")]
        public StrokeLineJoin lineJoin = StrokeLineJoin.miter;
        public StrokeLineCap lineCap = StrokeLineCap.butt;
        public Color32 color = Color.white;
        public float width = 1f;
        public float mitterLimit = 4f;
        public float roundQuality = 10;
        public float[] dashArray;
        public float dashOffset;
        public ClosePathRule closeLine = ClosePathRule.ALWAYS;

        protected MeshFilter _meshFilter;
        public MeshFilter meshFilter
        {
            get {
                if(_meshFilter == null)
                {
                    _meshFilter = GetComponent<MeshFilter>();
                }
                return _meshFilter;
            }
        }

        protected MeshRenderer _meshRenderer;
        public MeshRenderer meshRenderer
        {
            get {
                if(_meshRenderer == null)
                {
                    _meshRenderer = GetComponent<MeshRenderer>();
                }
                return _meshRenderer;
            }
        }

        void LateUpdate()
        {
            if(points == null || points.Length <= 1) return;
            RenderStroke();
        }

        protected virtual void RenderStroke () 
        {
            int pointsLength = points.Length;
            int pointsLengthMinusOne = pointsLength - 1;
            StrokeSegment[] segments = new StrokeSegment[pointsLengthMinusOne];
            for(int i = 0; i < pointsLengthMinusOne; i++)
            {
                segments[i] = new StrokeSegment(points[i].GetPosition(), points[i + 1].GetPosition());
            }
            meshFilter.sharedMesh =  SVGLineUtils.StrokeMesh(segments, width, color, lineJoin, lineCap, mitterLimit, dashArray, dashOffset, closeLine, roundQuality);
        }
    }
}
