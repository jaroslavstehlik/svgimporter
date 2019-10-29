

using UnityEngine;

namespace SVGImporter.Rendering
{
    public abstract class SVGPathSegCurvetoQuadratic : SVGPathSeg
    {
        public abstract Vector2 controlPoint1 { get; }
    }
}
