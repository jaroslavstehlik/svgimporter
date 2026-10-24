

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
