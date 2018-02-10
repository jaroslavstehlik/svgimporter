// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using System.Collections;

namespace SVGImporter
{
    [ExecuteInEditMode]
    [AddComponentMenu("Miscellaneous/SVG Frame Animator", 20)]
    public class SVGFrameAnimator : MonoBehaviour {

        /// <summary>
        /// Frame by frame animation array..
        /// </summary>
        public SVGAsset[] frames;
        /// <summary>
        /// Current visible frame..
        /// </summary>
        public float frameIndex;
        float lastFrameIndex;

        protected SVGRenderer _svgRenderer;
        public SVGRenderer svgRenderer
        {
            get {
                if(_svgRenderer == null) _svgRenderer = GetComponent<SVGRenderer>();
                return _svgRenderer;
            }
        }

        protected SVGImage _svgImage;
        public SVGImage svgImage
        {
            get {
                if(_svgImage == null) _svgImage = GetComponent<SVGImage>();
                return _svgImage;
            }
        }

        protected virtual void OnEnable()
        {
            UpdateMesh();
        }

        protected virtual void UpdateMesh()
        {
            if(frames == null || frames.Length == 0)
                return;

            int vectorGraphicsIndex = (int)Mathf.Repeat(frameIndex, frames.Length);
            if(svgRenderer != null)
            {
                if (svgRenderer.vectorGraphics != frames [vectorGraphicsIndex])
                {
                    svgRenderer.vectorGraphics = frames [vectorGraphicsIndex];
                }
            }
            if(svgImage != null)
            {
                if (svgImage.vectorGraphics != frames [vectorGraphicsIndex])
                {
                    svgImage.vectorGraphics = frames [vectorGraphicsIndex];
                }
            }
        }

    	void LateUpdate()
        {
            if(frameIndex != lastFrameIndex)
            {
                UpdateMesh();
                lastFrameIndex = frameIndex;
            }
        }

    }
}
