// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;

namespace SVGImporter.Rendering 
{
    public class SVGPathSegClosePath : SVGPathSeg
    {
        //================================================================================
        public SVGPathSegClosePath(Vector2 value, SVGPathSeg segment) : base()
        {
    //        Debug.Log(string.Format("Close: x: {0}, y: {1}",value.x, value.y));
            this._type = SVGPathSegTypes.Close;
            if(segment != null)
                _previousPoint = segment.currentPoint;
            _currentPoint = value;

            /*
            if (value.x == float.MinValue && value.y == float.MinValue)
            {
                _currentPoint = _previousPoint;
            } else {
                _currentPoint = value;
            } 
            */
        }
    }
}
