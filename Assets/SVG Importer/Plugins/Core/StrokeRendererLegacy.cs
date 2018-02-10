// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

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
