// Prototype: Gaussian-blurred (feathered) rectangles and rounded rectangles, computed analytically.
// See DESIGN.md, section 16, item 4.
//
// Coverage of a shape blurred with a Gaussian of standard deviation sigma is the convolution of
// the shape with the Gaussian. For an axis-aligned rectangle the Gaussian is separable, so the
// result is the product of two 1D integrals, each a difference of two erf values: exact.
// For a rounded rectangle, every horizontal slice is a span whose width depends on y. The x
// integral of each slice is exact (erf); the y integral sums a few slices, each weighted by the
// exact Gaussian mass of its y interval. For a plain rectangle that sum is exact as well.
//
// With a small sigma (about 0.4 pixels) the same function draws an antialiased shape, so blur and
// antialiasing are one continuous effect.
Shader "SVG Importer/Prototypes/Feathered Shape"
{
    Properties
    {
        _Color ("Color", Color) = (0, 0, 0, 1)
        _HalfSize ("Half Size", Vector) = (100, 75, 0, 0)
        _Radius ("Corner Radius", Float) = 0
        _Sigma ("Sigma", Float) = 12
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color;
            float4 _HalfSize;
            float _Radius;
            float _Sigma;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 position : SV_POSITION; float2 local : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                // The quad is centred on the shape, so object space is shape space.
                o.local = v.vertex.xy;
                return o;
            }

            // Abramowitz and Stegun 7.1.26: |error| < 1.5e-7.
            float erfApprox(float x)
            {
                float a = abs(x);
                float t = 1.0 / (1.0 + 0.3275911 * a);
                float poly = ((((1.061405429 * t - 1.453152027) * t + 1.421413741) * t - 0.284496736) * t + 0.254829592) * t;
                return sign(x) * (1.0 - poly * exp(-a * a));
            }

            // Fraction of a 1D Gaussian centred at x that falls inside [lo, hi].
            float gaussianMass(float x, float lo, float hi, float sigma)
            {
                float k = 0.70710678 / sigma;
                return 0.5 * (erfApprox((hi - x) * k) - erfApprox((lo - x) * k));
            }

            // Coverage of a blurred rounded rectangle centred at the origin.
            float blurredRoundedRect(float2 p, float2 halfSize, float radius, float sigma)
            {
                // Only slices within 4 sigma of p contribute (3 sigma would drop 0.27% of the mass).
                float lo = max(-halfSize.y, p.y - 4.0 * sigma);
                float hi = min(halfSize.y, p.y + 4.0 * sigma);
                if (hi <= lo) return 0.0;

                const int slices = 8;
                float step = (hi - lo) / slices;
                float coverage = 0.0;
                for (int i = 0; i < slices; i++)
                {
                    float y0 = lo + step * i;
                    float y = y0 + step * 0.5;
                    // Half width of the rounded rectangle at height y.
                    float dy = max(abs(y) - (halfSize.y - radius), 0.0);
                    float halfWidth = halfSize.x - radius + sqrt(max(radius * radius - dy * dy, 0.0));
                    coverage += gaussianMass(p.x, -halfWidth, halfWidth, sigma) * gaussianMass(p.y, y0, y0 + step, sigma);
                }
                return coverage;
            }

            float4 frag(v2f i) : SV_Target
            {
                float coverage = blurredRoundedRect(i.local, _HalfSize.xy, _Radius, _Sigma);
                return float4(_Color.rgb, _Color.a * coverage);
            }
            ENDCG
        }
    }
}
