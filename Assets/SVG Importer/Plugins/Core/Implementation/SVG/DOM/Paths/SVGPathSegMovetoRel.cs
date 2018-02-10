// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;

namespace SVGImporter.Rendering 
{
    public class SVGPathSegMovetoRel : SVGPathSeg
    {
        //================================================================================
        public SVGPathSegMovetoRel(float x, float y, SVGPathSeg segment) : base()
        {


            this._type = SVGPathSegTypes.MoveTo_Rel;
            if(segment != null)
                _previousPoint = segment.currentPoint;
            _currentPoint = _previousPoint + new Vector2(x, y);

//            Debug.Log("SVGPathSegMovetoRel: "+ new Vector2(x, y) +", currentPosition: "+_currentPoint+ ", LastPosition: "+_previousPoint);
        }
    }
}
