

using UnityEngine;

namespace SVGImporter.Rendering 
{
    public class SVGPathSegLinetoHorizontalRel : SVGPathSeg
    {
        //================================================================================
        public SVGPathSegLinetoHorizontalRel(float x, SVGPathSeg segment) : base()
        {
            this._type = SVGPathSegTypes.LineTo_Horizontal_Rel;
            if(segment != null)
                _previousPoint = segment.currentPoint;
            _currentPoint = _previousPoint + new Vector2(x, 0f);
        }
    }
}