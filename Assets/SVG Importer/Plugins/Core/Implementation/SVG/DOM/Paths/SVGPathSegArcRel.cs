// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;

namespace SVGImporter.Rendering 
{
    public class SVGPathSegArcRel : SVGPathSeg
    {
        private float _r1 = 0f;
        public float r1
        {
            get { return this._r1; }
        }
        private float _r2 = 0f;
        public float r2
        {
            get { return this._r2; }
        }
        private float _angle = 0f;
        public float angle
        {
            get { return this._angle; }
        }
        private bool _largeArcFlag = false;
        public bool largeArcFlag
        {
            get { return this._largeArcFlag; }
        }
        private bool _sweepFlag = false;
        public bool sweepFlag
        {
            get { return this._sweepFlag; }
        }
        //================================================================================

        //-----

        //================================================================================
        public SVGPathSegArcRel(float r1, float r2, float angle, bool largeArcFlag, bool sweepFlag, float x, float y, SVGPathSeg segment) : base()
        {
            this._type = SVGPathSegTypes.Arc_Rel;
            if(segment != null)
                _previousPoint = segment.currentPoint;
            _currentPoint = _previousPoint + new Vector2(x, y);

            this._r1 = r1;
            this._r2 = r2;
            this._angle = angle;
            this._largeArcFlag = largeArcFlag;
            this._sweepFlag = sweepFlag;
        }
    }
}
