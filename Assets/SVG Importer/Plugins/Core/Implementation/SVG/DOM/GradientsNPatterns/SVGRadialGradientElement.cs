// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;
using System.Collections.Generic;

namespace SVGImporter.Rendering 
{
    using Document;
    using Utils;

    public class SVGRadialGradientElement : SVGGradientElement
    {
        private SVGLength _cx;
        private SVGLength _cy;
        private SVGLength _r;
        private SVGLength _fx;
        private SVGLength _fy;
      
        public SVGLength cx
        {
            get { return _cx; }
        }

        public SVGLength cy
        {
            get { return _cy; }
        }

        public SVGLength r
        {
            get { return _r; }
        }

        public SVGLength fx
        {
            get { return _fx; }
        }

        public SVGLength fy
        {
            get { return _fy; }
        }
      
        public SVGRadialGradientElement(SVGParser xmlImp, Node node) : base(xmlImp, node)
        {
            string temp;
            temp = _attrList.GetValue("cx");
            _cx = new SVGLength((temp == "") ? "50%" : temp);

            temp = _attrList.GetValue("cy");
            _cy = new SVGLength((temp == "") ? "50%" : temp);

            temp = _attrList.GetValue("r");
            _r = new SVGLength((temp == "") ? "50%" : temp);

            temp = _attrList.GetValue("fx");
            _fx = new SVGLength((temp == "") ? "50%" : temp);

            temp = _attrList.GetValue("fy");
            _fy = new SVGLength((temp == "") ? "50%" : temp);
        }
    }
}
