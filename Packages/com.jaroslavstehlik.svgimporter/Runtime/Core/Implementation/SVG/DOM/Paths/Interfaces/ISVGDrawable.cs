

namespace SVGImporter.Rendering
{
    using Document;

    public interface ISVGDrawable
    {
        void BeforeRender(SVGTransformList transformList);
        void Render();
    }
}