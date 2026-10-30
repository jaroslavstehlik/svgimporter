// Copyright (C) 2015 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

namespace SVGImporter
{
    // SVG Renderer and SVG Image: components that draw an SVG Asset.
    internal interface ISVGRenderer
    {
        SVGAsset vectorGraphics { get; }
    }
}
