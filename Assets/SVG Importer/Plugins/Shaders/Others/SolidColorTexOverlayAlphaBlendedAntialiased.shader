// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

Shader "SVG Importer/SolidColor/SolidColorTexOverlayAlphaBlendedAntialiased" {
	
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
			#include "../SVGImporterCG.cginc"
			
			sampler2D _MainTex;
			
			struct vertex_input
			{
			    float4 vertex : POSITION;			    
			    half4 color : COLOR;		
			    half2 uv: TEXCOORD0;	    
			    float3 normal : NORMAL;
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
	
				o.vertex = UnityObjectToClipPos(Antialiasing(v.vertex, v.normal));			    
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
