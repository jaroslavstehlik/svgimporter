// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

namespace SVGImporter.Document 
{
    using Rendering;

    public class SVGDocument
    {
        private SVGElement _rootElement = null;

        public SVGElement rootElement
        {
            get
            {
                return _rootElement;
            }
        }

        private SVGParser parser;

        public SVGDocument(string originalDocument, SVGGraphics r)
        {
            parser = new SVGParser(originalDocument);

            while (!parser.isEOF && parser.node.name != SVGNodeName.SVG)
                parser.Next();

            _rootElement = new SVGElement(parser, new SVGTransformList(), null, true);
        }

        public void Clear()
        {
            _rootElement = null;
            parser = null;
        }
    }
}
