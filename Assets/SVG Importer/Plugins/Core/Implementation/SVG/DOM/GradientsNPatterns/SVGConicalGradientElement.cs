// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using System.Collections.Generic;

namespace SVGImporter.Rendering 
{
    using Document;
    using Utils;

    public class SVGConicalGradientElement : SVGGradientElement
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
      
        public SVGConicalGradientElement(SVGParser xmlImp, Node node) : base(xmlImp, node)
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
