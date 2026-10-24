

#include "SVGImporterCG.cginc"

struct vertex_input
{
    float4 vertex : POSITION;			    
    half4 color : COLOR;			    
};

struct vertex_input_normal
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

vertex_output vertexColor(vertex_input v)
{
    vertex_output o;
    o.vertex = UnityObjectToClipPos(v.vertex);    
    o.color = v.color;
    return o;
}

vertex_output vertexColorAntialiased(vertex_input_normal v)
{
	vertex_output o;	
	o.vertex = UnityObjectToClipPos(Antialiasing(v.vertex, v.normal));
	o.color = v.color;
	return o;
}

half4 fragmentColor(vertex_output i) : SV_Target
{
	return i.color;
}