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
    [RequireComponent(typeof(SVGRenderer))]
    [RequireComponent(typeof(PolygonCollider2D))]
    [AddComponentMenu("Physics 2D/SVG Collider 2D", 20)]
    public class SVGCollider2D : MonoBehaviour 
    {
        [Range(0f, 1f)]
        [SerializeField]
        protected float _quality = 0.9f;
        public float quality
        {
            get {
                return _quality;
            }
            set {
                if(_quality != value)
                {
                    _quality = value;
                    UpdateCollider();;
                }
            }
        }

        [SerializeField]
        protected float _offset = 0f;
        public float offset
        {
            get {
                return _offset;
            }
            set {
                if(_offset != value)
                {
                    _offset = value;
                    UpdateCollider();;
                }
            }
        }

        protected SVGRenderer svgRenderer;
        protected PolygonCollider2D polygonCollider2D;

        void OnValidate()
        {
            UpdateCollider();
        }

        float precision;
        protected virtual void UpdateCollider()
        {
            if(svgRenderer == null)
                svgRenderer = GetComponent<SVGRenderer>();
            if(polygonCollider2D == null)
                polygonCollider2D = GetComponent<PolygonCollider2D>();

            if(svgRenderer.vectorGraphics == null || svgRenderer.vectorGraphics.colliderShape == null || svgRenderer.vectorGraphics.colliderShape.Length == 0)
            {
                polygonCollider2D.pathCount = 0;
                polygonCollider2D.points = null;
            } else {
                SVGPath[] colliderShape = svgRenderer.vectorGraphics.colliderShape;
                polygonCollider2D.pathCount = 0;

                if(_quality < 1f)
                {
                    Bounds bounds = svgRenderer.vectorGraphics.bounds;
                    float finQuality = _quality;
                    if(finQuality < 0.001f)
                        finQuality = 0.001f;

                    precision = Mathf.Max(bounds.size.x, bounds.size.y) / finQuality;
                    if(precision < 0.001f)
                        precision = 0.001f;
                    precision *= 0.05f;
                }

                List<Vector2[]> optimisedPaths = new List<Vector2[]>();
                Vector2[] points;

                for(int i = 0; i < colliderShape.Length; i++)
                {
                    if(_quality < 1f)
                    {
                        points = SVGBezier.Optimise(colliderShape[i].points, precision);
                    } else {
                        points = (Vector2[])colliderShape[i].points.Clone();
                    }

                    //bool clockwiseWinding = SVGGeomUtils.IsWindingClockWise(points);
                    if(_offset != 0f)
                    {
                        points = SVGGeomUtils.OffsetVerts(points, _offset);
                    }

                    if(points != null && points.Length > 2)
                    {
                        optimisedPaths.Add(points);
                    }
                }

                if(optimisedPaths.Count > 0)
                {
                    polygonCollider2D.pathCount = optimisedPaths.Count;
                    for(int i = 0; i < optimisedPaths.Count; i++)
                    {
                        polygonCollider2D.SetPath(i, optimisedPaths[i]);
                    }
                }
            }
        }

        void OnEnable()
        {
            if(svgRenderer == null)
                svgRenderer = GetComponent<SVGRenderer>();

            svgRenderer.onVectorGraphicsChanged += OnVectorGraphicsChanged;
            UpdateCollider();
        }

        void OnDisable()
        {
            if(svgRenderer == null)
                svgRenderer = GetComponent<SVGRenderer>();

            svgRenderer.onVectorGraphicsChanged -= OnVectorGraphicsChanged;
        }

        protected virtual void OnVectorGraphicsChanged(SVGAsset svgAsset)
        {
            UpdateCollider();
        }
    }
}