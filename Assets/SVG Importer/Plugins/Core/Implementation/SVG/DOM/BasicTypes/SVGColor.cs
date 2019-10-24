// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;
using System.Collections;

namespace SVGImporter.Utils
{
    public enum SVGColorType : ushort
    {
        Unknown = 0,
        RGB = 1,
        Current = 2,
        None = 3
    }

    public struct SVGColor
    {
        public SVGColorType colorType;
        public Color color;

        public SVGColor(string colorString)
        {
            if(SVGColorExtractor.IsRGBColor(colorString))
            {
                colorType = SVGColorType.RGB;
                color = SVGColorExtractor.RGBColor(colorString);
            } else if (SVGColorExtractor.IsHexColor(colorString))
            {
                colorType = SVGColorType.RGB;
                color = SVGColorExtractor.HexColor(colorString);
            } else if (SVGColorExtractor.IsConstName(colorString))
            {
                colorType = SVGColorType.RGB;
                color = SVGColorExtractor.ConstColor(colorString);
            } else if (colorString.ToLower() == "current")
            {
                colorType = SVGColorType.Current;
                color = Color.black;
            } else if (colorString.ToLower() == "none")
            {
                colorType = SVGColorType.None;
                color = Color.black;
            } else
            {
                colorType = SVGColorType.Unknown;
                color = Color.black;
            }
        }
    }
}
