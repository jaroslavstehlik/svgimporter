// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using System.Collections;

namespace SVGImporter
{
    public class SVGRenderTexture
    {
        const int EMPTY_LAYER = 31;
        protected static Camera _camera;
        protected static SVGRenderer _renderer;

        protected static Camera camera
        {
            get
            {
                if (_camera == null)
                {
                    GameObject go = new GameObject("SVG Camera");
                    _camera = go.AddComponent<Camera>();
                    _camera.cullingMask = 1 << EMPTY_LAYER;
                    _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                    _camera.clearFlags = CameraClearFlags.Color;
                    _camera.orthographic = true;
                    _camera.enabled = false;
                }

                return _camera;
            }
        }

        protected static void RemoveCamera()
        {
            if (_camera != null)
            {
                _camera.targetTexture = null;
                GameObject.Destroy(_camera.gameObject);
                _camera = null;
            }
        }

        protected static SVGRenderer renderer
        {
            get
            {                
                if (_renderer == null)
                {
                    GameObject go = new GameObject("editor SVG Renderer");
                    go.layer = EMPTY_LAYER;
                    _renderer = go.AddComponent<SVGRenderer>();
                }

                return _renderer;
            }
        }

        protected static void RemoveSVGRenderer()
        {
            if (_renderer != null)
            {
                _renderer.vectorGraphics = null;
                GameObject.Destroy(_renderer.gameObject);
                _renderer = null;
            }
        }

        protected static RenderTexture GetRenderTexture(SVGAsset svgAsset, Rect textureSize)
        {            
            float aspect = 1f;
            if (svgAsset != null)
                aspect = svgAsset.bounds.size.x / svgAsset.bounds.size.y;
            int _previewResolution = Mathf.CeilToInt(textureSize.width);
            RenderTexture rt = new RenderTexture(_previewResolution, 
                                                  Mathf.CeilToInt(_previewResolution / aspect), 
                                                  24, 
                                                  RenderTextureFormat.Default, 
                                                  RenderTextureReadWrite.Default);
            rt.antiAliasing = 8;
            rt.Create();
            return rt;
        }

        // Call this function during Start or Awake only once
        public static RenderTexture RenderSVG(SVGAsset svgAsset, Rect textureSize)
        {                        
            Bounds bounds = svgAsset.bounds;

            // Initialize
            renderer.transform.position = camera.transform.forward * (camera.nearClipPlane + svgAsset.bounds.size.z + 1f) - svgAsset.bounds.center;
            renderer.vectorGraphics = svgAsset;

            if (bounds.size.x > bounds.size.y)
            {
                camera.orthographicSize = Mathf.Min(bounds.size.x, bounds.size.y) * 0.5f;
            } else
            {
                camera.orthographicSize = Mathf.Max(bounds.size.x, bounds.size.y) * 0.5f;
            }

            RenderTexture rt = GetRenderTexture(svgAsset, textureSize);
            camera.targetTexture = rt;
            camera.Render();
            camera.targetTexture = null;
            RemoveSVGRenderer();
            RemoveCamera();

            return rt;
        }
    }
}
        


        