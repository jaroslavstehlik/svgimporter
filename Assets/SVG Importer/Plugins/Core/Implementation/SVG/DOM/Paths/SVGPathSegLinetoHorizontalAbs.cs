

using UnityEngine;

namespace SVGImporter.Rendering 
{
    public class SVGPathSegLinetoHorizontalAbs : SVGPathSeg
    {
        //================================================================================
        public SVGPathSegLinetoHorizontalAbs(float x, SVGPathSeg segment) : base()
        {
            this._type = SVGPathSegTypes.LineTo_Horizontal_Abs;
            if(segment != null)
                _previousPoint = segment.currentPoint;
            _currentPoint = new Vector2(x, _previousPoint.y);
        }
    }
}
