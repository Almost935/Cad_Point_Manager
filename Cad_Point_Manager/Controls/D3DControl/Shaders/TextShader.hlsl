// TextShader.hlsl

cbuffer TransformationBuffer : register(b0)
{
    row_major matrix transformationMatrix; // world*view*proj (to CLIP space)
};

cbuffer TextSettingsBuffer : register(b1)
{
    float4 selectedColor;
    float4 selectedMouseOverColor;
};

// NEW: viewport size in pixels (width,height)
cbuffer ViewportBuffer : register(b2)
{
    float2 ViewportSize; // e.g., {renderTargetWidth, renderTargetHeight}
    float2 _padViewport;
}

float4 GetSnappedColor(float4 color)
{
    float3 lightBlue = float3(0.4, 0.4, 1.0);
    float3 resultRgb = lerp(color.rgb, lightBlue, 0.7);
    return float4(resultRgb, color.a);
}

// Input / Output
struct VSInput
{
    float3 Position : POSITION;
    uint LayerId : LAYERID; // Layer index for indirection
    uint ObjectId : OBJECTID; // Object index for indirection
    float IsMouseOver : ISMOUSEOVER;
    float IsSelected : ISSELECTED;
};

struct PSInput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
};

struct LayerState
{
    float4 Color;
    uint Flags; // bit0: visible, bit1: selected, bit2: mouseOver
    float3 Pad;
};
struct ObjectState
{
    uint Flags; // bit0: visible, bit1: selected, bit2: mouseOver, bit3: colorByLayer
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

// Replace per-vertex snapping with a uniform NDC delta computed once
float2 ComputeSnapDeltaNdc(float2 viewportSize)
{
    float4 originClip = mul(float4(0, 0, 0, 1), transformationMatrix);
    float2 originNdc = originClip.xy / originClip.w;
    float2 originPix = (originNdc * 0.5f + 0.5f) * viewportSize;

    float2 targetPix = floor(originPix) + 0.5f;
    float2 deltaPix = targetPix - originPix;

    return (deltaPix / viewportSize) * 2.0f;
}

PSInput VSMain(VSInput input)
{
    PSInput o;

    float4 clip = mul(float4(input.Position, 1.0), transformationMatrix);
    
    LayerState ls = LayerStates[input.LayerId];
    ObjectState os = ObjectStates[input.ObjectId];
    
    float visLayer = ((ls.Flags & LAYER_VISIBLE) != 0u) ? 1.0f : 0.0f;
    float visObject = ((os.Flags & OBJ_VISIBLE) != 0u) ? 1.0f : 0.0f;
    float colorByLayer = ((os.Flags & OBJ_COLOR_BY_LAYER) != 0u) ? 1.0f : 0.0f;

    float4 col;
    if (colorByLayer < 0.5)
    {
        col = os.Color;
    }
    else
    {
        col = ls.Color;
    }

    if (input.IsSelected > 0.5)
        col = (input.IsMouseOver > 0.5) ? selectedMouseOverColor : selectedColor;
    if (!visLayer)
        col.a = 0.0;

    // --- uniform snap (same offset for all vertices in this draw) ---
    float2 snapNdc = ComputeSnapDeltaNdc(ViewportSize);
    clip.xy += snapNdc * clip.w;

    o.Position = clip;
    o.Color = col;
    return o;
}


float4 PSMain(PSInput i) : SV_TARGET
{
    // Optional softening; you can remove if you’re already premultiplied
    float edgeFade = smoothstep(0.0, 0.1, i.Color.a);
    return float4(i.Color.rgb, i.Color.a * edgeFade);
}
