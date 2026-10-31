Shader "SVG Importer/GradientColor/GradientColorOpaque" {

	// texcoord0.x : Gradient X Transformation
	// texcoord0.y : Gradient Y Transformation
	// texcoord1.x : Image Index Integer
	// texcoord1.y : Gradient Type Integer

	// Universal Render Pipeline: lit by 2D lights in the 2D Renderer, unlit in the Universal
	// Renderer. Ignored by the Built-in Render Pipeline, which uses the SubShader below.
	SubShader
	{
		Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
		LOD 200
		Blend Off
		ZWrite On
		Cull Off

		Pass
		{
			Tags {"LightMode"="Universal2D"}
			CGPROGRAM
			#pragma vertex vertexGradients
			#pragma fragment fragmentGradientsOpaque
			#pragma target 3.5
			#pragma multi_compile USE_SHAPE_LIGHT_TYPE_0 __
			#pragma multi_compile USE_SHAPE_LIGHT_TYPE_1 __
			#pragma multi_compile USE_SHAPE_LIGHT_TYPE_2 __
			#pragma multi_compile USE_SHAPE_LIGHT_TYPE_3 __
			#pragma multi_compile_local __ SVG_UNLIT_2D
			#include "UnityCG.cginc"
			#include "../../SVGImporterLight2D.cginc"
			#include "../../SVGImporterGradientCG.cginc"
			ENDCG
		}

		Pass
		{
			Tags {"LightMode"="UniversalForwardOnly"}
			CGPROGRAM
			#pragma vertex vertexGradients
			#pragma fragment fragmentGradientsOpaque
			#pragma target 3.5
			#include "UnityCG.cginc"
			#include "../../SVGImporterGradientCG.cginc"
			ENDCG
		}
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
