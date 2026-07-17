cbuffer TransformationBuffer : register(b0)
{
    row_major float4x4 transformationMatrix;
}

cbuffer TextSettingsBuffer : register(b1)
{
    float4 selectedColor;
}

cbuffer ViewportBuffer : register(b2)
{
    float2 ViewportSize;
    float2 _viewportPad;
}

cbuffer MsdfSettings : register(b3)
{
    float AtlasWidth;
    float AtlasHeight;
    float DistanceRange;
    float Padding;
}

struct LabelState
{
    float2 Offset;
    uint Flags;
    float _pad;
};

struct PointState
{
    float2 Offset;
    float2 PointInfoOffset;
    uint GroupId;
    uint Flags;
    float2 _pad;
};

struct GroupState
{
    float4 Color;
    float Scale;
    uint Flags;
    float TextInfoBaseXoffset;
    float _pad;
};

StructuredBuffer<LabelState> LabelStates : register(t0);
StructuredBuffer<PointState> PointStates : register(t1);
StructuredBuffer<GroupState> GroupStates : register(t2);

Texture2D FontAtlas : register(t3);

SamplerState FontSampler : register(s0);

struct VSVertex
{
    float2 Corner : POSITION;
};

struct VSInstance
{
    float EmToWorld : EM_TO_WORLD;
    float PenX : PEN_X;
    float YSign : YSIGN;
    uint LabelId : LABEL_ID;
    uint PointId : POINT_ID;
    float2 PlaneOrigin : PLANE_ORIGIN;
    float2 PlaneSize : PLANE_SIZE;
    float2 UvOrigin : UV_ORIGIN;
    float2 UvSize : UV_SIZE;
};

struct VSOut
{
    float4 Position : SV_POSITION;
    float2 UV : TEXCOORD0;
    float4 Color : COLOR;
    float Visible : TEXCOORD1;
};

float Median(float r, float g, float b)
{
    return max(min(r, g), min(max(r, g), b));
}
float ScreenPxRange(float2 uv)
{
    float2 texelSize = float2(AtlasWidth, AtlasHeight);

    float2 dx = ddx(uv * texelSize);
    float2 dy = ddy(uv * texelSize);

    float deriv = max(length(dx), length(dy));

    return DistanceRange / deriv;
}

// Bit masks for flags (keep in sync with CPU)
static const uint LABEL_VISIBLE = 1u << 0;

static const uint POINT_VISIBLE = 1u << 0;
static const uint POINT_SELECTED = 1u << 1;
static const uint POINT_MOUSEOVR = 1u << 2;
static const uint POINT_ISFLIPPEDY = 1u << 6;
static const uint POINT_ISFLIPPEDX = 1u << 7;

static const uint GROUP_VISIBLE = 1u << 0;


VSOut VSMain(VSVertex v, VSInstance inst)
{
    VSOut o;
    
    LabelState ls = LabelStates[inst.LabelId];
    PointState ps = PointStates[inst.PointId];
    GroupState gs = GroupStates[ps.GroupId];
    
    float visLbl = ((ls.Flags & LABEL_VISIBLE) != 0u) ? 1.0f : 0.0f;
    float visPt = ((ps.Flags & POINT_VISIBLE) != 0u) ? 1.0f : 0.0f;
    float visGrp = ((gs.Flags & GROUP_VISIBLE) != 0u) ? 1.0f : 0.0f;
    float visible = visLbl * visGrp * visPt;
    
    if (visible < 0.5f)
    {
        o.Position = float4(0, 0, 0, 0);
        o.UV = 0;
        o.Color = 0;
        o.Visible = 0;
        return o;
    }
    
    o.Visible = visible;
    
     // Selection / hover
    float sel = ((ps.Flags & POINT_SELECTED) != 0u) ? 1.0f : 0.0f;
    float mo = ((ps.Flags & POINT_MOUSEOVR) != 0u) ? 1.0f : 0.0f;
    
    // Flipped axis
    float isFlippedY = ((ps.Flags & POINT_ISFLIPPEDY) != 0u) ? -1.0f : 1.0f;

    float2 corner = v.Corner + 0.5;

    float2 local = inst.PlaneOrigin + corner * inst.PlaneSize;
    local.x += inst.PenX;
    local.y *= inst.YSign;
    
    local *= (inst.EmToWorld * gs.Scale);
    
    float textInfoOffset = gs.TextInfoBaseXoffset * isFlippedY;
    float2 origin;
    origin.x = ps.Offset.x + ps.PointInfoOffset.x + ls.Offset.x + textInfoOffset;
    origin.y = ps.Offset.y + ps.PointInfoOffset.y + ls.Offset.y * gs.Scale;
    
    local += origin;

    o.Position = mul(float4(local, 0, 1), transformationMatrix);

    o.UV = lerp(inst.UvOrigin, inst.UvOrigin + inst.UvSize, corner);

    o.Color = gs.Color;
    
    if (sel > 0.5f)
    {
        o.Color = selectedColor;
    }

    return o;
}

float4 PSMain(VSOut input) : SV_Target
{
    if (input.Visible < 0.5f)
    {
        clip(-1);
    }
    
    float3 msd = FontAtlas.Sample(FontSampler, input.UV).rgb;

    float sd = Median(msd.r, msd.g, msd.b);

    float screenPxDistance = ScreenPxRange(input.UV) * (sd - 0.5);

    float opacity = saturate(screenPxDistance + 0.5);

    return float4(input.Color.rgb, opacity);
}