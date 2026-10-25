// Gradient colours from stop data (DESIGN.md, 7.1). Each gradient has a slot in the atlas texture
// (RGBAHalf, read with Load: no filtering, so neighbouring gradients can't bleed in). Row 0 of the
// slot holds two texels per stop, its colour (sRGB, straight alpha) and its offset. Row 1 holds the
// stop count, the spread method (0 pad, 1 reflect, 2 repeat) and the focal offset, then the focal
// radius. Written by SVGAtlas.RenderGradient.
//
// The atlas is one texture for all SVG materials, so SVGAtlas binds it, and its layout, as global
// shader properties: materials, including custom ones, never hold a reference to it.

Texture2D _SVGGradientAtlas;
// Atlas width and height, slot width and height, in texels.
float4 _SVGGradientAtlasParams;

// The gradient's colour at position t (0 to 1), from the stops of the slot at origin. Colours are
// interpolated in sRGB, as browsers do, and converted to linear in Linear color space projects.
float4 SVGGradientColor(int2 origin, int count, float t)
{
	// The last stop at or before t, by binary search over the stop offsets (at most 64 stops).
	int low = 0, high = count - 1;
	[loop] for (int step = 0; step < 7 && low < high; step++)
	{
		int middle = (low + high + 1) >> 1;
		if (_SVGGradientAtlas.Load(int3(origin.x + middle * 2 + 1, origin.y, 0)).r <= t) low = middle;
		else high = middle - 1;
	}
	int next = min(low + 1, max(count - 1, 0));

	float offset0 = _SVGGradientAtlas.Load(int3(origin.x + low * 2 + 1, origin.y, 0)).r;
	float offset1 = _SVGGradientAtlas.Load(int3(origin.x + next * 2 + 1, origin.y, 0)).r;
	float4 color = lerp(_SVGGradientAtlas.Load(int3(origin.x + low * 2, origin.y, 0)),
	                    _SVGGradientAtlas.Load(int3(origin.x + next * 2, origin.y, 0)),
	                    saturate((t - offset0) / max(offset1 - offset0, 1e-6)));
#ifndef UNITY_COLORSPACE_GAMMA
	color.rgb = SVGSRGBToLinear(color.rgb);
#endif
	return color;
}

// The position along a radial gradient (x), and whether the point is painted at all (y). The
// gradient runs from the focal circle (centre 0.5 + focalOffset, radius focalRadius) at 0 to the end
// circle (centre 0.5, radius 0.5) at 1; a point's position is that of the largest circle in between,
// or beyond, that passes through it, as SVG 2 defines it. Points that no circle with a positive
// radius reaches, outside the cone of a focal point outside the end circle, aren't painted.
float2 SVGRadialPosition(float2 uv, float2 focalOffset, float focalRadius)
{
	float2 centres = -focalOffset;
	float radii = 0.5 - focalRadius;
	float2 p = uv - 0.5 - focalOffset;
	// |p - t * centres| = focalRadius + t * radii, solved for t: a t^2 - 2 b t + c = 0.
	float a = dot(centres, centres) - radii * radii;
	float b = dot(p, centres) + focalRadius * radii;
	float c = dot(p, p) - focalRadius * focalRadius;
	if (abs(a) < 1e-5)
	{
		// The focal circle touches the end circle: one solution. If the circles are the same,
		// points inside them are at 0, as in resvg, and points outside aren't painted.
		if (abs(b) < 1e-6) return float2(0.0, c <= 0.0);
		float t = c / (2.0 * b);
		return float2(t, focalRadius + t * radii >= 0.0);
	}
	float d = b * b - a * c;
	if (d < 0.0) return 0.0;
	float root = sqrt(d);
	float t0 = (b + root) / a, t1 = (b - root) / a;
	float larger = max(t0, t1), smaller = min(t0, t1);
	if (focalRadius + larger * radii >= 0.0) return float2(larger, 1.0);
	return float2(smaller, focalRadius + smaller * radii >= 0.0);
}

// The position along the gradient beyond its ends: clamped (pad), mirrored (reflect) or repeated.
float SVGSpreadPosition(float t, float spread)
{
	if (spread > 1.5) return frac(t);
	if (spread > 0.5) return 1.0 - abs(frac(t * 0.5) * 2.0 - 1.0);
	return saturate(t);
}

// The colour of a gradient fill at uv, the point in the gradient's unit square: linear gradients
// run along x from 0 to 1, radial and conical ones are centred at (0.5, 0.5) and radial ones end at
// a radius of 0.5. type selects the gradient type (x linear, y radial, z conical, w solid). The
// position is computed per pixel, so it is exact at any zoom (DESIGN.md, 7.1).
float4 SVGGradientFill(float2 slot, float2 uv, float4 type)
{
	int2 origin = int2(slot + 0.5);
	float4 header = _SVGGradientAtlas.Load(int3(origin.x, origin.y + 1, 0));
	float focalRadius = _SVGGradientAtlas.Load(int3(origin.x + 1, origin.y + 1, 0)).r;

	float2 radial = SVGRadialPosition(uv, header.zw, focalRadius);
	float2 d = uv - 0.5;
	float conical = frac(atan2(d.y, d.x) * 0.15915494 + 1.0);
	float t = dot(float4(uv.x, radial.x, conical, 1.0), type);
	float painted = lerp(1.0, radial.y, type.y);

	return SVGGradientColor(origin, (int)header.r, SVGSpreadPosition(t, header.g)) * painted;
}
