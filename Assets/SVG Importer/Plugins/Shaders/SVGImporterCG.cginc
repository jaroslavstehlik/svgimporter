// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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