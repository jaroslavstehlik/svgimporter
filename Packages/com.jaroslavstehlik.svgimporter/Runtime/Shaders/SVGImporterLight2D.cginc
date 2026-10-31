// Lighting from the URP 2D Renderer's 2D lights, for the Universal2D passes. The 2D Renderer
// renders each light blend style in use into a screen-sized light texture and enables
// USE_SHAPE_LIGHT_TYPE_n for it; this combines them as URP's Sprite-Lit-Default does
// (CombinedShapeLightShared.hlsl), with a white mask: SVGs have no mask texture. Without 2D
// lights no keyword is enabled and colours pass through unlit.
//
// Declares URP's globals directly instead of including URP's HLSL files, so the shader still
// compiles in projects without URP. A pass that includes this needs:
//   #pragma multi_compile USE_SHAPE_LIGHT_TYPE_0 __
//   #pragma multi_compile USE_SHAPE_LIGHT_TYPE_1 __
//   #pragma multi_compile USE_SHAPE_LIGHT_TYPE_2 __
//   #pragma multi_compile USE_SHAPE_LIGHT_TYPE_3 __
// and, to let materials opt out with the SVG_UNLIT_2D keyword (SVGRenderer.receive2DLights off):
//   #pragma multi_compile_local __ SVG_UNLIT_2D

#define SVG_LIGHT_2D 1

half _HDREmulationScale;

#define SVG_SHAPE_LIGHT(index) \
    sampler2D _ShapeLightTexture##index; \
    half2 _ShapeLightBlendFactors##index; \
    half4 _ShapeLightMaskFilter##index; \
    half4 _ShapeLightInvertedFilter##index;

// Adds light texture index at uv to modulate and additive. The mask filter selects a mask channel,
// inverted or not: with a white mask that is 1, or 0 for an inverted channel.
#define SVG_ADD_SHAPE_LIGHT(index, uv, modulate, additive) \
    { \
        half3 shapeLight = tex2D(_ShapeLightTexture##index, uv).rgb; \
        if (any(_ShapeLightMaskFilter##index)) \
            shapeLight *= dot(1.0 - _ShapeLightInvertedFilter##index, _ShapeLightMaskFilter##index); \
        modulate += shapeLight * _ShapeLightBlendFactors##index.x; \
        additive += shapeLight * _ShapeLightBlendFactors##index.y; \
    }

#ifdef USE_SHAPE_LIGHT_TYPE_0
SVG_SHAPE_LIGHT(0)
#endif
#ifdef USE_SHAPE_LIGHT_TYPE_1
SVG_SHAPE_LIGHT(1)
#endif
#ifdef USE_SHAPE_LIGHT_TYPE_2
SVG_SHAPE_LIGHT(2)
#endif
#ifdef USE_SHAPE_LIGHT_TYPE_3
SVG_SHAPE_LIGHT(3)
#endif

// The light textures' coordinates of a vertex: its position on screen, from 0 to 1.
#define SVG_LIGHTING_COORDS(n) float2 lightingUV : TEXCOORD##n;
#define SVG_TRANSFER_LIGHTING(o) o.lightingUV = ComputeScreenPos(o.vertex / o.vertex.w).xy;
#define SVG_APPLY_LIGHTING(color, i) color = SVGLight2D(color, i.lightingUV);

half4 SVGLight2D(half4 color, float2 lightingUV)
{
#if !defined(SVG_UNLIT_2D) && (defined(USE_SHAPE_LIGHT_TYPE_0) || defined(USE_SHAPE_LIGHT_TYPE_1) || defined(USE_SHAPE_LIGHT_TYPE_2) || defined(USE_SHAPE_LIGHT_TYPE_3))
    half3 modulate = 0.0, additive = 0.0;
#ifdef USE_SHAPE_LIGHT_TYPE_0
    SVG_ADD_SHAPE_LIGHT(0, lightingUV, modulate, additive)
#endif
#ifdef USE_SHAPE_LIGHT_TYPE_1
    SVG_ADD_SHAPE_LIGHT(1, lightingUV, modulate, additive)
#endif
#ifdef USE_SHAPE_LIGHT_TYPE_2
    SVG_ADD_SHAPE_LIGHT(2, lightingUV, modulate, additive)
#endif
#ifdef USE_SHAPE_LIGHT_TYPE_3
    SVG_ADD_SHAPE_LIGHT(3, lightingUV, modulate, additive)
#endif
    color.rgb = max(0.0, _HDREmulationScale * (color.rgb * modulate + additive));
#endif
    return color;
}
