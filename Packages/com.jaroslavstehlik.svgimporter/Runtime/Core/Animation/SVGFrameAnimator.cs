

using UnityEngine;
using System.Collections;

namespace SVGImporter
{
    [ExecuteInEditMode]
    [HelpURL(SVGDocumentation.url)]
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

        private SVGRenderer _svgRenderer;
        public SVGRenderer svgRenderer
        {
            get {
                if(_svgRenderer == null) _svgRenderer = GetComponent<SVGRenderer>();
                return _svgRenderer;
            }
        }

        private SVGImage _svgImage;
        public SVGImage svgImage
        {
            get {
                if(_svgImage == null) _svgImage = GetComponent<SVGImage>();
                return _svgImage;
            }
        }

        private void OnEnable()
        {
            UpdateMesh();
        }

        private void UpdateMesh()
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
