// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using System.Collections.Generic;

namespace SVGImporter.Rendering 
{
    using Document;

    public enum SVGSpreadMethod : ushort
    {
        Unknown = 0,
        Pad = 1,
        Reflect = 2,
        Repeat = 3
    }

    public enum SVGGradientUnit : ushort
    {
        UserSpaceOnUse = 0,
        ObjectBoundingBox = 1
    }

    public class SVGGradientElement
    {
        private SVGGradientUnit _gradientUnits;
        private SVGSpreadMethod _spreadMethod;
        private SVGTransformList _gradientTransform;

        private string _id;
        private SVGParser _xmlImp;
        private List<SVGStopElement> _stopList;
        protected AttributeList _attrList;

        public SVGGradientUnit gradientUnits
        {
            get { return _gradientUnits; }
        }

        public SVGSpreadMethod spreadMethod
        {
            get { return _spreadMethod; }
        }

        public string id
        {
            get { return _id; }
        }

        public List<SVGStopElement> stopList
        {
            get { return _stopList; }
        }
        public SVGTransformList gradientTransform
        {
            get { return _gradientTransform; }
        }

        public SVGGradientElement(SVGParser xmlImp, Node node)
        {
            _attrList = node.attributes;
            _xmlImp = xmlImp;
            _stopList = new List<SVGStopElement>();
            _id = _attrList.GetValue("id");
            _gradientUnits = SVGGradientUnit.ObjectBoundingBox;
            if (_attrList.GetValue("gradiantUnits") == "userSpaceOnUse")
            {
                _gradientUnits = SVGGradientUnit.UserSpaceOnUse;
            }

            _gradientTransform = new SVGTransformList(_attrList.GetValue("gradientTransform"));

            //------
            // TODO: It's probably a bug that the value is not innoculated for CaSe
            // VaRiAtIoN in GetValue, below:
            _spreadMethod = SVGSpreadMethod.Pad;
            if (_attrList.GetValue("spreadMethod") == "reflect")
            {
                _spreadMethod = SVGSpreadMethod.Reflect;
            } else if (_attrList.GetValue("spreadMethod") == "repeat")
            {
                _spreadMethod = SVGSpreadMethod.Repeat;
            }

            if(node is BlockOpenNode)
            {
                GetElementList();
            }
        }

        protected void GetElementList()
        {
            bool exitFlag = false;
            while (!exitFlag && _xmlImp.Next())
            {
                if (_xmlImp.node is BlockCloseNode)
                {
                    exitFlag = true;
                    continue;
                }
                if (_xmlImp.node.name == SVGNodeName.Stop)
                    _stopList.Add(new SVGStopElement(_xmlImp.node.attributes));
            }
        }

        public SVGStopElement GetStopElement(int i)
        {
            if ((i >= 0) && (i < _stopList.Count))
                return _stopList [i];
            return null;
        }
    }
}
