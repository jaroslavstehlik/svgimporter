

using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter
{
    using Utils;

    /// <summary>Whether a fill is drawn opaque or with transparency.</summary>
    public enum SVGFillBlend : byte
    {
        Opaque = 0,
        Transparent = 1,
    }

    /// <summary>What a fill paints with.</summary>
    public enum SVGFillType : byte
    {
        Solid = 0,
        Gradient = 1,
        Texture = 2,
    }

    /// <summary>The shape of a gradient fill.</summary>
    public enum SVGGradientType : byte
    {
        Linear = 0,
        Radial = 1,
        Conical = 2,
    }

    [System.Serializable]
    public class SVGFill : System.Object
    {	
    	public SVGFillType fillType;
    	public SVGFillBlend blend;
        public SVGGradientType gradientType;
    	public Color32 color;
        public float opacity;
        public Rect viewport;
        public SVGMatrix transform;

        public string gradientId;

        public SVGGradient gradientColors;

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

        public SVGFill (Color32 color, SVGFillBlend blend)
        {
            this.color = color;
            this.blend = blend;
        }

        public SVGFill (Color32 color, SVGFillBlend blend, SVGFillType fillType)
        {
            this.color = color;
            this.blend = blend;
            this.fillType = fillType;
        }

        public SVGFill (Color32 color, SVGFillBlend blend, SVGFillType fillType, SVGGradientType gradientType)
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

        // Takes another fill's values, keeping this fill's gradient: what renderers reset after
        // modifiers and tints changed a copy of the asset's fills.
        internal void CopyFrom(SVGFill other)
        {
            fillType = other.fillType;
            blend = other.blend;
            gradientType = other.gradientType;
            color = other.color;
            opacity = other.opacity;
            viewport = other.viewport;
            transform = other.transform;
            gradientId = other.gradientId;
        }
    }
}

