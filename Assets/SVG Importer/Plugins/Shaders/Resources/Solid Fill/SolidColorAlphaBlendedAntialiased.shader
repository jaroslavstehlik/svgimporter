

Shader "SVG Importer/SolidColor/SolidColorAlphaBlendedAntialiased" {
	
	Properties {
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
			#pragma vertex vertexColorAntialiased
			#pragma fragment fragmentColor
			#pragma fragmentoption ARB_precision_hint_fastest
			#include "UnityCG.cginc"
			#include "../../SVGImporterSolidCG.cginc"					
			ENDCG
        }
	}
}