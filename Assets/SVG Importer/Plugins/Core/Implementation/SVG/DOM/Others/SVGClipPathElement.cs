// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using System.Collections.Generic;

namespace SVGImporter.Rendering 
{
    using Document;
    
    public class SVGClipPathElement
    {
        private string _id;
        private SVGParser _xmlImp;
        protected AttributeList _attrList;

        public string id
        {
            get { return _id; }
        }

        public SVGClipPathElement(SVGParser xmlImp, Node node)
        {
            _attrList = node.attributes;
            _xmlImp = xmlImp;
            _id = _attrList.GetValue("id");
            GetElementList();
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
            }
        }
    }
}