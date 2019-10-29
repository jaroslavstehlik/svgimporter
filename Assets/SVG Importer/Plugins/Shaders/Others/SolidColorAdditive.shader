

Shader "SVG Importer/SolidColor/SolidColorAdditive" {
	
	Properties {
	}
	
	SubShader
	{
		Tags {"RenderType"="Transparent" "Queue"="Transparent"}
		LOD 200	
		Lighting Off	
		Blend One One
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
			#include "../SVGImporterSolidCG.cginc"					
			ENDCG
        }
	}
}
