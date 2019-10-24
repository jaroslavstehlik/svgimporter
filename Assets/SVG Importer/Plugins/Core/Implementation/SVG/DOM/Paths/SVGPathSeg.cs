// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
