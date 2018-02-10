// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using System.Collections;

namespace SVGImporter.Rendering
{
    public class SVGShader 
    {    	
        protected static Shader _GradientColorAlphaBlended;
    	public static Shader GradientColorAlphaBlended {
    		get {
                if(_GradientColorAlphaBlended == null) _GradientColorAlphaBlended = Shader.Find ("SVG Importer/GradientColor/GradientColorAlphaBlended");
                return _GradientColorAlphaBlended;
    		}
    	}
        protected static Shader _GradientColorAlphaBlendedAntialiased;
        public static Shader GradientColorAlphaBlendedAntialiased {
            get {
                if(_GradientColorAlphaBlendedAntialiased == null) _GradientColorAlphaBlendedAntialiased = Shader.Find ("SVG Importer/GradientColor/GradientColorAlphaBlendedAntialiased");
                return _GradientColorAlphaBlendedAntialiased;
            }
        }
        protected static Shader _GradientColorOpaque;
    	public static Shader GradientColorOpaque {
    		get {
                if(_GradientColorOpaque == null) _GradientColorOpaque = Shader.Find ("SVG Importer/GradientColor/GradientColorOpaque");
                return _GradientColorOpaque;
    		}
        }
        protected static Shader _SolidColorAlphaBlended;
    	public static Shader SolidColorAlphaBlended {
    		get {
                if(_SolidColorAlphaBlended == null) _SolidColorAlphaBlended = Shader.Find ("SVG Importer/SolidColor/SolidColorAlphaBlended");
                return _SolidColorAlphaBlended;
    		}
    	}
        protected static Shader _SolidColorAlphaBlendedAntialiased;
        public static Shader SolidColorAlphaBlendedAntialiased {
            get {
                if(_SolidColorAlphaBlendedAntialiased == null) _SolidColorAlphaBlendedAntialiased = Shader.Find ("SVG Importer/SolidColor/SolidColorAlphaBlendedAntialiased");
                return _SolidColorAlphaBlendedAntialiased;
            }
        }
        protected static Shader _SolidColorOpaque;
    	public static Shader SolidColorOpaque {
    		get {
                if(_SolidColorOpaque == null) _SolidColorOpaque = Shader.Find ("SVG Importer/SolidColor/SolidColorOpaque");
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
                if(_UIAntialiased == null) _UI = Shader.Find("SVG Importer/UI/UIAntialiased");
                return _UI;
            }
        }       
    }
}
