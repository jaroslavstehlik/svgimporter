// Lambert lighting for the Universal Render Pipeline.
// Declares URP's main light globals directly instead of including URP's
// HLSL files, so the shader still compiles in projects without URP.

#include "UnityCG.cginc"
#include "SVGImporterCG.cginc"

float4 _MainLightPosition;
half4 _MainLightColor;

struct vertex_input
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    half4 color : COLOR;
};

struct vertex_output
{
    float4 vertex : SV_POSITION;
    half4 color : COLOR;
};

vertex_output vertexDiffuse(vertex_input v)
{
    vertex_output o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    float3 normalWS = UnityObjectToWorldNormal(v.normal);
    half ndotl = saturate(dot(normalWS, _MainLightPosition.xyz));
    half3 lighting = _MainLightColor.rgb * ndotl + ShadeSH9(float4(normalWS, 1.0));
    half4 color = SVGVertexColor(v.color);
    o.color = half4(color.rgb * lighting, color.a);
    return o;
}

half4 fragmentDiffuse(vertex_output i) : SV_Target
{
    return i.color;
}
