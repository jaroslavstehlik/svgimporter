

// SVG colors are authored in sRGB and stored as vertex colors. In a Linear color space
// project they must be converted, or they render washed out. (UI shaders do not need
// this: the Canvas converts its vertex colors itself.)
// Exact sRGB transfer functions (Unity's GammaToLinearSpace is an approximation), so that solid
// colours and gradient stops convert the same.
float3 SVGLinearToSRGB(float3 c)
{
	c = max(c, 0.0);
	return lerp(1.055 * pow(c, 1.0 / 2.4) - 0.055, c * 12.92, step(c, 0.0031308));
}

float3 SVGSRGBToLinear(float3 c)
{
	return lerp(pow((max(c, 0.0) + 0.055) / 1.055, 2.4), c / 12.92, step(c, 0.04045));
}

// Vertex colours are sRGB; Linear color space projects need them linear.
half4 SVGVertexColor(half4 color)
{
#ifndef UNITY_COLORSPACE_GAMMA
    color.rgb = SVGSRGBToLinear(color.rgb);
#endif
    return color;
}

// The antialiasing width from the project settings: the fringe is half this many pixels wide.
float SVG_ANTIALIASING_WIDTH;

// Moves a vertex on a shape's outline along its antialiasing extrusion vector (DESIGN.md, 7.2):
// fringe vertices outward or inward, and the fill's outline vertices inward, so that the fringe is
// centred on the outline and as wide on screen as the antialiasing width, whatever the transform,
// camera or canvas. The vector's length is the corner's miter factor; vertices without one stay put.
float4 Antialiasing(float4 vertex, float3 extrusion)
{
	float length2 = dot(extrusion.xy, extrusion.xy);
	if (length2 == 0.0) return vertex;
	float3 direction = float3(extrusion.xy * rsqrt(length2), 0.0);

	// Pixels per object unit along the direction: the derivative of the screen position.
	float4 clip = UnityObjectToClipPos(vertex);
	float4 clipDirection = mul(UNITY_MATRIX_VP, float4(mul((float3x3)unity_ObjectToWorld, direction), 0.0));
	float2 ndc = (clipDirection.xy * clip.w - clip.xy * clipDirection.w) / (clip.w * clip.w);
	float pixels = length(ndc * 0.5 * _ScreenParams.xy);
	if (pixels <= 0.0) return vertex;

	// Each edge of the fringe moves half the fringe's width, a quarter of the setting.
	vertex.xy += extrusion.xy * (0.25 * SVG_ANTIALIASING_WIDTH / pixels);
	return vertex;
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
