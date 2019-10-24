// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

Shader "SVG Importer/SolidColor/SolidColorDiffuseOpaque"
{	
	SubShader
	{
		Tags {"RenderType"="Opaque" "Queue"="Geometry"}
		LOD 200
		Lighting On
		Blend Off
		ZWrite On
		Cull Back
		Fog { Mode Off }	
				
        CGPROGRAM
        #pragma surface surf Lambert
  
		struct Input {
			fixed4 color : COLOR;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			o.Albedo = IN.color.rgb;
		}
        ENDCG
	}
	Fallback "SVG Importer/SolidColor/SolidColorOpaque"
}
