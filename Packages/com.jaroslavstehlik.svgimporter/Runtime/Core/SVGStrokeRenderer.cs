

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter 
{
    using Rendering;
    using Utils;

    [ExecuteInEditMode]
    [RequireComponent(typeof(ISVGShape), typeof(ISVGRenderer))]
    [AddComponentMenu("Rendering/SVG Stroke Renderer", 21)]
    public class SVGStrokeRenderer : MonoBehaviour, ISVGModify
    {   
        public StrokeLineJoin lineJoin = StrokeLineJoin.miter;
        public StrokeLineCap lineCap = StrokeLineCap.butt;
        public Color32 color = Color.white;
        public float width = 1f;
        public float mitterLimit = 4f;
        public float roundQuality = 10;
        public float[] dashArray;
        public float dashOffset;
        public ClosePathRule closeLine = ClosePathRule.ALWAYS;
        
        protected ISVGShape svgShape;
        protected ISVGRenderer svgRenderer;

        // This method is invoked by Unity when rendering to Camera
        void OnWillRenderObject()
        {
            if(svgRenderer == null || svgRenderer.lastFrameChanged == Time.frameCount) return;
            svgRenderer.UpdateRenderer();
        }

        protected virtual void PrepareForRendering (SVGLayer[] layers, SVGAsset svgAsset, bool force) {
            /*
            if(sharedMesh == null) return;

            SVGPath[] shape = svgShape.shape;
            if(shape != null && shape.Length > 0)
            {
                int[][] submeshes = new int[sharedMesh.subMeshCount][];
                int subMeshCount = sharedMesh.subMeshCount;
                int i, j;
                for(i = 0; i < subMeshCount; i++)
                {
                    submeshes[i] = sharedMesh.GetTriangles(i);
                }

                Mesh[] meshes = new Mesh[shape.Length + 1];

                for(i = 0; i < shape.Length; i++)
                {
                    int pointsLength = shape[i].points.Length - 1;
                    StrokeSegment[] segments = new StrokeSegment[pointsLength];
                    for(j = 0; j < pointsLength; j++)
                    {
                        segments[j] = new StrokeSegment(shape[i].points[j], shape[i].points[j + 1]);
                    }
                    
                    //meshes[i] = SVGLineUtils.StrokeMesh(segments, width, color, lineJoin, lineCap, mitterLimit, dashArray, dashOffset, closeLine, roundQuality);
                }

                CombineInstance[] combineInstances = new CombineInstance[meshes.Length];
                for(i = 0; i < meshes.Length; i++)
                {
                    combineInstances[i].mesh = meshes[i];
                }

                sharedMesh.CombineMeshes(combineInstances, false, false);
            }
            */
        }

        void Init()
        {
            svgShape = GetComponent(typeof(ISVGShape)) as ISVGShape;
            svgRenderer = GetComponent(typeof(ISVGRenderer)) as ISVGRenderer;
            if(svgRenderer != null)
            {
                svgRenderer.AddModifier(this);
                svgRenderer.OnPrepareForRendering += PrepareForRendering;
            }
        }

        void Clear()
        {
            if(svgRenderer != null) 
            {
                svgRenderer.OnPrepareForRendering -= PrepareForRendering;
                svgRenderer.RemoveModifier(this);
                svgRenderer = null;
            }
            svgShape = null;
        }

        public bool active
        {
            get
            {
                return enabled;
            }
        }

        void OnEnable()
        {
            Init();
        }

        void OnDisable()
        {
            Clear();
        }
    }
}
