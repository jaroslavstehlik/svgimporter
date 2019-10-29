

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