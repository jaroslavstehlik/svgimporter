

using UnityEngine;

namespace SVGImporter.Rendering 
{
    public class SVGPathSegLinetoVerticalRel : SVGPathSeg
    {
        //================================================================================
        public SVGPathSegLinetoVerticalRel(float y, SVGPathSeg segment) : base()
        {
            this._type = SVGPathSegTypes.LineTo_Vertical_Rel;
            if(segment != null)
                _previousPoint = segment.currentPoint;
            _currentPoint = _previousPoint + new Vector2(0f, y);
        }
    }
}