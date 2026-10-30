

using UnityEngine;

using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;

namespace SVGImporter
{
    /// <summary>How a gradient continues past its ends (SVG's spreadMethod). Unknown reads as Pad.</summary>
    public enum SVGSpreadMethod : ushort
    {
        Unknown = 0,
        Pad = 1,
        Reflect = 2,
        Repeat = 3
    }

    [System.Serializable]
    public struct SVGGradientColorKey {
        public float time;
        public Color32 color;
        
        public SVGGradientColorKey (Color32 color, float time)
        {
            this.time = time;
            this.color = color;
        }

        public override string ToString()
        {
            return string.Format("[SVGGradientColorKey: time={0}, color={1}", time, color);
        }
    }

    [System.Serializable]
    public struct SVGGradientAlphaKey {
        public float time;
        public float alpha;

    	public SVGGradientAlphaKey (float alpha, float time)
    	{
    		this.time = time;
            this.alpha = alpha;
    	}
    	
        public override string ToString()
        {
            return string.Format("[SVGGradientAlphaKey: time={0}, alpha={1}]", time, alpha);
        }
    }

    /// <summary>A gradient stop: a colour (sRGB, straight alpha) at an offset along the gradient.</summary>
    [System.Serializable]
    public struct SVGGradientStop {
        public float offset;
        public Color color;

        public SVGGradientStop (float offset, Color color)
        {
            this.offset = offset;
            this.color = color;
        }
    }

    /// <summary>
    /// What a gradient draws, compared exactly: its stops (GetStops), spread method and focal
    /// point. Gradients with equal keys share an atlas slot; different gradients never do,
    /// whatever their hash codes.
    /// </summary>
    internal readonly struct GradientKey : IEquatable<GradientKey>
    {
        readonly int[] bits;
        readonly int hashCode;

        public GradientKey (SVGGradient gradient)
        {
            List<SVGGradientStop> stops = gradient.GetStops();
            bits = new int[stops.Count * 5 + 4];
            for (int i = 0; i < stops.Count; i++)
            {
                bits[i * 5] = BitConverter.SingleToInt32Bits(stops[i].offset);
                bits[i * 5 + 1] = BitConverter.SingleToInt32Bits(stops[i].color.r);
                bits[i * 5 + 2] = BitConverter.SingleToInt32Bits(stops[i].color.g);
                bits[i * 5 + 3] = BitConverter.SingleToInt32Bits(stops[i].color.b);
                bits[i * 5 + 4] = BitConverter.SingleToInt32Bits(stops[i].color.a);
            }
            int end = stops.Count * 5;
            bits[end] = (int)gradient.spreadMode;
            bits[end + 1] = BitConverter.SingleToInt32Bits(gradient.focalOffset.x);
            bits[end + 2] = BitConverter.SingleToInt32Bits(gradient.focalOffset.y);
            bits[end + 3] = BitConverter.SingleToInt32Bits(gradient.focalRadius);
            // FNV-1a over the values.
            uint hash = 2166136261;
            foreach (int value in bits)
                hash = (hash ^ (uint)value) * 16777619;
            hashCode = (int)hash;
        }

        public bool Equals (GradientKey other)
        {
            if (hashCode != other.hashCode || bits.Length != other.bits.Length) return false;
            for (int i = 0; i < bits.Length; i++)
                if (bits[i] != other.bits[i]) return false;
            return true;
        }

        public override bool Equals (object obj) { return obj is GradientKey && Equals((GradientKey)obj); }
        public override int GetHashCode () { return hashCode; }
    }

    [System.Serializable]
    public class SVGGradient : System.Object {
    	public SVGGradientColorKey[] colorKeys;
    	public SVGGradientAlphaKey[] alphaKeys;

        /// <summary>How the gradient continues before its first and after its last stop.</summary>
        public SVGSpreadMethod spreadMethod;
        /// <summary>
        /// Radial gradients: the focal point relative to the centre, and the focal radius, in the
        /// gradient's normalised space, where the centre is at (0.5, 0.5) and the radius is 0.5.
        /// Zero for a gradient that starts at its centre.
        /// </summary>
        public Vector2 focalOffset;
        public float focalRadius;

