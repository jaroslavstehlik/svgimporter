// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
