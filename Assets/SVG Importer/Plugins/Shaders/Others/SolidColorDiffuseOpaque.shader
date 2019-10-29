

Shader "SVG Importer/SolidColor/SolidColorDiffuseOpaque"
{	
	SubShader
	{
		Tags {"RenderType"="Opaque" "Queue"="Geometry"}
		LOD 200
		Lighting On
		Blend Off
		ZWrite On
		Cull Back
		Fog { Mode Off }	
				
        CGPROGRAM
        #pragma surface surf Lambert
  
		struct Input {
			fixed4 color : COLOR;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			o.Albedo = IN.color.rgb;
		}
        ENDCG
	}
	Fallback "SVG Importer/SolidColor/SolidColorOpaque"
}
