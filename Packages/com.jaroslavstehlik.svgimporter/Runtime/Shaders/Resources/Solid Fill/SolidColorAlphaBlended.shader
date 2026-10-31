Shader "SVG Importer/SolidColor/SolidColorAlphaBlended" {

	// Sprite Mask interaction: SVGAtlas sets these on copies of the material (see
	// SVGRenderer.maskInteraction). Sprite Masks count up the stencil buffer where they are.
	Properties {
		[HideInInspector] _SVGStencilRef ("Stencil Ref", Float) = 1
		[HideInInspector] _SVGStencilComp ("Stencil Comparison", Float) = 8
	}

	// Universal Render Pipeline: lit by 2D lights in the 2D Renderer, unlit in the Universal
	// Renderer. Ignored by the Built-in Render Pipeline, which uses the SubShader below.
	SubShader
	{
		Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"}
		LOD 200
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		Cull Off
		Stencil {
			Ref [_SVGStencilRef]
			Comp [_SVGStencilComp]
		}

		Pass
		{
			Tags {"LightMode"="Universal2D"}
			CGPROGRAM
			#pragma vertex vertexColor
			#pragma fragment fragmentColor
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile USE_SHAPE_LIGHT_TYPE_0 __
			#pragma multi_compile USE_SHAPE_LIGHT_TYPE_1 __
			#pragma multi_compile USE_SHAPE_LIGHT_TYPE_2 __
			#pragma multi_compile USE_SHAPE_LIGHT_TYPE_3 __
			#pragma multi_compile_local __ SVG_UNLIT_2D
			#include "UnityCG.cginc"
			#include "../../SVGImporterLight2D.cginc"
			#include "../../SVGImporterSolidCG.cginc"
			ENDCG
		}

		Pass
		{
			Tags {"LightMode"="UniversalForwardOnly"}
			CGPROGRAM
			#pragma vertex vertexColor
			#pragma fragment fragmentColor
			#pragma fragmentoption ARB_precision_hint_fastest
			#include "UnityCG.cginc"
			#include "../../SVGImporterSolidCG.cginc"
			ENDCG
		}
	}

	SubShader
	{
		Tags {"RenderType"="Transparent" "Queue"="Transparent"}
		LOD 200
		Lighting Off
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		Cull Off
		Stencil {
			Ref [_SVGStencilRef]
			Comp [_SVGStencilComp]
		}
		Fog { Mode Off }

		Pass
		{
			CGPROGRAM
			#pragma vertex vertexColor
			#pragma fragment fragmentColor
			#pragma fragmentoption ARB_precision_hint_fastest
			#include "UnityCG.cginc"
			#include "../../SVGImporterSolidCG.cginc"
			ENDCG
		}
	}
}
