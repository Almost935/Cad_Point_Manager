// ToggleAnchor.hlsl

cbuffer TransformBuffer : register(b0)
{
    row_major float4x4 ViewProj;
}

cbuffer ToggleAnchorSettingsBuffer : register(b1)
{
    float4 baseColor;
    float4 selectedColor;
    float4 mouseOverColor;
    float size;
    float cornerRadius;
    float feather;
}

// Stream 0: unit quad
struct VSQuadIn
{
    float2 local : POSITION;
};

// Stream 1: per-instance data  (FIXED: unique TEXCOORD slots)
struct VSInst
{
    float2 center : TEXCOORD0; // world center
    uint pointId : POINT_ID; // index into PointStates
    uint groupId : GROUP_ID; // index into GroupStates
};

struct VSOut
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float2 size : TEXCOORD1;
    float2 rf : TEXCOORD2;
    uint state : TEXCOORD3; // 0=normal, 1=mouseOver, 2=selected
    float show : TEXCOORD4; // 1 = draw, 0 = cull
};

struct PointState
{
    float2 Offset; // world-space drag delta
    float LeaderLineAngle; // degrees
    uint Flags; // bit0: visible, bit1: selected, bit2: mouseOver, bit3: hasLeaderLine, bit4: mouseOverAnchor, bit5: anchorPressed
    float2 _padLS; // keep 16B stride
};
struct GroupState
{
    float4 Color;
    float Scale;
    uint Flags; // bit0: visible
    float2 _padGS;
};

StructuredBuffer<PointState> PointStates : register(t0);
StructuredBuffer<GroupState> GroupStates : register(t1);

static const uint POINT_VISIBLE = 1u << 0;
static const uint POINT_SELECTED = 1u << 1;
static const uint POINT_MOUSEOVR = 1u << 2;
static const uint POINT_MOUSEOVERANCHOR = 1u << 4;
static const uint POINT_ANCHORPRESSED = 1u << 5;

static const uint GROUP_VISIBLE = 1u << 0;

VSOut VSMain(VSQuadIn v, VSInst i)
{
    VSOut o;

    // Fetch dynamic state
    PointState ps = PointStates[i.pointId];
    GroupState gs = GroupStates[i.groupId];
    
    // Visibility + selection gates
    const float visPt = ((ps.Flags & POINT_VISIBLE) != 0u) ? 1.0f : 0.0f;
    const float visGrp = ((gs.Flags & GROUP_VISIBLE) != 0u) ? 1.0f : 0.0f;
    const float sel = ((ps.Flags & POINT_SELECTED) != 0u) ? 1.0f : 0.0f;
    const float mouseOver = ((ps.Flags & POINT_MOUSEOVERANCHOR) != 0u) ? 1.0f : 0.0f;
    const float anchorPressed = ((ps.Flags & POINT_ANCHORPRESSED) != 0u) ? 1.0f : 0.0f;

    uint state = 0; // normal
    if (mouseOver > 0.5f)
        state = 1; // mouseOver
    if (anchorPressed > 0.5f)
        state = 2; // selected
    
    // Show only when visible
    o.show = visPt * visGrp * sel;

    // Build world position as usual
    const float2 worldPos = i.center + ps.Offset + v.local * size;

    o.pos = mul(float4(worldPos, 0.0, 1.0), ViewProj);
    o.uv = v.local * size;
    o.size = size;
    o.rf = cornerRadius;
    o.state = state;

    return o;
}

float4 PSMain(VSOut i) : SV_Target
{
    // Early kill when not to be shown
    if (i.show < 0.5f)
        discard;

    // Rounded-rect SDF
    float2 q = abs(i.uv) - (i.size - i.rf.x);
    float d = length(max(q, 0.0)) - i.rf.x;

    float aa = max(fwidth(d), 1e-6);
    float fillAlpha = smoothstep(0.5 * aa, -0.5 * aa, d);

    // Simple state-based color (you can refine)
    float4 col = baseColor;
    if (i.state == 1)
        col = mouseOverColor;
    if (i.state == 2)
        col = selectedColor;

    // Uniform 1px border
    const float BorderPx = 1.0;
    float outer = smoothstep(0.5 * aa, -0.5 * aa, d);
    float inner = smoothstep(0.5 * aa, -0.5 * aa, d + BorderPx * aa);
    float borderMask = saturate(outer - inner);
    float4 borderCol = float4(0, 0, 0, 0.3);
    float4 outCol = lerp(col, borderCol, borderMask);

    outCol.a *= fillAlpha;
    if (outCol.a <= 0.001)
        discard;

    return outCol;
}
