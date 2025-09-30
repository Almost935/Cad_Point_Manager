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

// Input structure for the Vertex Shader
struct VSInput
{
    float3 Position : POSITION; // 3D position of the vertex
    uint LayerId : LAYERID; // Layer index for indirection
    float IsMouseOver : ISMOUSEOVER;
    float IsSelected : ISSELECTED; // Indicates if the vertex is selected (0 or 1)
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
    uint Flags;
    float3 Pad;
};

StructuredBuffer<LayerState> LayerStates : register(t0);

static const uint LAYER_VISIBLE = 1u << 0;


// Vertex Shader: Transforms input vertex and passes color through
PSInput VSMain(VSInput input)
{
    PSInput output;

    LayerState ls = LayerStates[input.LayerId];
    float visLayer = ((ls.Flags & LAYER_VISIBLE) != 0u) ? 1.0f : 0.0f;
    if (!visLayer)
    {
        output.Color = float4(0, 0, 0, 0);
        return output;
    }
    
    output.Position = mul(float4(input.Position, 1.0), transformationMatrix);
    
    output.Color = ls.Color;    
    if (input.IsSelected > 0.5)
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
