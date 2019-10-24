// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#include "SVGImporterCG.cginc"

sampler2D _GradientColor;
sampler2D _GradientShape;
float4 _Params;

struct vertex_input
{
    float4 vertex : POSITION;	
    float2 texcoord0 : TEXCOORD0;
    float2 texcoord1 : TEXCOORD1;
    half4 color : COLOR;
};

struct vertex_input_normal
{
    float4 vertex : POSITION;	
    float2 texcoord0 : TEXCOORD0;
    float2 texcoord1 : TEXCOORD1;		    
    float3 normal : NORMAL;
    half4 color : COLOR;    
};

struct vertex_output
{
    float4 vertex : POSITION;			    
    float4 uv0 : TEXCOORD0;
    float4 uv1 : TEXCOORD1;
    half4 color : COLOR;
};

vertex_output vertexGradients(vertex_input v)
{
    vertex_output o;
    o.vertex = UnityObjectToClipPos(v.vertex);			    
    o.uv0.xy = v.texcoord0;
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

vertex_output vertexGradientsAntialiased(vertex_input_normal v)
{
    vertex_output o;
	o.vertex = UnityObjectToClipPos(Antialiasing(v.vertex, v.normal));
    o.uv0.xy = v.texcoord0;
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

float4 fragmentGradientsOpaque(vertex_output i) : COLOR
{
	float gradient = dot(tex2D(_GradientShape, i.uv0), i.uv1) ;
	float2 gradientColorUV = float2(i.uv0.z + gradient, i.uv0.w);
	return float4(tex2D(_GradientColor, gradientColorUV).rgb * i.color.rgb, 1.0);
}

float4 fragmentGradientsAlphaBlended(vertex_output i) : COLOR
{	
	float gradient = dot(tex2D(_GradientShape, i.uv0), i.uv1) ;
	float2 gradientColorUV = float2(i.uv0.z + gradient, i.uv0.w);
	return tex2D(_GradientColor, gradientColorUV) * i.color;
}