// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

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