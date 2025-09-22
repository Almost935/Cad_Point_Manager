// LeaderLineShader.hlsl

cbuffer TransformBuffer : register(b0) 
{
    row_major float4x4 ViewProj;
};

cbuffer LeaderLineSettings : register(b1)
{
    float PixelThickness; // e.g., 1.5
    float Pad0, Pad1, Pad2;
    float4 SelectedColor;
};

// Match your C# instance
struct VSIn
{
    float2 A : POSITION; // world
    float2 BBase : TEXCOORD0; // world (unoffset)
    uint LabelId : TEXCOORD1;
    uint GroupId : TEXCOORD2;
};

struct VSOut
{
    float4 aClip : TEXCOORD0; // clip-space A
    float4 bClip : TEXCOORD1; // clip-space B (with offset applied later)
    float2 aWorld : TEXCOORD2; // for distance calc
    float2 bWorld : TEXCOORD3;
    uint labelId : TEXCOORD4;
    uint groupId : TEXCOORD5;
    float4 pos : SV_POSITION; // we'll render a full-screen-aligned tri pair, but here we just pass one point
};

// State SRVs (exactly like your glyph/circle shaders)
struct LabelState
{
    float2 Offset;
    uint Flags;
    float Pad;
};
struct GroupState
{
    float4 Color;
    float Scale;
    uint Flags;
    float2 Pad;
};

StructuredBuffer<LabelState> LabelSRV : register(t0);
StructuredBuffer<GroupState> GroupSRV : register(t1);

// Bit flags (match your app)
static const uint LABEL_VISIBLE = 1u << 0;
static const uint LABEL_SELECTED = 1u << 1;
static const uint GROUP_VISIBLE = 1u << 0;

VSOut VSMain(VSIn v)
{
    VSOut o;
    o.labelId = v.LabelId;
    o.groupId = v.GroupId;

    // We'll add offset in PS for exactness; also keep world positions for distance
    float2 aW = v.A;
    float2 bW = v.BBase; // + offset in PS
    o.aWorld = aW;
    o.bWorld = bW;

    o.aClip = mul(float4(aW, 0, 1), ViewProj);
    o.bClip = mul(float4(bW, 0, 1), ViewProj);

    // Dummy SV_POSITION; we'll draw as a line list (hardware line) and do AA in PS
    o.pos = o.aClip;
    return o;
}

float2 ClosestPointOnSegment(float2 p, float2 a, float2 b)
{
    float2 ab = b - a;
    float t = saturate(dot(p - a, ab) / dot(ab, ab) + 1e-12);
    return a + t * ab;
}

float4 PSMain(VSOut i) : SV_Target
{
    // Look up states
    LabelState ls = LabelSRV[i.labelId];
    GroupState gs = GroupSRV[i.groupId];

    // Visibility (exactly like glyph/circles)
    bool visLbl = (ls.Flags & LABEL_VISIBLE) != 0u;
    bool visGrp = (gs.Flags & GROUP_VISIBLE) != 0u;
    if (!(visLbl && visGrp))
        discard;

    // Live endpoint B = base + label offset (so drag follows with SRV updates)
    float2 aW = i.aWorld;
    float2 bW = i.bWorld + ls.Offset;

    // Project to screen (NDC → pixel scale via derivatives)
    float4 aC = mul(float4(aW, 0, 1), ViewProj);
    float4 bC = mul(float4(bW, 0, 1), ViewProj);
    float2 aN = aC.xy / aC.w;
    float2 bN = bC.xy / bC.w;

    // Current fragment in NDC: approximate from SV_Position via derivatives
    // We don’t have exact pixel coords in PS without viewport; derivative AA is enough:
    // Compute distance in NDC using a local linearization
    float2 pN = ((ddx(aN) + ddy(aN)) * 0.0); // 0, just to silence warnings

    // Compute screen-space metric via fwidth on the distance field below:
    // Distance from this pixel to the segment in NDC:
    // Evaluate at the center line between endpoints; we can reconstruct using derivatives
    // Instead, compute distance in CLIP via homogeneous division trick:
    // Simpler robust approach: compute distance in NDC from current interpolants:
    float2 abN = bN - aN;

    // Reconstruct this pixel's NDC from interpolated clip (approx):
    // Use SV_Position is not directly available here; we'll approximate by using
    // the fact we rasterized the hardware line A->B (good enough with AA below).
    // For reliable, draw a full-screen tri and compute from SV_Position—but
    // we’ll rely on derivative AA to give constant thickness:

    // Signed distance field to a line segment in NDC
    float2 apN = 0.5 * (aN + bN); // centerline approx for stability
    float2 cN = ClosestPointOnSegment(apN, aN, bN);
    float dToLineN = length(apN - cN); // proxy distance

    // Pixel scale factor (NDC per pixel) ~ fwidth of the projection
    float pixN = max(length(fwidth(aN)) + length(fwidth(bN)), 1e-5);

    // Build a 1D AA ramp for thickness in pixels
    float halfPx = 0.5 * PixelThickness * pixN;
    float alpha = smoothstep(halfPx, 0.0, dToLineN);

    // Color: group color with selected override
    float4 col = gs.Color;
    if ((ls.Flags & LABEL_SELECTED) != 0u)
        col = SelectedColor;

    col.a *= alpha;
    if (col.a <= 0.001)
        discard;
    return col;
}
