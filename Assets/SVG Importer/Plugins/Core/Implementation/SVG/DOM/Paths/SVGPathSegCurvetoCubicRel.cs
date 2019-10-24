// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;

namespace SVGImporter.Rendering 
{
    public class SVGPathSegCurvetoCubicRel : SVGPathSegCurvetoCubic
    {
        protected Vector2 _controlPoint1 = Vector2.zero;
        protected Vector2 _controlPoint2 = Vector2.zero;

        //================================================================================
        public SVGPathSegCurvetoCubicRel(float x1, float y1, float x2, float y2, float x, float y, SVGPathSeg segment) : base()
        {
            this._type = SVGPathSegTypes.CurveTo_Cubic_Rel;
            if(segment != null)
                _previousPoint = segment.currentPoint;
            _currentPoint = _previousPoint + new Vector2(x, y);
            _controlPoint1 = _previousPoint + new Vector2(x1, y1);
            _controlPoint2 = _previousPoint + new Vector2(x2, y2);
            /*
            Debug.Log(string.Format("CurveCubicRel x1: {0}, y1: {1}, x2: {2}, y2: {3}, x: {4}, y: {5}", x1, y1, x2, y2, x, y)+"\n"+
                      string.Format("_controlPoint1: {0}, _controlPoint2: {1}, _currentPoint: {2}",_controlPoint1,_controlPoint2, _currentPoint)
                      );
            */
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
