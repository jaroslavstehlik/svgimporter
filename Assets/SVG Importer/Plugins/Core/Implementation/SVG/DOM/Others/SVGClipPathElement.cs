// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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