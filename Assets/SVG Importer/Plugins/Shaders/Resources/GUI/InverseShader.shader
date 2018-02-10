// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "SVG Importer/Utils/InverseShader"
{
	Properties
	{
		
	}
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"  }
		LOD 100

		ZWrite Off
		Cull Off
		Blend OneMinusDstColor OneMinusSrcAlpha
		

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag	
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);				
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{				
				return fixed4(1.0, 1.0, 1.0, 0.5);
			}
			ENDCG
		}
	}
}
