

using UnityEngine;

namespace SVGImporter.Rendering
{
    public class SVGPathSegCurvetoCubicSmoothAbs : SVGPathSegCurvetoCubic
    {
        protected Vector2 _controlPoint1 = Vector2.zero;
        protected Vector2 _controlPoint2 = Vector2.zero;

        //================================================================================
        public SVGPathSegCurvetoCubicSmoothAbs(float x2, float y2, float x, float y, SVGPathSeg segment) : base()
        {
            this._type = SVGPathSegTypes.CurveTo_Cubic_Smooth_Abs;
            if (segment != null)
                _previousPoint = segment.currentPoint;
            _currentPoint = new Vector2(x, y);
            
            SVGPathSegCurvetoCubic pSegment = segment as SVGPathSegCurvetoCubic;
            if (pSegment != null)
            {
                _controlPoint1 = _previousPoint + (_previousPoint - pSegment.controlPoint2);
            } else
            {
                _controlPoint1 = _previousPoint;
            }
            
            _controlPoint2 = new Vector2(x2, y2);
        }

        //-----
        public override Vector2 controlPoint1
        {
            get
            {
                return _controlPoint1;
            }
        }
        //-----
        public override Vector2 controlPoint2
        {
            get
            {
                return _controlPoint2;
            }
        }
    }
}
