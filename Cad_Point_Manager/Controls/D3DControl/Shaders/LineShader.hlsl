
// LineShader.hlsl

// Constant buffer for 2D transformation matrix
cbuffer TransformationBuffer : register(b0)
{
    row_major matrix transformationMatrix; // 2D transformation matrix
};

cbuffer LineSettingsBuffer : register(b1)
{
    float4 selectedColor;
    float4 selectedMouseOverColor;
};

cbuffer LineRenderModeBuffer : register(b2)
{
    uint RenderSelectedOnly;
    uint RenderGlowPass;
    float2 Padding;
}

// Input structure for the Vertex Shader
struct VSInput
{
    float3 Position : POSITION; // 3D position of the vertex
    uint LayerId : LAYERID; // Layer index for indirection
    uint ObjectId : OBJECTID; // Object index for indirection
};

// Output structure from the Vertex Shader and input for the Pixel Shader
struct PSInput
{
    float4 Position : SV_POSITION; // Transformed position in screen space
    float4 Color : COLOR; // RGBA color passed to the Pixel Shader
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

// Vertex Shader: Transforms input vertex and passes color through
PSInput VSMain(VSInput input)
{
    PSInput output;

    LayerState ls = LayerStates[input.LayerId];
    ObjectState os = ObjectStates[input.ObjectId];
    
    float visLayer = ((ls.Flags & LAYER_VISIBLE) != 0u) ? 1.0f : 0.0f;
    float visObject = ((os.Flags & OBJ_VISIBLE) != 0u) ? 1.0f : 0.0f;
    float colorByLayer = ((os.Flags & OBJ_COLOR_BY_LAYER) != 0u) ? 1.0f : 0.0f;

    if (!visLayer || !visObject)
    {
        output.Color = float4(0, 0, 0, 0);
        return output;
    }
    
    // Selection / hover
    float sel = ((os.Flags & OBJ_SELECTED) != 0u) ? 1.0f : 0.0f;
    float mo = ((os.Flags & OBJ_MOUSEOVER) != 0u) ? 1.0f : 0.0f;
    
    if (RenderSelectedOnly == 1u)
    {
        if (sel < 0.5f)
        {
            output.Position = float4(0, 0, 0, 0);
            output.Color = float4(0, 0, 0, 0);
            return output;
        }
    }
    else
    {
        if (sel > 0.5f)
        {
            output.Position = float4(0, 0, 0, 0);
            output.Color = float4(0, 0, 0, 0);
            return output;
        }
    }
    
    output.Position = mul(float4(input.Position, 1.0), transformationMatrix);
    output.Color = ls.Color;
    
    if (colorByLayer < 0.5)
    {
        output.Color = os.Color;
    }
    else
    {
        output.Color = ls.Color;
    }
    
    if (sel > 0.5)
    {
        output.Color = selectedColor;
    }

    return output;
}

// Pixel Shader: Determines the color of each pixel
float4 PSMain(PSInput input) : SV_TARGET
{
    return input.Color;
}
