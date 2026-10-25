

Shader "SVG Importer/GradientColor/GradientColorMultiply" {
	
	// texcoord0.x : Gradient X Transformation
	// texcoord0.y : Gradient Y Transformation
	// texcoord1.x : Image Index Integer
	// texcoord1.y : Gradient Type Integer
	
	
	Properties {
	}
	
	SubShader
	{		
		Tags {"RenderType"="Transparent" "Queue"="Transparent"}	
		LOD 200
		Lighting Off
		Blend DstColor Zero
		ZWrite Off
		Cull Off
		Fog { Mode Off }
		
		Pass
		{			
			CGPROGRAM
			#pragma vertex vertexGradients
			#pragma fragment fragmentGradientsAlphaBlended
			#pragma target 3.5
			#include "UnityCG.cginc"
			#include "../SVGImporterGradientCG.cginc"			
			ENDCG
        }
	}
}
