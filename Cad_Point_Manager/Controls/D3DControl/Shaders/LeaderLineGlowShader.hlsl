//-----------------------------------------------------------------------------
// LeaderLineGlowShader.hlsl
//-----------------------------------------------------------------------------

cbuffer TransformBuffer : register(b0)
{
    row_major float4x4 ViewProj;
};

cbuffer LeaderLineSettings : register(b1)
{
    float2 ViewportSize;
    float PixelThickness;
    float GlowPixelOffset;

    float4 HoverColor;
};

//-----------------------------------------------------------------------------
// Input
//-----------------------------------------------------------------------------

struct VSInput
{
    float2 Local : LOCAL;
};

struct VSInstance
{
    uint PointId : POINT_ID;
};

struct PSInput
{
    float4 Position : SV_POSITION;

    float Side : TEXCOORD0;
    float AlongPixels : TEXCOORD1;

    nointerpolation float LineLengthPixels : TEXCOORD2;
    nointerpolation uint PointId : TEXCOORD3;
};

//-----------------------------------------------------------------------------
// GPU state
//-----------------------------------------------------------------------------

struct PointState
{
    float2 Offset;
    float2 PointInfoOffset;

    uint GroupId;
    uint Flags;

    float2 _padLS;
};

struct GroupState
{
    float4 Color;

    float Scale;
    uint Flags;

    float TextInfoBaseXoffset;
    float _padGS;
};

StructuredBuffer<PointState> PointSRV : register(t0);
StructuredBuffer<GroupState> GroupSRV : register(t1);

static const uint POINT_VISIBLE = 1u << 0;
static const uint POINT_MOUSE_OVER = 1u << 2;
static const uint POINT_HAS_LEADER = 1u << 3;

static const uint GROUP_VISIBLE = 1u << 0;

//-----------------------------------------------------------------------------
// Vertex shader
//-----------------------------------------------------------------------------

PSInput VSMain(
    VSInput vertex,
    VSInstance instance)
{
    PSInput output;

    PointState ps =
        PointSRV[instance.PointId];

    //--------------------------------------------
    // Live endpoints
    //--------------------------------------------

    float2 start =
        ps.Offset;

    float2 end =
        ps.Offset +
        ps.PointInfoOffset;

    //--------------------------------------------
    // Transform
    //--------------------------------------------

    float4 clipStart =
        mul(
            float4(start, 0.0, 1.0),
            ViewProj);

    float4 clipEnd =
        mul(
            float4(end, 0.0, 1.0),
            ViewProj);

    float2 ndcStart =
        clipStart.xy / clipStart.w;

    float2 ndcEnd =
        clipEnd.xy / clipEnd.w;

    //--------------------------------------------
    // Pixel-space direction
    //--------------------------------------------

    float2 pixelScale =
        float2(
            ViewportSize.x * 0.5,
            ViewportSize.y * 0.5);

    float2 dirPixels =
        (ndcEnd - ndcStart) *
        pixelScale;

    float lineLengthPixels =
        length(dirPixels);

    if (lineLengthPixels < 1e-6)
    {
        dirPixels =
            float2(1.0, 0.0);
    }
    else
    {
        dirPixels /=
            lineLengthPixels;
    }

    float2 normalPixels =
        float2(
            -dirPixels.y,
             dirPixels.x);

    //--------------------------------------------
    // Pixel directions -> NDC
    //--------------------------------------------

    float2 normalNdc =
        float2(
            normalPixels.x *
                (2.0 / ViewportSize.x),

            normalPixels.y *
                (2.0 / ViewportSize.y));

    float2 directionNdc =
        float2(
            dirPixels.x *
                (2.0 / ViewportSize.x),

            dirPixels.y *
                (2.0 / ViewportSize.y));

    //--------------------------------------------
    // Dimensions
    //--------------------------------------------

    float visibleHalfWidth =
        PixelThickness * 0.5;

    float glowHalfWidth =
        visibleHalfWidth +
        GlowPixelOffset;

    //--------------------------------------------
    // Position along segment
    //--------------------------------------------

    float t =
        vertex.Local.y;

    float2 ndc =
        lerp(
            ndcStart,
            ndcEnd,
            t);

    //--------------------------------------------
    // Extend beyond both endpoints
    //--------------------------------------------

    float endDirection =
        vertex.Local.y * 2.0 - 1.0;

    ndc +=
        directionNdc *
        GlowPixelOffset *
        endDirection;

    //--------------------------------------------
    // Expand sideways
    //--------------------------------------------

    ndc +=
        normalNdc *
        glowHalfWidth *
        vertex.Local.x;

    //--------------------------------------------
    // Back to clip
    //--------------------------------------------

    float4 clip =
        lerp(
            clipStart,
            clipEnd,
            t);

    clip.xy =
        ndc * clip.w;

    output.Position =
        clip;

    output.Side =
        vertex.Local.x;

    output.AlongPixels =
        lerp(
            -GlowPixelOffset,
            lineLengthPixels +
                GlowPixelOffset,
            t);

    output.LineLengthPixels =
        lineLengthPixels;

    output.PointId =
        instance.PointId;

    return output;
}

//-----------------------------------------------------------------------------
// Pixel shader
//-----------------------------------------------------------------------------

float4 PSMain(PSInput input) : SV_TARGET
{
    PointState ps = PointSRV[input.PointId];
    GroupState gs = GroupSRV[ps.GroupId];

    //--------------------------------------------
    // Visibility
    //--------------------------------------------

    if ((gs.Flags & GROUP_VISIBLE) == 0u)
        discard;

    if ((ps.Flags & POINT_VISIBLE) == 0u)
        discard;

    if ((ps.Flags & POINT_HAS_LEADER) == 0u)
        discard;

    if ((ps.Flags & POINT_MOUSE_OVER) == 0u)
        discard;

    //--------------------------------------------
    // Dimensions
    //--------------------------------------------

    float visibleHalfWidth =
        PixelThickness * 0.5;

    float glowHalfWidth =
        visibleHalfWidth +
        GlowPixelOffset;

    //--------------------------------------------
    // Perpendicular distance
    //--------------------------------------------

    float perpendicularDistance =
        abs(input.Side) *
        glowHalfWidth;

    //--------------------------------------------
    // Distance beyond physical endpoints
    //--------------------------------------------

    float alongDistance =
        0.0;

    if (input.AlongPixels < 0.0)
    {
        alongDistance =
            -input.AlongPixels;
    }
    else if (
        input.AlongPixels >
        input.LineLengthPixels)
    {
        alongDistance =
            input.AlongPixels -
            input.LineLengthPixels;
    }

    //--------------------------------------------
    // Distance from centerline
    //--------------------------------------------

    float centerlineDistance =
        length(
            float2(
                perpendicularDistance,
                alongDistance));

    //--------------------------------------------
    // Don't draw over visible leader itself
    //--------------------------------------------

    if (centerlineDistance <=
        visibleHalfWidth)
    {
        discard;
    }

    //--------------------------------------------
    // Distance outside visible stroke
    //--------------------------------------------

    float glowDistance =
        centerlineDistance -
        visibleHalfWidth;

    if (glowDistance >=
        GlowPixelOffset)
    {
        discard;
    }

    //--------------------------------------------
    // Glow falloff
    //--------------------------------------------

    float glowT = saturate(glowDistance / GlowPixelOffset);
    float glowAlpha = 1.0 - smoothstep(0.0, 1.0, glowT);

    const float MaxGlowAlpha = 0.45;

    glowAlpha *= MaxGlowAlpha;

    return float4(0.0, 0.0, 0.0, glowAlpha);
}