// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;

namespace SVGImporter.Rendering 
{
    public enum SVGPathSegTypes : ushort
    {
        Unknown = 0,
        Close = 1,
        MoveTo_Abs = 2,
        MoveTo_Rel = 3,
        LineTo_Abs = 4,
        LineTo_Rel = 5,
        CurveTo_Cubic_Abs = 6,
        CurveTo_Cubic_Rel = 7,
        CurveTo_Quadratic_Abs = 8,
        CurveTo_Quadratic_Rel = 9,
        Arc_Abs = 10,
        Arc_Rel = 11,
        LineTo_Horizontal_Abs = 12,
        LineTo_Horizontal_Rel = 13,
        LineTo_Vertical_Abs = 14,
        LineTo_Vertical_Rel = 15,
        CurveTo_Cubic_Smooth_Abs = 16,
        CurveTo_Cubic_Smooth_Rel = 17,
        CurveTo_Quadratic_Smooth_Abs = 18,
        CurveTo_Quadratic_Smooth_Rel = 19
    }

    public abstract class SVGPathSeg
    {
        protected SVGPathSegTypes _type;
        public SVGPathSegTypes type {
            get {
                return _type;
            }
        }

        protected int _index = -1;
        public int index {
            get {
                return _index;
            }
        }

        protected SVGPathSeg _prevSeg;
        protected Vector2 _currentPoint = Vector2.zero;
        protected Vector2 _previousPoint = Vector2.zero;

        public int SetIndex(int value)
        {
            _index = value;
            return value;
        }

        public void SetPosition(Vector2 value)
        {
            _currentPoint = value;
        }

        public void SetPreviousSegment(SVGPathSeg prevSeg)
        {
            if(prevSeg == null)
                return;

            this._prevSeg = prevSeg;
            _previousPoint = prevSeg.currentPoint;
        }

        protected SVGPathSegList _segList;
        /***********************************************************************************/
        internal void SetList(SVGPathSegList segList)
        {
            this._segList = segList;
        }

        public SVGPathSeg previousSeg
        {
            get { return _segList.GetPreviousSegment(_index); }
        }
        /***********************************************************************************/
        public Vector2 currentPoint 
        { 
            get{
                return _currentPoint;
            } 
        }

        public Vector2 previousPoint
        {
            get
            {
                return _previousPoint;
            }
        }
    }
}
