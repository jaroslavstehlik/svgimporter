// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;

namespace SVGImporter.Rendering 
{
    public class SVGPathSegCurvetoCubicAbs : SVGPathSegCurvetoCubic
    {
        protected Vector2 _controlPoint1 = Vector2.zero;
        protected Vector2 _controlPoint2 = Vector2.zero;

        //================================================================================
        public SVGPathSegCurvetoCubicAbs(float x1, float y1, float x2, float y2, float x, float y, SVGPathSeg segment) : base()
        {
    //        Debug.Log(string.Format("CurveCubicAbs x1: {0}, y1: {1}, x2: {2}, y2: {3}, x: {4}, y: {5}", x1, y1, x2, y2, x, y));
            this._type = SVGPathSegTypes.CurveTo_Cubic_Abs;
            if(segment != null)
                _previousPoint = segment.currentPoint;
            _currentPoint = new Vector2(x, y);        
            _controlPoint1 = new Vector2(x1, y1);
            _controlPoint2 = new Vector2(x2, y2);

    //        Debug.Log(string.Format("_controlPoint1: {0}, _controlPoint2: {1}, _currentPoint: {2}",_controlPoint1,_controlPoint2, _currentPoint));
        }

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
