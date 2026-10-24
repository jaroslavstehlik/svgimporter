

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
