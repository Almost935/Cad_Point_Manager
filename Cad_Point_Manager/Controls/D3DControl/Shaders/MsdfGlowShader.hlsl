cbuffer TransformationBuffer : register(b0)
{
    row_major float4x4 transformationMatrix;
}

cbuffer DrawingSettingsBuffer : register(b1)
{
    float2 ViewportSize;
    float2 _pad1;

    float LineHalfWidthPixels;
    float GlobalLineTypeScale;
    float AnnotationScale;
    float GlowPixelOffset;

    float4 SelectedColor;
    float4 SelectedMouseOverColor;
};

cbuffer MsdfSettings : register(b2)
{
    float AtlasWidth;
    float AtlasHeight;
    float DistanceRange;
    float CameraZoom;
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
    nointerpolation float Visible : TEXCOORD1;
    nointerpolation uint PointFlags : TEXCOORD2;
};

float Median(float r, float g, float b)
{
    return max(min(r, g), min(max(r, g), b));
}

float4 ScreenPxRange(float2 uv)
{
    float2 fw = fwidth(uv);
    return float4(fw.x * 1000, fw.y * 1000, 0, 1);
}

// Bit masks for flags (keep in sync with CPU)
static const uint LABEL_VISIBLE = 1u << 0;

static const uint POINT_VISIBLE = 1u << 0;
static const uint POINT_SELECTED = 1u << 1;
static const uint POINT_MOUSEOVER = 1u << 2;
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
    o.PointFlags = ps.Flags;

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

    return o;
}

float4 PSMain(VSOut input) : SV_Target
{
    if (input.Visible < 0.5f ||
        (input.PointFlags & POINT_MOUSEOVER) == 0u)
    {
        clip(-1);
    }

    float3 msd = FontAtlas.Sample(FontSampler, input.UV).rgb;
    float sd = Median(msd.r, msd.g, msd.b);
    float d = (sd - 0.5f) * DistanceRange;

    //---------------------------------------
    // Outside glow
    //---------------------------------------

    float glowRadius = clamp(120.0f / CameraZoom, 0.01f, DistanceRange * 0.5f);

    // Positive distance going OUTWARD from the glyph boundary.
    float outsideDistance = max(-d, 0.0f);

    // 1 at glyph boundary -> 0 at glowRadius.
    float halo = 1.0f - smoothstep(0.0f, glowRadius, outsideDistance);

    // Keep this contribution outside the glyph.
    float outsideMask = 1.0f - smoothstep(-0.25f, 0.25f, d);
    halo *= outsideMask;

    //---------------------------------------
    // Interior
    //---------------------------------------

    float fill = smoothstep(-0.25f, 0.5f, d);

    //---------------------------------------
    // Combine
    //---------------------------------------

    float alpha = halo * 0.55f + fill * 0.20f;
    alpha = saturate(alpha);

    return float4(0, 0, 0, alpha);
}