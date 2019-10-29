

using UnityEngine;

namespace SVGImporter.Rendering 
{
    public class SVGPathSegLinetoVerticalAbs : SVGPathSeg
    {
        //================================================================================
        public SVGPathSegLinetoVerticalAbs(float y, SVGPathSeg segment) : base()
        {
            this._type = SVGPathSegTypes.LineTo_Vertical_Abs;
            if(segment != null)
                _previousPoint = segment.currentPoint;
            _currentPoint = new Vector2(_previousPoint.x, y);
        }
    }
}
