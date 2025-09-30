// OverlayOutlineShader.hlsl
cbuffer TransformationBuffer : register(b0) // you already have this
{
    row_major float4x4 transformationMatrix;
};

cbuffer OutlineSettings : register(b1)
{
    float2 RectMinWorld; // world-space min (x,y) of the rect you already compute
    float2 RectMaxWorld; // world-space max (x,y)
    float ThicknessPx; // desired border thickness in pixels (e.g., 1.5)
    float FeatherPx; // AA feather in pixels (e.g., 1.0)
    float4 BorderColor; // RGBA
}

// Use the same vertex layout you're filling now: POSITION + COLOR, but COLOR is unused here.
struct VSIn
{
    float3 pos : POSITION;
    float4 col : COLOR; // ignored
};
struct VSOut
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0; // 0..1 across the rect
};

VSOut VSMain(VSIn i)
{
    VSOut o;
    // Standard transform
    o.pos = mul(float4(i.pos, 1.0f), transformationMatrix);

    // Map world position → UV in [0,1] using the rect bounds you already know
    float2 size = max(RectMaxWorld - RectMinWorld, 1e-8); // avoid div-by-zero
    o.uv = (i.pos.xy - RectMinWorld) / size;
    return o;
}

float4 PSMain(VSOut i) : SV_TARGET
{
    // UV distance to nearest edge
    float2 dEdge = min(i.uv, 1.0 - i.uv);

    // Per-axis UV→pixel scale (how many UV units per pixel)
    float du = max(length(float2(ddx(i.uv.x), ddy(i.uv.x))), 1e-6);
    float dv = max(length(float2(ddx(i.uv.y), ddy(i.uv.y))), 1e-6);

    // Convert distances to pixels along each axis
    float2 dPx = float2(dEdge.x / du, dEdge.y / dv);
    float minPx = min(dPx.x, dPx.y);

    // Smooth border in pixels
    float edge = smoothstep(ThicknessPx + FeatherPx, ThicknessPx, minPx);

    return float4(BorderColor.rgb, BorderColor.a * edge);
}
