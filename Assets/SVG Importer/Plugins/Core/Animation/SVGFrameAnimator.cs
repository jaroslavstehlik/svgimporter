// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
