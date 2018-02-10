// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

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

