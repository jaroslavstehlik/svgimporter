// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

Shader "SVG Importer/UI/UIAntialiased" {
	
	Properties {
		_GradientColor ("Gradient Color (RGBA)", 2D) = "white" { }
		_GradientShape ("Gradient Shape (RGBA)", 2D) = "white" { }
		_Params ("Params", Vector) = (1.0, 1.0, 1.0, 1.0)
		
		_Color ("Tint", Color) = (1,1,1,1)
		
 		_StencilComp ("Stencil Comparison", Float) = 8
 		_Stencil ("Stencil ID", Float) = 0
 		_StencilOp ("Stencil Operation", Float) = 0
 		_StencilWriteMask ("Stencil Write Mask", Float) = 255
 		_StencilReadMask ("Stencil Read Mask", Float) = 255
 		
 		_ColorMask ("Color Mask", Float) = 15
	}
	
	SubShader
	{
		Tags {"RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector" = "True"}
		LOD 200
		Lighting Off
		ZTest [unity_GUIZTestMode]
		ZWrite Off
		Cull Off
		
		Stencil {
			Ref [_Stencil]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]						
			Comp [_StencilComp]
			Pass [_StencilOp]
		}
		
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]		
		Fog { Mode Off }
		
		Pass
		{
			CGPROGRAM
			#pragma vertex vertexGradientsAntialiased
			#pragma fragment fragmentGradientsAlphaBlended
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma glsl_no_auto_normalization 
			#include "UnityCG.cginc"			
			#include "UnityUI.cginc"
			#include "../../SVGImporterUICG.cginc"			
			ENDCG
        }
	}
}