        /// <summary>The spread method, with Unknown read as Pad, the SVG default.</summary>
        internal SVGSpreadMethod spreadMode
        {
            get { return spreadMethod == SVGSpreadMethod.Unknown ? SVGSpreadMethod.Pad : spreadMethod; }
        }

    	// The gradient's slot: in its asset, or in the atlas once registered.
        [SerializeField] internal int index;

        [HideInInspector]
        [System.NonSerialized]
    	internal int atlasIndex;

        public SVGGradient (SVGGradientColorKey[] colorKeys, SVGGradientAlphaKey[] alphaKeys, bool sort = true)
    	{
    		SetKeys(colorKeys, alphaKeys, sort);
    	}
    	
    	public void SetKeys (SVGGradientColorKey[] colorKeys, SVGGradientAlphaKey[] alphaKeys, bool sort = true)
    	{
            this.colorKeys = (SVGGradientColorKey[])colorKeys.Clone();
            this.alphaKeys = (SVGGradientAlphaKey[])alphaKeys.Clone();

            if(sort)
            {
                // Descending time; keys at the same time (hard stops) end up in reverse order, so
                // that reading the array backwards gives their original order. Array.Sort isn't
                // stable and could swap them.
                this.colorKeys = this.colorKeys.Select((key, i) => new { key, i }).OrderByDescending(k => k.key.time).ThenByDescending(k => k.i).Select(k => k.key).ToArray();
                this.alphaKeys = this.alphaKeys.Select((key, i) => new { key, i }).OrderByDescending(k => k.key.time).ThenByDescending(k => k.i).Select(k => k.key).ToArray();
            }

            if (this.alphaKeys == null || this.alphaKeys.Length == 0) {
                this.alphaKeys = new SVGGradientAlphaKey[]{
                    new SVGGradientAlphaKey (1f, 0f),
                    new SVGGradientAlphaKey (1f, 1f)
                };			
    		}	    		
    	}
    	
        /// <summary>
        /// The gradient as stops in offset order: the colour keys and alpha keys merged, each
        /// stop with both a colour and an alpha. Keys at the same offset (hard stops) keep their
        /// order. This is what the gradient draws, and what the atlas stores.
        /// </summary>
        public List<SVGGradientStop> GetStops ()
        {
            var stops = new List<SVGGradientStop>();
            if (colorKeys == null || alphaKeys == null) return stops;
            // The keys are kept in descending time order (see SetKeys).
            SVGGradientColorKey[] colors = colorKeys.Reverse().ToArray();
            SVGGradientAlphaKey[] alphas = alphaKeys.Reverse().ToArray();
            int c = 0, a = 0;
            while (c < colors.Length || a < alphas.Length)
            {
                float colorTime = c < colors.Length ? colors[c].time : float.MaxValue;
                float alphaTime = a < alphas.Length ? alphas[a].time : float.MaxValue;
                float time = Mathf.Min(colorTime, alphaTime);
                Color color = colorTime == time ? (Color)colors[c].color : ColorAt(colors, c, time);
                color.a = alphaTime == time ? alphas[a].alpha : AlphaAt(alphas, a, time);
                stops.Add(new SVGGradientStop(Mathf.Clamp01(time), color));
                if (colorTime == time) c++;
                if (alphaTime == time) a++;
            }
            return stops;
        }

        // The colour between keys[next - 1] and keys[next] at time.
        static Color ColorAt (SVGGradientColorKey[] keys, int next, float time)
        {
            if (keys.Length == 0) return Color.black;
            if (next <= 0) return keys[0].color;
            if (next >= keys.Length) return keys[keys.Length - 1].color;
            SVGGradientColorKey from = keys[next - 1], to = keys[next];
            return Color.Lerp(from.color, to.color, to.time > from.time ? (time - from.time) / (to.time - from.time) : 0f);
        }

        static float AlphaAt (SVGGradientAlphaKey[] keys, int next, float time)
        {
            if (keys.Length == 0) return 1f;
            if (next <= 0) return keys[0].alpha;
            if (next >= keys.Length) return keys[keys.Length - 1].alpha;
            SVGGradientAlphaKey from = keys[next - 1], to = keys[next];
            return Mathf.Lerp(from.alpha, to.alpha, to.time > from.time ? (time - from.time) / (to.time - from.time) : 0f);
        }

