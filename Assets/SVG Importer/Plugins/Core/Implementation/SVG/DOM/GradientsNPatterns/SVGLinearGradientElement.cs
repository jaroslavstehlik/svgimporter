// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;

namespace SVGImporter.Rendering 
{
    using Document;
    using Utils;

    public class SVGLinearGradientElement : SVGGradientElement
    {
        private SVGLength _x1;
        private SVGLength _y1;
        private SVGLength _x2;
        private SVGLength _y2;

        public SVGLength x1
        {
            get { return _x1; }
        }

        public SVGLength y1
        {
            get { return _y1; }
        }

        public SVGLength x2
        {
            get { return _x2; }
        }

        public SVGLength y2
        {
            get { return _y2; }
        }

        public SVGLinearGradientElement(SVGParser xmlImp, Node node) : base(xmlImp, node)
        {
            string temp;
            temp = _attrList.GetValue("x1");
            _x1 = new SVGLength((temp == "") ? "0%" : temp);

            temp = this._attrList.GetValue("y1");
            _y1 = new SVGLength((temp == "") ? "0%" : temp);

            temp = this._attrList.GetValue("x2");
            _x2 = new SVGLength((temp == "") ? "100%" : temp);

            temp = this._attrList.GetValue("y2");
            _y2 = new SVGLength((temp == "") ? "0%" : temp);
        }
    }
}