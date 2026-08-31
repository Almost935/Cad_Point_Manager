//-----------------------------------------------------------------------------
// LeaderLineShader.hlsl
//-----------------------------------------------------------------------------

cbuffer TransformationBuffer : register(b0)
{
    row_major matrix transformationMatrix;
};

cbuffer LeaderLineSettings : register(b1)
{
    float2 ViewportSize;
    float PixelThickness;
    float _pad0;
    float4 SelectedColor;
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
    nointerpolation uint PointId : TEXCOORD1;
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
static const uint POINT_SELECTED = 1u << 1;
static const uint POINT_HAS_LEADER = 1u << 3;

static const uint GROUP_VISIBLE = 1u << 0;

//-----------------------------------------------------------------------------
// Vertex shader
//-----------------------------------------------------------------------------

PSInput VSMain(VSInput vertex, VSInstance instance)
{
    PSInput output;

    PointState ps = PointSRV[instance.PointId];

    //--------------------------------------------
    // Live endpoints
    //--------------------------------------------

    float2 start = ps.Offset;
    float2 end = ps.Offset + ps.PointInfoOffset;

    //--------------------------------------------
    // Transform endpoints
    //--------------------------------------------

    float4 clipStart = mul(float4(start, 0.0, 1.0), transformationMatrix);
    float4 clipEnd = mul(float4(end, 0.0, 1.0), transformationMatrix);
    
    float2 ndcStart = clipStart.xy / clipStart.w;
    float2 ndcEnd = clipEnd.xy / clipEnd.w;

    //--------------------------------------------
    // Direction in PIXEL space
    //--------------------------------------------

    float2 pixelScale = float2(ViewportSize.x * 0.5, ViewportSize.y * 0.5);
    float2 dirPixels = (ndcEnd - ndcStart) * pixelScale;
    float lineLengthPixels = length(dirPixels);

    if (lineLengthPixels < 1e-6)
    {
        dirPixels = float2(1.0, 0.0);
    }
    else
    {
        dirPixels /= lineLengthPixels;
    }

    float2 normalPixels = float2(-dirPixels.y, dirPixels.x);

    //--------------------------------------------
    // Pixel normal -> NDC
    //--------------------------------------------

    float2 normalNdc =
        float2(normalPixels.x * (2.0 / ViewportSize.x), normalPixels.y * (2.0 / ViewportSize.y));

    //--------------------------------------------
    // Build quad
    //--------------------------------------------

    float t = vertex.Local.y;
    float2 ndc = lerp(ndcStart, ndcEnd, t);

    float halfWidthPixels = PixelThickness * 0.5;

    ndc += normalNdc * halfWidthPixels * vertex.Local.x;

    //--------------------------------------------
    // Back to clip space
    //--------------------------------------------

    float4 clip = lerp(clipStart, clipEnd, t);
    clip.xy = ndc * clip.w;
    output.Position = clip;
    output.Side = vertex.Local.x;
    output.PointId = instance.PointId;

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

    //--------------------------------------------
    // Color
    //--------------------------------------------

    float4 color = gs.Color;

    if ((ps.Flags & POINT_SELECTED) != 0u)
        color = SelectedColor;

    //--------------------------------------------
    // Analytic antialiasing
    //--------------------------------------------

    float d = abs(input.Side);
    float w = fwidth(d);

    float alpha = 1.0 - smoothstep(1.0 - w, 1.0 + w, d);
    color.a *= alpha;

    return color;
}