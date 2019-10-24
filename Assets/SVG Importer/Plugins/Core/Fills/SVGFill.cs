// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter.Rendering
{
    using Utils;

    public enum FILL_BLEND : byte
    {
    	OPAQUE,
    	ALPHA_BLENDED,
    	ADDITIVE,
    	MULTIPLY
    }

    public enum FILL_TYPE : byte
    {
    	SOLID,
    	GRADIENT,
    	TEXTURE
    }

    public enum GRADIENT_TYPE : byte
    {
    	LINEAR = 0,
    	RADIAL = 1,
        CONICAL = 2
    }

    [System.Serializable]
    public class SVGFill : System.Object
    {	
    	public FILL_TYPE fillType;
    	public FILL_BLEND blend;
        public GRADIENT_TYPE gradientType;
    	public Color32 color;
        public float opacity;
        public Rect viewport;
        public SVGMatrix transform;

        public string gradientId;
        public string gradientHash {
            get {
                return gradientColors.hash;
            }
        }

        public CCGradient gradientColors;

        public Color32 finalColor
        {
            get {
                return new Color32(color.r, color.g, color.b, (byte)Mathf.RoundToInt((float)color.a * opacity));
            }
        }

        public SVGFill ()
    	{
    	}

        public SVGFill (Color32 color)
        {
            this.color = color;
        }

        public SVGFill (Color32 color, FILL_BLEND blend)
        {
            this.color = color;
            this.blend = blend;
        }

        public SVGFill (Color32 color, FILL_BLEND blend, FILL_TYPE fillType)
        {
            this.color = color;
            this.blend = blend;
            this.fillType = fillType;
        }

        public SVGFill (Color32 color, FILL_BLEND blend, FILL_TYPE fillType, GRADIENT_TYPE gradientType)
        {
            this.color = color;
            this.blend = blend;
            this.fillType = fillType;
            this.gradientType = gradientType;
        }

        public SVGFill Clone()
        {
            SVGFill fill = new SVGFill(this.color, this.blend, this.fillType, this.gradientType);
            fill.gradientId = this.gradientId;
            fill.transform = this.transform;
            fill.opacity = this.opacity;;
            fill.viewport = this.viewport;
            if(gradientColors != null) fill.gradientColors = gradientColors.Clone();
            return fill;
        }
    }
}

