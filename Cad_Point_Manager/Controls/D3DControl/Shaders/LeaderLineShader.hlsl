// LeaderLineShader.hlsl
// One POINT per line: (A, BBase, LabelId, GroupId) -> GS extrudes pixel-width quad.
// Uses LabelSRV/GroupSRV for visibility, selection, color; adds LabelSRV.Offset so it follows drag.

cbuffer TransformBuffer : register(b0) // same as your other passes
{
    row_major float4x4 ViewProj; // world -> clip
}

cbuffer LeaderLineSettings : register(b1)
{
    float2 InvViewport; // (1/width, 1/height) in pixels
    float PixelThickness; // e.g., 1.5
    float _pad0;
    float4 SelectedColor; // rgba
}

// Must match your C# instance (PointList)
struct VSIn
{
    float2 A : POSITION; // world: ellipse center
    float2 BBase : TEXCOORD0; // world: text base (UN-offset)
    uint LabelId : TEXCOORD1; // index into LabelSRV
    uint GroupId : TEXCOORD2; // index into GroupSRV
};

struct VSOut
{
    float4 aClip : TEXCOORD0; // clip-space A
    float4 bClip : TEXCOORD1; // clip-space BBase
    float2 aWorld : TEXCOORD2; // world A
    float2 bWorld : TEXCOORD3; // world BBase
    uint labelId : TEXCOORD4;
    uint groupId : TEXCOORD5;
    float4 pos : SV_POSITION; // dummy (IA = PointList)
};

// Match your C# SRV structs (packing/flags)
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

// Flags (same as your other passes)
static const uint LABEL_VISIBLE = 1u << 0;
static const uint LABEL_SELECTED = 1u << 1;
static const uint GROUP_VISIBLE = 1u << 0;

VSOut VSMain(VSIn v)
{
    VSOut o;
    o.aWorld = v.A;
    o.bWorld = v.BBase;
    o.aClip = mul(float4(v.A, 0, 1), ViewProj);
    o.bClip = mul(float4(v.BBase, 0, 1), ViewProj);
    o.labelId = v.LabelId;
    o.groupId = v.GroupId;
    o.pos = o.aClip; // not used; required output
    return o;
}

struct GSOut
{
    float4 pos : SV_POSITION;
    float4 col : COLOR0;
};

[maxvertexcount(4)]
void GSMain(point VSOut vin[1], inout TriangleStream<GSOut> tri)
{
    VSOut i = vin[0];

    // Look up state
    LabelState ls = LabelSRV[i.labelId];
    GroupState gs = GroupSRV[i.groupId];

    // Visibility (same rules as glyphs/circles)
    if (((ls.Flags & LABEL_VISIBLE) == 0u) || ((gs.Flags & GROUP_VISIBLE) == 0u))
        return;

    // Live endpoint B = BBase + label offset
    float2 aW = i.aWorld;
    float2 bW = i.bWorld + ls.Offset;

    // Project to CLIP & NDC
    float4 aC = mul(float4(aW, 0, 1), ViewProj);
    float4 bC = mul(float4(bW, 0, 1), ViewProj);
    float2 aN = aC.xy / aC.w;
    float2 bN = bC.xy / bC.w;

    // Direction in NDC
    float2 dir = bN - aN;
    float len = length(dir);
    if (len < 1e-6)
        return;
    dir /= len;

    // Perp in NDC; convert desired pixel thickness to NDC using InvViewport (NDC range = 2)
    float2 perpN = float2(-dir.y, dir.x);
    float2 pxToN = 2.0 * InvViewport;
    float2 offsN = perpN * (0.5 * PixelThickness) * pxToN;

    // Quad corners in NDC
    float2 aN0 = aN - offsN;
    float2 aN1 = aN + offsN;
    float2 bN0 = bN - offsN;
    float2 bN1 = bN + offsN;

    // Back to CLIP using original w,z for each end
    float4 vA0 = float4(aN0 * aC.w, aC.z, aC.w);
    float4 vA1 = float4(aN1 * aC.w, aC.z, aC.w);
    float4 vB0 = float4(bN0 * bC.w, bC.z, bC.w);
    float4 vB1 = float4(bN1 * bC.w, bC.z, bC.w);

    // Color from group; override if selected
    float4 col = gs.Color;
    if ((ls.Flags & LABEL_SELECTED) != 0u)
        col = SelectedColor;

    // Emit strip
    GSOut o;
    o.col = col;
    o.pos = vA0;
    tri.Append(o);
    o.pos = vA1;
    tri.Append(o);
    o.pos = vB0;
    tri.Append(o);
    o.pos = vB1;
    tri.Append(o);
    tri.RestartStrip();
}

float4 PSMain(GSOut i) : SV_Target
{
    return i.col; // simple solid; AA comes from rasterization of the quad
}
