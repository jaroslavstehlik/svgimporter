

Shader "SVG Importer/GradientColor/GradientColorOpaque" {
	
	// texcoord0.x : Gradient X Transformation
	// texcoord0.y : Gradient Y Transformation
	// texcoord1.x : Image Index Integer
	// texcoord1.y : Gradient Type Integer
	
	
	Properties {
	}
	
	SubShader
	{
		Tags {"RenderType"="Opaque" "Queue"="Geometry"}
		LOD 200
		Lighting Off
		Blend Off
		ZWrite On
		Cull Off
		Fog { Mode Off }
				
		Pass
		{			
			CGPROGRAM
			#pragma vertex vertexGradients
			#pragma fragment fragmentGradientsOpaque
			#pragma target 3.5
			#include "UnityCG.cginc"
			#include "../../SVGImporterGradientCG.cginc"			
			ENDCG
        }
	}
}
