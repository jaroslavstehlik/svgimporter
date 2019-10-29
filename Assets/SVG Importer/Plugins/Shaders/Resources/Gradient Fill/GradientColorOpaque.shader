

Shader "SVG Importer/GradientColor/GradientColorOpaque" {
	
	// texcoord0.x : Gradient X Transformation
	// texcoord0.y : Gradient Y Transformation
	// texcoord1.x : Image Index Integer
	// texcoord1.y : Gradient Type Integer
	
	// _Params.x : Atlas Width
	// _Params.y : Atlas Height
	// _Params.z : Gradient width
	// _Params.w : Gradient height
	
	Properties {
		_GradientColor ("Gradient Color (RGBA)", 2D) = "white" { }
		_GradientShape ("Gradient Shape (RGBA)", 2D) = "white" { }
		_Params ("Params", Vector) = (1.0, 1.0, 1.0, 1.0)
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
			#include "UnityCG.cginc"
			#include "../../SVGImporterGradientCG.cginc"			
			ENDCG
        }
	}
}
