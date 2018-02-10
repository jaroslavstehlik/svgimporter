// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

Shader "SVG Importer/SolidColor/SolidColorDiffuseAlphaBlended"
{	
	Properties
    {
		_MaxLights ("Max Lights", Int) = 4
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
			o.Albedo = IN.color.rgb;
			o.Alpha = IN.color.a;
		}
        ENDCG
    }

	Fallback "SVG Importer/SolidColor/SolidColorAlphaBlended"	
}
