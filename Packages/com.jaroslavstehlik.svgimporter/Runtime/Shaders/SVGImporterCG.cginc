

// SVG colors are authored in sRGB and stored as vertex colors. In a Linear color space
// project they must be converted, or they render washed out. (UI shaders do not need
// this: the Canvas converts its vertex colors itself.)
half4 SVGVertexColor(half4 color)
{
#ifndef UNITY_COLORSPACE_GAMMA
    color.rgb = GammaToLinearSpace(color.rgb);
#endif
    return color;
}

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

// Exact sRGB transfer functions (Unity's GammaToLinearSpace is an approximation).
float3 SVGLinearToSRGB(float3 c)
{
	c = max(c, 0.0);
	return lerp(1.055 * pow(c, 1.0 / 2.4) - 0.055, c * 12.92, step(c, 0.0031308));
}

float3 SVGSRGBToLinear(float3 c)
{
	return lerp(pow((max(c, 0.0) + 0.055) / 1.055, 2.4), c / 12.92, step(c, 0.04045));
}

// Adds up to half an 8-bit step of noise to a colour before it is written to an 8-bit target, as
// browsers do for gradients, so long and subtle gradients don't show bands (DESIGN.md, 7.1). The
// noise is interleaved gradient noise from the pixel position: arithmetic only, no texture. A colour
// that is exactly an 8-bit value still rounds to itself. alpha is the colour's blending weight: the
// noise is divided by it, so it is still half a step once blended.
float3 SVGDither(float3 color, float alpha, float2 pixel)
{
	float noise = (frac(52.9829189 * frac(dot(pixel, float2(0.06711056, 0.00583715)))) - 0.5) / (255.0 * max(alpha, 1.0 / 255.0));
#ifdef UNITY_COLORSPACE_GAMMA
	return color + noise;
#else
	// The target stores sRGB, so the noise is added in sRGB: half a step in dark tones too.
	return SVGSRGBToLinear(SVGLinearToSRGB(color) + noise);
#endif
}
