// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

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