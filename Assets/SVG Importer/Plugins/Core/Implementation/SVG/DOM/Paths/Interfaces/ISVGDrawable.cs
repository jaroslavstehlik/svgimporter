// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

namespace SVGImporter.Rendering
{
    using Document;

    public interface ISVGDrawable
    {
        void BeforeRender(SVGTransformList transformList);
        void Render();
    }
}