

#include "SVGImporterCG.cginc"
#include "SVGImporterGradientColor.cginc"


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
    float4 vertex : SV_POSITION;			    
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

	// The gradient's slot in the atlas (atlas width and height, slot width and height),
	// in texels, and which gradient position to use (x linear, y radial, z conical, w solid).
	float slotIndex = v.texcoord1.x * _SVGGradientAtlasParams.z;
	o.uv0.z = fmod(slotIndex, _SVGGradientAtlasParams.x);
	o.uv0.w = floor(slotIndex / _SVGGradientAtlasParams.x) * _SVGGradientAtlasParams.w;
	o.uv1 = saturate(1.0 - abs(float4(0.0, 1.0, 2.0, 3.0) - v.texcoord1.y));

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
	
	// The gradient's slot in the atlas (atlas width and height, slot width and height),
	// in texels, and which gradient position to use (x linear, y radial, z conical, w solid).
	float slotIndex = v.texcoord1.x * _SVGGradientAtlasParams.z;
	o.uv0.z = fmod(slotIndex, _SVGGradientAtlasParams.x);
	o.uv0.w = floor(slotIndex / _SVGGradientAtlasParams.x) * _SVGGradientAtlasParams.w;
	o.uv1 = saturate(1.0 - abs(float4(0.0, 1.0, 2.0, 3.0) - v.texcoord1.y));
    
    return o;
}

half4 fragmentGradientsOpaque(vertex_output i) : SV_Target
{
	float4 gradientColor = SVGGradientColor(i.uv0.zw, dot(SVGGradientShape(i.uv0.xy), i.uv1));
	return float4(SVGDither((gradientColor.rgb + _TextureSampleAdd) * i.color.rgb, 1.0, i.vertex.xy), 1.0);
}

half4 fragmentGradientsAlphaBlended(vertex_output i) : SV_Target
{
	float4 gradientColor = SVGGradientColor(i.uv0.zw, dot(SVGGradientShape(i.uv0.xy), i.uv1));
	half4 output = (gradientColor + _TextureSampleAdd) * i.color;
	
	output.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
	output.rgb = SVGDither(output.rgb, output.a, i.vertex.xy);

#ifdef UNITY_UI_ALPHACLIP
	clip(output.a - 0.001);
#endif

	return output;
}