

namespace SVGImporter.Rendering
{
    using Geometry;

    public class SVGParentable : SVGTransformable
    {
        public SVGParentable parent;
        //public SVGMesh mesh;

        public SVGParentable(SVGTransformList inheritTransformList) : base(inheritTransformList)
        {

        }
    }
}