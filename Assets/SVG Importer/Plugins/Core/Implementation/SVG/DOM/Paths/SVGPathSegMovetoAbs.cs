// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;

namespace SVGImporter.Rendering 
{
    public class SVGPathSegMovetoAbs : SVGPathSeg
    {
        //================================================================================
        public SVGPathSegMovetoAbs(float x, float y, SVGPathSeg segment) : base()
        {
    //        Debug.Log(string.Format("MovetoAbs: x: {0}, y: {1}",x, y));
            this._type = SVGPathSegTypes.MoveTo_Abs;
            if(segment != null)
            {
                _previousPoint = segment.currentPoint;
            } else {
                _previousPoint = _currentPoint;
            }
            _currentPoint = new Vector2(x, y);
        }   
    }
}
