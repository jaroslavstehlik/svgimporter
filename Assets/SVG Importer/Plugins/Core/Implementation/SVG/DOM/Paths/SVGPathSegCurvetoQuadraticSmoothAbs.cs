
using UnityEngine;

namespace SVGImporter.Rendering
{
    public class SVGPathSegCurvetoQuadraticSmoothAbs : SVGPathSegCurvetoQuadratic
    {
        protected Vector2 _controlPoint1 = Vector2.zero;

        //================================================================================
        public SVGPathSegCurvetoQuadraticSmoothAbs(float x, float y, SVGPathSeg segment) : base()
        {
            this._type = SVGPathSegTypes.CurveTo_Quadratic_Smooth_Abs;
            if (segment != null)
                _previousPoint = segment.currentPoint;
            _currentPoint = new Vector2(x, y);
            
            SVGPathSegCurvetoQuadratic pSegment = segment as SVGPathSegCurvetoQuadratic;
            if (pSegment != null)
            {
                _controlPoint1 = _previousPoint + (_previousPoint - pSegment.controlPoint1);
            } else
            {
                _controlPoint1 = _previousPoint;
            }
        }

        //-----
        public override Vector2 controlPoint1
        {
            get
            {
                return _controlPoint1;
            }
        }   
    }
}
