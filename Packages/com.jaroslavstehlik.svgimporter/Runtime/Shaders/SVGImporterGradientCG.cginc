

#include "SVGImporterCG.cginc"
#include "SVGImporterGradientColor.cginc"


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
    float4 vertex : SV_POSITION;			    
    float4 uv0 : TEXCOORD0;
    float4 uv1 : TEXCOORD1;
    half4 color : COLOR;
    SVG_LIGHTING_COORDS(2)
};

vertex_output vertexGradients(vertex_input v)
{
    vertex_output o;
    o.vertex = UnityObjectToClipPos(v.vertex);			    
    o.uv0.xy = v.texcoord0;
    o.color = SVGVertexColor(v.color);
	
	// The gradient's slot in the atlas (atlas width and height, slot width and height),
	// in texels, and which gradient position to use (x linear, y radial, z conical, w solid).
	float slotIndex = v.texcoord1.x * _SVGGradientAtlasParams.z;
	o.uv0.z = fmod(slotIndex, _SVGGradientAtlasParams.x);
	o.uv0.w = floor(slotIndex / _SVGGradientAtlasParams.x) * _SVGGradientAtlasParams.w;
	o.uv1 = saturate(1.0 - abs(float4(0.0, 1.0, 2.0, 3.0) - v.texcoord1.y));
    SVG_TRANSFER_LIGHTING(o)
    return o;
}

vertex_output vertexGradientsAntialiased(vertex_input_normal v)
{
    vertex_output o;
	o.vertex = UnityObjectToClipPos(Antialiasing(v.vertex, v.normal));
    o.uv0.xy = v.texcoord0;
    o.color = SVGVertexColor(v.color);
	
	// The gradient's slot in the atlas (atlas width and height, slot width and height),
	// in texels, and which gradient position to use (x linear, y radial, z conical, w solid).
	float slotIndex = v.texcoord1.x * _SVGGradientAtlasParams.z;
	o.uv0.z = fmod(slotIndex, _SVGGradientAtlasParams.x);
	o.uv0.w = floor(slotIndex / _SVGGradientAtlasParams.x) * _SVGGradientAtlasParams.w;
	o.uv1 = saturate(1.0 - abs(float4(0.0, 1.0, 2.0, 3.0) - v.texcoord1.y));
    SVG_TRANSFER_LIGHTING(o)
    return o;
}

float4 fragmentGradientsOpaque(vertex_output i) : SV_Target
{
	float4 output = float4(SVGGradientFill(i.uv0.zw, i.uv0.xy, i.uv1).rgb * i.color.rgb, 1.0);
	SVG_APPLY_LIGHTING(output, i)
	return float4(SVGDither(output.rgb, 1.0, i.vertex.xy), 1.0);
}

float4 fragmentGradientsAlphaBlended(vertex_output i) : SV_Target
{	
	float4 gradientColor = SVGGradientFill(i.uv0.zw, i.uv0.xy, i.uv1);
	float4 output = gradientColor * i.color;
	SVG_APPLY_LIGHTING(output, i)
	output.rgb = SVGDither(output.rgb, output.a, i.vertex.xy);
	return output;
}