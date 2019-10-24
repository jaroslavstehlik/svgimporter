// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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

half4 fragmentColor(vertex_output i) : COLOR
{
	return i.color;
}