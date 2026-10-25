// Gradient colours from stop data (DESIGN.md, 7.1). Each gradient has a slot in the atlas texture
// (RGBAHalf, read with Load: no filtering, so neighbouring gradients can't bleed in). Row 0 of the
// slot holds two texels per stop, its colour (sRGB, straight alpha) and its offset; row 1 starts
// with the stop count. Written by SVGAtlas.RenderGradient.

Texture2D _GradientColor;

// The gradient's colour at position t, for the slot whose first texel is slot. Colours are
// interpolated in sRGB, as browsers do, and converted to linear in Linear color space projects.
float4 SVGGradientColor(float2 slot, float t)
{
	int2 origin = int2(slot + 0.5);
	int count = (int)_GradientColor.Load(int3(origin.x, origin.y + 1, 0)).r;

	// The last stop at or before t, by binary search over the stop offsets (at most 64 stops).
	int low = 0, high = count - 1;
	[loop] for (int step = 0; step < 7 && low < high; step++)
	{
		int middle = (low + high + 1) / 2;
		if (_GradientColor.Load(int3(origin.x + middle * 2 + 1, origin.y, 0)).r <= t) low = middle;
		else high = middle - 1;
	}
	int next = min(low + 1, max(count - 1, 0));

	float offset0 = _GradientColor.Load(int3(origin.x + low * 2 + 1, origin.y, 0)).r;
	float offset1 = _GradientColor.Load(int3(origin.x + next * 2 + 1, origin.y, 0)).r;
	float4 color = lerp(_GradientColor.Load(int3(origin.x + low * 2, origin.y, 0)),
	                    _GradientColor.Load(int3(origin.x + next * 2, origin.y, 0)),
	                    saturate((t - offset0) / max(offset1 - offset0, 1e-6)));
#ifndef UNITY_COLORSPACE_GAMMA
	color.rgb = SVGSRGBToLinear(color.rgb);
#endif
	return color;
}
