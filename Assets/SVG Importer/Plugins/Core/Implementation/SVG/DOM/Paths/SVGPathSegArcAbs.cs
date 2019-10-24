// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;

namespace SVGImporter.Rendering 
{
    public class SVGPathSegArcAbs : SVGPathSeg
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
        public SVGPathSegArcAbs(float r1, float r2, float angle, bool largeArcFlag, bool sweepFlag, float x, float y, SVGPathSeg segment) : base()
        {    
            this._type = SVGPathSegTypes.Arc_Abs;
            if(segment != null)
                _previousPoint = segment.currentPoint;
            _currentPoint = new Vector2(x, y);

            this._r1 = r1;
            this._r2 = r2;
            this._angle = angle;
            this._largeArcFlag = largeArcFlag;
            this._sweepFlag = sweepFlag;
        }
    }
}
