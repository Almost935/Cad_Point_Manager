//-----------------------------------------------------------------------------
// LineShader.hlsl
//-----------------------------------------------------------------------------

cbuffer TransformationBuffer : register(b0)
{
    row_major matrix transformationMatrix;
};

cbuffer LineSettingsBuffer : register(b1)
{
    float4 SelectedColor;
    float4 SelectedMouseOverColor;
    float HalfWidth;
    float3 _padding1;
};

cbuffer LineRenderModeBuffer : register(b2)
{
    uint RenderSelectedOnly;
    uint RenderGlowPass;
    float2 _padding2;
};

cbuffer ViewportBuffer : register(b3)
{
    float2 ViewportSize;
    float2 _padding3;
};

//-----------------------------------------------------------------------------
// Vertex input
//-----------------------------------------------------------------------------

struct VSInput
{
    float2 Local : LOCAL;
};

struct VSInstance
{
    float2 Start : START;
    float2 End : END;

    uint LayerId : LAYERID;
    uint ObjectId : OBJECTID;
};

//-----------------------------------------------------------------------------
// Pixel input
//-----------------------------------------------------------------------------

struct PSInput
{
    float4 Position : SV_POSITION;

    float Side : TEXCOORD0;
    float Along : TEXCOORD1;

    nointerpolation uint LayerId : TEXCOORD2;
    nointerpolation uint ObjectId : TEXCOORD3;
};

//-----------------------------------------------------------------------------
// GPU state
//-----------------------------------------------------------------------------

struct LayerState
{
    float4 Color;
    uint Flags;
    float3 Pad;
};

struct ObjectState
{
    uint Flags;
    float3 Pad;
    float4 Color;
};

StructuredBuffer<LayerState> LayerStates : register(t0);
StructuredBuffer<ObjectState> ObjectStates : register(t1);

static const uint LAYER_VISIBLE = 1u << 0;

static const uint OBJ_VISIBLE = 1u << 0;
static const uint OBJ_SELECTED = 1u << 1;
static const uint OBJ_MOUSEOVER = 1u << 2;
static const uint OBJ_COLOR_BY_LAYER = 1u << 3;

//-----------------------------------------------------------------------------
// Vertex shader
//-----------------------------------------------------------------------------

PSInput VSMain(VSInput vertex, VSInstance instance)
{
    PSInput output;

    //--------------------------------------------
    // Transform endpoints to clip space
    //--------------------------------------------

    float4 clipStart =
        mul(float4(instance.Start, 0, 1), transformationMatrix);

    float4 clipEnd =
        mul(float4(instance.End, 0, 1), transformationMatrix);

    //--------------------------------------------
    // Convert to NDC
    //--------------------------------------------

    float2 ndcStart =
        clipStart.xy / clipStart.w;

    float2 ndcEnd =
        clipEnd.xy / clipEnd.w;

    //--------------------------------------------
    // Screen-space direction
    //--------------------------------------------

    float2 delta =
        ndcEnd - ndcStart;

    float len = length(delta);

    if (len < 1e-6)
    {
        delta = float2(1, 0);
        len = 1;
    }

    float2 pixelScale =
    float2(
        ViewportSize.x * 0.5,
        ViewportSize.y * 0.5);

// Convert NDC direction into pixel direction
    float2 dirPixels =
    (ndcEnd - ndcStart) * pixelScale;

    dirPixels = normalize(dirPixels);

    float2 normalPixels =
    float2(-dirPixels.y, dirPixels.x);

// Convert back to NDC
    float2 offset =
    float2(
        normalPixels.x * (2.0 / ViewportSize.x),
        normalPixels.y * (2.0 / ViewportSize.y));

    offset *= HalfWidth;

    //--------------------------------------------
    // Position along the segment
    //--------------------------------------------

    float t = vertex.Local.y;

    float2 ndc =
        lerp(
            ndcStart,
            ndcEnd,
            t);

    //ndc +=
    //    offset *
    //    vertex.Local.x;
    
    ndc += offset * (vertex.Local.x * 2.0);

    //--------------------------------------------
    // Convert back to clip coordinates
    //--------------------------------------------

    float4 clip =
        lerp(
            clipStart,
            clipEnd,
            t);

    clip.xy =
        ndc *
        clip.w;

    //--------------------------------------------

    output.Position = clip;

    output.Side = vertex.Local.x;

    output.Along = t;

    output.LayerId = instance.LayerId;

    output.ObjectId = instance.ObjectId;

    return output;
}

//-----------------------------------------------------------------------------
// Pixel shader
//-----------------------------------------------------------------------------

float4 PSMain(PSInput input) : SV_TARGET
{
    LayerState ls = LayerStates[input.LayerId];
    ObjectState os = ObjectStates[input.ObjectId];

    if ((ls.Flags & LAYER_VISIBLE) == 0)
        discard;

    if ((os.Flags & OBJ_VISIBLE) == 0)
        discard;

    bool selected = (os.Flags & OBJ_SELECTED) != 0;

    if (RenderSelectedOnly == 1)
    {
        if (!selected)
            discard;
    }
    else
    {
        if (selected)
            discard;
    }

    float4 color = ((os.Flags & OBJ_COLOR_BY_LAYER) != 0) ? ls.Color : os.Color;

    if (selected)
        color = SelectedColor;

    float d = abs(input.Side);

    float w = fwidth(d);

    //float alpha = 1.0 - smoothstep(1.0 - w, 1.0 + w, d);
    
    float visibleSide = abs(input.Side) * 2.0;

    float alpha = 1.0 - smoothstep(1.0 - w, 1.0 + w, visibleSide);

    color.a *= alpha;
    
    return color;
}