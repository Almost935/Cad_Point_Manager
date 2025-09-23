// ToggleAnchor.hlsl

cbuffer TransformBuffer : register(b0)
{
    // World->Clip for your 2D scene
    row_major float4x4 ViewProj;
}

// Stream 0: unit quad vertices in [-1..1] space
struct VSQuadIn
{
    float2 local : POSITION; // e.g., (-1,-1),(1,-1),(1,1), (-1,-1),(1,1),(-1,1)
};

// Stream 1: per-instance data
struct VSInst
{
    float2 center : TEXCOORD0; // world center
    float2 size : TEXCOORD1; // world half-width/half-heaight
    float2 rf : TEXCOORD2; // x=corner radius (world), y=feather (world)
    float4 baseCol : TEXCOORD3; // normal color
    float4 hoverCol : TEXCOORD4; // hover color
    float4 pressCol : TEXCOORD5; // pressed color
    float on : TEXCOORD6; // 0 or 1 (draw inner dot)
    uint state : TEXCOORD7; // 0=normal, 1=hover, 2=pressed
};

struct VSOut
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0; // local coords scaled into world units
    float2 size : TEXCOORD1; // world half extents
    float2 rf : TEXCOORD2; // radius/feather (world)
    float4 baseCol : TEXCOORD3;
    float4 hoverCol : TEXCOORD4;
    float4 pressCol : TEXCOORD5;
    float on : TEXCOORD6;
    uint state : TEXCOORD7;
};

VSOut VSMain(VSQuadIn v, VSInst i)
{
    VSOut o;

    // Local [-1..1] → world offset using half-size in world units
    float2 worldPos = i.center + v.local * i.size;

    o.pos = mul(float4(worldPos, 0.0, 1.0), ViewProj);
    o.uv = v.local * i.size; // keep world-scaled local for SDF math
    o.size = i.size;
    o.rf = i.rf;
    o.baseCol = i.baseCol;
    o.hoverCol = i.hoverCol;
    o.pressCol = i.pressCol;
    o.on = i.on;
    o.state = i.state;

    return o;
}

float4 PSMain(VSOut i) : SV_Target
{
    // --- SDF for rounded rect (unchanged) ---
    float2 q = abs(i.uv) - (i.size - i.rf.x);
    float d = length(max(q, 0.0)) - i.rf.x;

    // ===== Screen-space AA using derivatives (uniform across corners/sides) =====
    // aa ~ distance change across one pixel in screen space
    float aa = max(fwidth(d), 1e-6);

    // Fill alpha (inside the shape), centered on the edge (d = 0)
    // This yields ~1 inside, ~0 outside, with a smooth ~1px transition
    float fillAlpha = smoothstep(0.5 * aa, -0.5 * aa, d);

    // ----- Choose color by UI state (unchanged) -----
    float4 col = i.baseCol;
    if (i.state == 1)
        col = i.hoverCol;
    if (i.state == 2)
        col = i.pressCol;

    // Optional "on" dot (unchanged)
    if (i.on > 0.5)
    {
        float dotR = min(i.size.x, i.size.y) * 0.35;
        float dotA = saturate(1.0 - length(i.uv) / dotR);
        float4 dotCol = float4(0, 0, 0, 1);
        col = lerp(col, dotCol, dotA);
    }

    // ===== Uniform-thickness border in pixels =====
    // Desired border thickness in *pixels* (tweak to taste)
    const float BorderPx = 1.0;

    // Outer coverage (edge-inclusive)
    float outer = smoothstep(0.5 * aa, -0.5 * aa, d);
    // Inner coverage: offset SDF inward by BorderPx pixels
    float inner = smoothstep(0.5 * aa, -0.5 * aa, d + BorderPx * aa);

    // Ring mask = region between the two iso-lines; ~BorderPx thick in pixels
    float borderMask = saturate(outer - inner);

    // Blend a border color only in the ring; keep the fill elsewhere
    float4 borderCol = float4(0, 0, 0, 0.3);
    float4 outCol = lerp(col, borderCol, borderMask);

    // Final alpha = fill coverage (so only inside the shape is visible)
    outCol.a *= fillAlpha;
    if (outCol.a <= 0.001)
        discard;
    return outCol;
}
