

using UnityEngine;

namespace SVGImporter.Rendering 
{
    public class SVGPathSegLinetoRel : SVGPathSeg
    {
        //================================================================================
        public SVGPathSegLinetoRel(float x, float y, SVGPathSeg segment) : base()
        {
            this._type = SVGPathSegTypes.LineTo_Rel;
            if(segment != null)
                _previousPoint = segment.currentPoint;
            _currentPoint = _previousPoint + new Vector2(x, y);
        }
    }
}