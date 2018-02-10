// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

float SVG_ANTIALIASING_WIDTH;

float4 Antialiasing(float4 vertex, float3 normal)
{
	float4 output = vertex;
	if (unity_OrthoParams.w == 0)
	{
		output.xy += normal.xy * length(ObjSpaceViewDir(vertex)) * SVG_ANTIALIASING_WIDTH * (min(_ScreenParams.z, _ScreenParams.w) - 1);
	}
	else
	{
		float3 origin = mul(unity_ObjectToWorld, float4(0, 0, 0, 1));
		float3 offset = mul(unity_ObjectToWorld, float4(1, 0, 0, 1));
		float size = 1/length(origin - offset);
		output.xy += normal.xy * size * unity_OrthoParams.x * SVG_ANTIALIASING_WIDTH * (min(_ScreenParams.z, _ScreenParams.w) - 1);
	}
	return output;
}