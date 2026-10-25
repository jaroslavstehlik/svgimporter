

#include "SVGImporterCG.cginc"

sampler2D _GradientColor;
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

// The position along the gradient for each gradient type (x linear, y radial, z conical, w solid),
// computed rather than read from a lookup texture, so it is exact at any zoom (DESIGN.md, 7.1).
// uv is the point in the gradient's unit square: linear gradients run along x from 0 to 1, radial
// and conical gradients are centred at (0.5, 0.5) and radial ones end at a radius of 0.5. Outside
// that range the position is clamped, which pads with the end colours.
float4 SVGGradientShape(float2 uv)
{
	float2 d = uv - 0.5;
	return saturate(float4(uv.x, length(d) * 2.0, frac(atan2(d.y, d.x) * 0.15915494 + 1.0), 1.0));
}

half4 fragmentGradientsOpaque(vertex_output i) : SV_Target
{
	float gradient = dot(SVGGradientShape(i.uv0.xy), i.uv1) ;
	float2 gradientColorUV = float2(i.uv0.z + gradient, i.uv0.w);
	return float4((tex2D(_GradientColor, gradientColorUV).rgb + _TextureSampleAdd) * i.color.rgb, 1.0);
}

half4 fragmentGradientsAlphaBlended(vertex_output i) : SV_Target
{
	float gradient = dot(SVGGradientShape(i.uv0.xy), i.uv1) ;
	float2 gradientColorUV = float2(i.uv0.z + gradient, i.uv0.w);
	half4 output = (tex2D(_GradientColor, gradientColorUV) + _TextureSampleAdd) * i.color;
	
	output.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);

#ifdef UNITY_UI_ALPHACLIP
	clip(output.a - 0.001);
#endif

	return output;
}