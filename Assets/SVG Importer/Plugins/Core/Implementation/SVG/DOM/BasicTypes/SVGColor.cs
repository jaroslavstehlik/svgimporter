// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

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
