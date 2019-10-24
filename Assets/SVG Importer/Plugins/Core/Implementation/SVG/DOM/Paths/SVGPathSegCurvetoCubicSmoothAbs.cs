// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
