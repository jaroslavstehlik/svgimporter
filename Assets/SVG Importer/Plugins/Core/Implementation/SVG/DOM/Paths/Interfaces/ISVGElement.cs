// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

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