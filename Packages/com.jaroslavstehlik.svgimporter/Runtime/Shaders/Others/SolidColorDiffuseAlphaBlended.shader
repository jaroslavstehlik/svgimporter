

Shader "SVG Importer/SolidColor/SolidColorDiffuseAlphaBlended"
{	
	Properties
    {
		_MaxLights ("Max Lights", Int) = 4
	}

	// Universal Render Pipeline. Ignored by the Built-in Render Pipeline,
	// which uses the surface shader below.
	SubShader
	{
		Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"}
		LOD 200

		Pass
		{
			Tags {"LightMode"="UniversalForward"}
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			Cull Back

			CGPROGRAM
			#pragma vertex vertexDiffuse
			#pragma fragment fragmentDiffuse
			#include "../SVGImporterDiffuseURP.cginc"
			ENDCG
		}

		// The URP 2D Renderer has no directional light, so draw the vertex colors unlit.
		Pass
		{
			Tags {"LightMode"="Universal2D"}
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			Cull Back

			CGPROGRAM
			#pragma vertex vertexColor
			#pragma fragment fragmentColor
			#include "UnityCG.cginc"
			#include "../SVGImporterSolidCG.cginc"
			ENDCG
		}
	}

	SubShader
	{      
		Tags {"RenderType"="Transparent" "Queue"="Transparent" }
		LOD 200	
		Lighting On	
		Blend SrcAlpha OneMinusSrcAlpha			
		ZWrite Off
		Cull Back	
		Fog { Mode Off }	
			
        CGPROGRAM
        #pragma surface surf Lambert alpha
  
		struct Input {
			fixed4 color : COLOR;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			// SVG colors are sRGB; convert them in Linear color space projects.
			#ifndef UNITY_COLORSPACE_GAMMA
			IN.color.rgb = GammaToLinearSpace(IN.color.rgb);
			#endif
			o.Albedo = IN.color.rgb;
			o.Alpha = IN.color.a;
		}
        ENDCG
    }

	Fallback "SVG Importer/SolidColor/SolidColorAlphaBlended"	
}