        /// <summary>
        /// The colour at a position along the gradient, as the gradient shaders compute it: at a
        /// hard stop, the stop after it.
        /// </summary>
    	public Color32 Evaluate (float time)
    	{
            return Evaluate(GetStops(), time);
        }

        internal static Color Evaluate (List<SVGGradientStop> stops, float time)
        {
            if (stops.Count == 0) return Color.black;
            int last = stops.FindLastIndex(stop => stop.offset <= time);
            if (last < 0) return stops[0].color;
            if (last == stops.Count - 1) return stops[last].color;
            SVGGradientStop from = stops[last], to = stops[last + 1];
            return Color.Lerp(from.color, to.color, to.offset > from.offset ? (time - from.offset) / (to.offset - from.offset) : 0f);
    	}

    	internal Color32 ApproximateColor (int samples)
    	{
    		int r = 0, g = 0, b = 0, a = 0;
			float totalTime = samples - 1;//, progress = 0f;
    		Color32 color;
    		for (float i = 0; i < samples; i++) {
    			color = Evaluate (i / totalTime);
    			r += color.r;
    			g += color.g;
    			b += color.b;
    			a += color.a;
    		}
    		
    		r = Mathf.Clamp(Mathf.RoundToInt ((float)r / (float)samples), 0, 255);
    		g = Mathf.Clamp(Mathf.RoundToInt ((float)g / (float)samples), 0, 255);
    		b = Mathf.Clamp(Mathf.RoundToInt ((float)b / (float)samples), 0, 255);
    		a = Mathf.Clamp (Mathf.RoundToInt ((float)a / (float)samples), 0, 255);
    		
    		return new Color32 ((byte)r, (byte)g, (byte)b, (byte)a);
    	}
    	
    	internal bool initialised
    	{
    		get {
    			return (colorKeys != null && alphaKeys != null && colorKeys.Length != 0 && alphaKeys.Length != 0);
    		}
    	}
    	
        public SVGGradient Clone()
        {
            if(this.colorKeys == null || this.alphaKeys == null)
                return null;

            SVGGradient gradient = new SVGGradient((SVGGradientColorKey[])this.colorKeys.Clone(), 
                                                 (SVGGradientAlphaKey[])this.alphaKeys.Clone(), false);
            gradient.index = this.index;
            gradient.atlasIndex = this.atlasIndex;
            gradient.spreadMethod = this.spreadMethod;
            gradient.focalOffset = this.focalOffset;
            gradient.focalRadius = this.focalRadius;
            return gradient;
        }

        public override string ToString()
        {
            string output = string.Format("[SVGGradient: initialised={0}, index={1}, atlasIndex={2}]", initialised, index, atlasIndex);

            if(colorKeys != null && colorKeys.Length > 0)
            {
                output += "\nColorKeys:\n";
                for(int i = 0; i < colorKeys.Length; i++)
                {
                    output += colorKeys[i].ToString()+"\n";
                }
            }

            if(alphaKeys != null && alphaKeys.Length > 0)
            {
                output += "\nAlphaKeys:\n";
                for(int i = 0; i < alphaKeys.Length; i++)
                {
                    output += alphaKeys[i].ToString()+"\n";
                }
            }

            return output;
        }

        /// <summary>Whether both gradients draw the same: equal stops (see GetStops), spread and focal point, compared exactly.</summary>
        public bool GradientEquals(SVGGradient gradient)
        {
            if (gradient == null || !gradient.initialised || !initialised) return false;
            return new GradientKey(this).Equals(new GradientKey(gradient));
        }

        // Note that Color32 and Color implictly convert to each other. You may pass a Color object to this method without first casting it.
        internal static string ColorToHex (Color32 color)
        {
            string hex = color.r.ToString ("X2") + color.g.ToString ("X2") + color.b.ToString ("X2");
            return hex;
        }
        
        internal static Color HexToColor (string hex)
        {
            byte r, g, b;
            byte.TryParse (hex.Substring (0, 2), System.Globalization.NumberStyles.HexNumber, null as IFormatProvider, out r);
            byte.TryParse (hex.Substring (2, 2), System.Globalization.NumberStyles.HexNumber, null as IFormatProvider, out g);
            byte.TryParse (hex.Substring (4, 2), System.Globalization.NumberStyles.HexNumber, null as IFormatProvider, out b);
            return new Color32 (r, g, b, 255);
        }
    }
}
