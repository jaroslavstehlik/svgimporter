

using UnityEngine;
using System.Collections.Generic;

namespace SVGImporter.Rendering
{
    using Document;
    
    public interface ISVGElement
    {
        AttributeList attrList {get;}
        SVGPaintable paintable {get;}
        SVGMatrix transformMatrix {get;}        

        List<List<Vector2>> GetPath();
        List<List<Vector2>> GetClipPath();
    }
}