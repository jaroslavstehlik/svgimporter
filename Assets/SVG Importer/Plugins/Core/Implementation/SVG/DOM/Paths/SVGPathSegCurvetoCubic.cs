

using UnityEngine;

namespace SVGImporter.Rendering
{
    public abstract class SVGPathSegCurvetoCubic : SVGPathSeg
    {
        public abstract Vector2 controlPoint1 { get; }

        public abstract Vector2 controlPoint2 { get; }
    }
}