

using UnityEngine;
using System.Collections;

namespace SVGImporter.Rendering
{
    internal class SVGShader 
    {    	
        // The shaders' names. Imports store these rather than looking the shaders up, which
        // fails while Unity hasn't imported the shaders yet (on a project's first import).
        public const string GradientColorAlphaBlendedName = "SVG Importer/GradientColor/GradientColorAlphaBlended";
        public const string GradientColorAlphaBlendedAntialiasedName = "SVG Importer/GradientColor/GradientColorAlphaBlendedAntialiased";
        public const string GradientColorOpaqueName = "SVG Importer/GradientColor/GradientColorOpaque";
        public const string SolidColorAlphaBlendedName = "SVG Importer/SolidColor/SolidColorAlphaBlended";
        public const string SolidColorAlphaBlendedAntialiasedName = "SVG Importer/SolidColor/SolidColorAlphaBlendedAntialiased";
        public const string SolidColorOpaqueName = "SVG Importer/SolidColor/SolidColorOpaque";

        protected static Shader _GradientColorAlphaBlended;
    	public static Shader GradientColorAlphaBlended {
    		get {
                if(_GradientColorAlphaBlended == null) _GradientColorAlphaBlended = Shader.Find (GradientColorAlphaBlendedName);
                return _GradientColorAlphaBlended;
    		}
    	}
        protected static Shader _GradientColorAlphaBlendedAntialiased;
        public static Shader GradientColorAlphaBlendedAntialiased {
            get {
                if(_GradientColorAlphaBlendedAntialiased == null) _GradientColorAlphaBlendedAntialiased = Shader.Find (GradientColorAlphaBlendedAntialiasedName);
                return _GradientColorAlphaBlendedAntialiased;
            }
        }
        protected static Shader _GradientColorOpaque;
    	public static Shader GradientColorOpaque {
    		get {
                if(_GradientColorOpaque == null) _GradientColorOpaque = Shader.Find (GradientColorOpaqueName);
                return _GradientColorOpaque;
    		}
        }
        protected static Shader _SolidColorAlphaBlended;
    	public static Shader SolidColorAlphaBlended {
    		get {
                if(_SolidColorAlphaBlended == null) _SolidColorAlphaBlended = Shader.Find (SolidColorAlphaBlendedName);
                return _SolidColorAlphaBlended;
    		}
    	}
        protected static Shader _SolidColorAlphaBlendedAntialiased;
        public static Shader SolidColorAlphaBlendedAntialiased {
            get {
                if(_SolidColorAlphaBlendedAntialiased == null) _SolidColorAlphaBlendedAntialiased = Shader.Find (SolidColorAlphaBlendedAntialiasedName);
                return _SolidColorAlphaBlendedAntialiased;
            }
        }
        protected static Shader _SolidColorOpaque;
    	public static Shader SolidColorOpaque {
    		get {
                if(_SolidColorOpaque == null) _SolidColorOpaque = Shader.Find (SolidColorOpaqueName);
                return _SolidColorOpaque;
    		}
    	}
        protected static Shader _UI;
        public static Shader UI {
            get {
                if(_UI == null) _UI = Shader.Find("SVG Importer/UI/UI");
                return _UI;
            }
        }
        protected static Shader _UIAntialiased;
        public static Shader UIAntialiased {
            get {
                if(_UIAntialiased == null) _UIAntialiased = Shader.Find("SVG Importer/UI/UIAntialiased");
                return _UIAntialiased;
            }
        }       
    }
}
