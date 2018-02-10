// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms
#include "SVGImporterCG.cginc"

sampler2D _GradientColor;
sampler2D _GradientShape;
float4 _Params;

float4 _ClipRect;
fixed4 _TextureSampleAdd;
			
struct vertex_input
{
    float4 vertex : POSITION;	
    float4 texcoord0 : TEXCOORD0;
	float4 texcoord1 : TEXCOORD1;
    float3 normal : NORMAL;
    half4 color : COLOR;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct vertex_output
{
    float4 vertex : POSITION;			    
    float4 uv0 : TEXCOORD0;
    float4 uv1 : TEXCOORD1;
    float4 worldPosition : TEXCOORD2;
    half4 color : COLOR;
	UNITY_VERTEX_OUTPUT_STEREO
};

vertex_output vertexGradients(vertex_input v)
{
    vertex_output o;
	UNITY_SETUP_INSTANCE_ID(v);
    o.worldPosition = v.vertex;
	o.vertex = UnityObjectToClipPos(v.vertex);
	o.uv0.xy = v.texcoord0.xy;
	o.color = v.color;

	// half pixel
	float2 texelOffset = float2(0.5 / _Params.x, 0.5 / _Params.y);
	float imageIndex = v.texcoord1.x * _Params.z;

	// Horizontal Start
	o.uv0.z = saturate((fmod(imageIndex, _Params.x) / _Params.x) + texelOffset.x);
	// Horizontal Width
	o.uv1 = saturate((1.0 - abs(float4(0.0, 1.0, 2.0, 3.0) - v.texcoord1.y)) * (_Params.z / _Params.x - texelOffset.x * 2.0));
	// Vertical Start
	o.uv0.w = saturate((floor((imageIndex / _Params.x) * _Params.w) / _Params.y) + texelOffset.y);

    return o;
}

vertex_output vertexGradientsAntialiased(vertex_input v)
{
    vertex_output o;
	UNITY_SETUP_INSTANCE_ID(v);
	o.worldPosition = v.vertex;	
    
	// Antialiasing		
	o.vertex = UnityObjectToClipPos(Antialiasing(v.vertex, v.normal));

    o.uv0.xy = v.texcoord0.xy;
    o.color = v.color;
	
	// half pixel
	float2 texelOffset = float2(0.5 / _Params.x, 0.5 / _Params.y);
	float imageIndex = v.texcoord1.x * _Params.z;
	
	// Horizontal Start
    o.uv0.z = saturate((fmod(imageIndex, _Params.x) / _Params.x) + texelOffset.x);
    // Horizontal Width
    o.uv1 = saturate((1.0 - abs(float4(0.0, 1.0, 2.0, 3.0) - v.texcoord1.y)) * (_Params.z / _Params.x - texelOffset.x * 2.0));
    // Vertical Start
    o.uv0.w = saturate((floor((imageIndex / _Params.x) * _Params.w) / _Params.y) + texelOffset.y);    
    
    return o;
}

half4 fragmentGradientsOpaque(vertex_output i) : COLOR
{
	float gradient = dot(tex2D(_GradientShape, i.uv0), i.uv1) ;
	float2 gradientColorUV = float2(i.uv0.z + gradient, i.uv0.w);
	return float4((tex2D(_GradientColor, gradientColorUV).rgb + _TextureSampleAdd) * i.color.rgb, 1.0);
}

half4 fragmentGradientsAlphaBlended(vertex_output i) : COLOR
{
	float gradient = dot(tex2D(_GradientShape, i.uv0), i.uv1) ;
	float2 gradientColorUV = float2(i.uv0.z + gradient, i.uv0.w);
	half4 output = (tex2D(_GradientColor, gradientColorUV) + _TextureSampleAdd) * i.color;
	
	output.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);

#ifdef UNITY_UI_ALPHACLIP
	clip(output.a - 0.001);
#endif

	return output;
}