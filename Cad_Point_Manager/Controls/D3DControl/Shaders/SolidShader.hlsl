// SolidShader.hlsl

cbuffer TransformationBuffer : register(b0)
{
    row_major matrix transformationMatrix; // 2D transformation matrix
};

cbuffer SolidSettingsBuffer : register(b1)
{
    float4 selectedColor;
    float4 selectedMouseOverColor;
};

struct VSInput
{
    float3 Position : POSITION;
    uint LayerId : LAYERID; // Layer index for indirection
    uint ObjectId : OBJECTID; // Object index for indirection
};

struct PSInput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
};

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

float4 GetObjectColor(VSInput input)
{
    float4 col;

    LayerState ls = LayerStates[input.LayerId];
    ObjectState os = ObjectStates[input.ObjectId];
    
    float visLayer = ((ls.Flags & LAYER_VISIBLE) != 0u) ? 1.0f : 0.0f;
    float visObject = ((os.Flags & OBJ_VISIBLE) != 0u) ? 1.0f : 0.0f;
    float colorByLayer = ((os.Flags & OBJ_COLOR_BY_LAYER) != 0u) ? 1.0f : 0.0f;
    
    if (colorByLayer < 0.5)
    {
        col = os.Color;
    }
    else
    {
        col = ls.Color;
    }

    //float sel = ((os.Flags & OBJ_SELECTED) != 0u) ? 1.0f : 0.0f;
    //float mo = ((os.Flags & OBJ_MOUSEOVER) != 0u) ? 1.0f : 0.0f;
    
    //if (sel > 0.5)
    //{
    //    col = (mo > 0.5) ? selectedMouseOverColor : selectedColor;
    //}
    
    if (!visLayer)
    {
        col.a = 0.0;
    }
    
    return col;
}

PSInput VSMain(VSInput input)
{
    PSInput o;

    float4 clip = mul(float4(input.Position, 1.0), transformationMatrix);
    
    float4 col = GetObjectColor(input);

    o.Position = clip;
    o.Color = col;
    return o;
}

float4 PSMain(PSInput input) : SV_TARGET
{
    return input.Color;
}