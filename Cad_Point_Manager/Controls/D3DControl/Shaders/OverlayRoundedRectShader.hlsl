// OverlayRoundedRectShader.hlsl
cbuffer TransformationBuffer : register(b0)
{
    row_major matrix transformationMatrix; // 2D transformation matrix
};

// STREAM 0: per-vertex local quad coords (-1..1)
struct VSIn0
{
    float2 local : POSITION; // e.g. (-1,-1), (-1,1), (1,1) ...
};

// STREAM 1: per-instance rect parameters (all from vertex data)
struct VSIn1
{
    float2 Center : CENTER; // world coords
    float2 HalfSize : HALFSIZE; // world half extents
    float2 RadiusFeather : RF; // x = corner radius (world), y = feather (world)
    float4 Color : COLOR; // rgba
};

struct VSOut
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0; // local [-1,1] coords
    float2 hs : TEXCOORD1; // half-size (world) for PS conversion
    float2 rf : TEXCOORD2; // radius, feather (world)
    float4 col : COLOR;
};

VSOut VSMain(VSIn0 v0, VSIn1 v1)
{
    VSOut o;
    // local quad -> world position
    float2 worldPos = v1.Center + v0.local * v1.HalfSize;
    o.pos = mul(float4(worldPos, 0, 1), transformationMatrix);

    o.uv = v0.local; // keep local for SDF
    o.hs = v1.HalfSize; // pass down for world->uv conversions
    o.rf = v1.RadiusFeather; // (radius, feather) in world
    o.col = v1.Color;
    return o;
}

float sdRoundBox(float2 p, float2 b, float r)
{
    float2 q = abs(p) - b + r;
    return length(max(q, 0)) - r;
}

float4 PSMain(VSOut i) : SV_Target
{
    // Box in uv-space is [-1,1] -> b = 1
    // Convert world radius/feather to uv space using min half-size to keep corners circular
    float minHS = min(i.hs.x, i.hs.y);
    float r_uv = saturate(i.rf.x / max(minHS, 1e-6)); // corner radius (uv units)
    float f_uv = max(i.rf.y / max(minHS, 1e-6), 1e-6); // feather (uv units)

    float d = sdRoundBox(i.uv, float2(1, 1), r_uv);

    // Alpha ramp over feather band
    float a = saturate(0.5f - d / f_uv);

    return float4(i.col.rgb, i.col.a * a);
}
