// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

Shader "SVG Importer/SolidColor/SolidColorTexOverlayAlphaBlended" {
	
	Properties {
		_MainTex ("Texture", 2D) = "white" { }
	}
	
	SubShader
	{
		Tags {"RenderType"="Transparent" "Queue"="Transparent"}
		LOD 200	
		Lighting Off	
		Blend SrcAlpha OneMinusSrcAlpha			
		ZWrite Off
		Cull Off	
		Fog { Mode Off }	
		
		Pass
		{
			CGPROGRAM
			#pragma vertex vertexColor
			#pragma fragment fragmentColor
			#pragma fragmentoption ARB_precision_hint_fastest
			#include "UnityCG.cginc"
			
			sampler2D _MainTex;
			
			struct vertex_input
			{
			    float4 vertex : POSITION;			    
			    half4 color : COLOR;		
			    half2 uv: TEXCOORD0;	    
			};
			
			struct vertex_output
			{
			    float4 vertex : SV_POSITION;			    
			    half4 color : COLOR;	
			    half2 uv: TEXCOORD0;	    		    
			};	
			
			vertex_output vertexColor(vertex_input v)
			{
			    vertex_output o;
			    o.vertex = UnityObjectToClipPos(v.vertex);    
			    o.color = v.color;
			    o.uv = v.uv;
			    return o;
			}
			
			half4 fragmentColor(vertex_output i) : COLOR
			{
				return i.color * tex2D(_MainTex, i.uv);
			}
			
			ENDCG
        }
	}
}
