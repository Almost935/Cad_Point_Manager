// LineGlowShader.hlsl

cbuffer TransformationBuffer : register(b0)
{
    row_major matrix transformationMatrix;
};
cbuffer LineGlowSettingsBuffer : register(b1)
{
    float glowOffset;
    float glowTransparency;
    float2 padding;
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

struct GSInput
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

StructuredBuffer<LayerState> LayerStates : register(t0);

static const uint LAYER_VISIBLE = 1u << 0;

VSInput VSMain(VSInput input)
{
    return input;
}

[maxvertexcount(6)]
void GSMain(line VSInput input[2], inout TriangleStream<GSInput> triStream)
{
    LayerState ls = LayerStates[input[0].LayerId];
    float visLayer = ((ls.Flags & LAYER_VISIBLE) != 0u) ? 1.0f : 0.0f;
    const bool isSelected = (input[0].IsSelected > 0.5) || (input[1].IsSelected > 0.5);
    const bool isMouseOver = (input[0].IsMouseOver > 0.5) || (input[1].IsMouseOver > 0.5);

    if (!visLayer || !isMouseOver)
    {
        return;
    }

    float halfGlowOffset = glowOffset / 2;
    
    float2 dir = normalize(input[1].Position.xy - input[0].Position.xy);
    float2 normal = float2(-dir.y, dir.x);
    
    // Extend the line endpoints
    float3 start = input[0].Position - float3(dir * halfGlowOffset, 0.0f);
    float3 end = input[1].Position + float3(dir * halfGlowOffset, 0.0f);

    float3 offset = float3(normal * halfGlowOffset, 0.0f);

    float3 p0 = start + offset;
    float3 p1 = end + offset;
    float3 p2 = end - offset;
    float3 p3 = start - offset;

    float4 color;
    if (input[0].IsSelected > 0.5)
    {
        color = float4(selectedMouseOverColor.rgb, 1);
    }
    else
    {
        color = float4(0, 0, 0, glowTransparency);
    }

    GSInput out0 = { mul(float4(p0, 1.0), transformationMatrix), color };
    GSInput out1 = { mul(float4(p1, 1.0), transformationMatrix), color };
    GSInput out2 = { mul(float4(p2, 1.0), transformationMatrix), color };
    GSInput out3 = { mul(float4(p3, 1.0), transformationMatrix), color };

    // First triangle (p0, p1, p2)
    triStream.Append(out0);
    triStream.Append(out1);
    triStream.Append(out2);
    triStream.RestartStrip();

    // Second triangle (p2, p3, p0)
    triStream.Append(out2);
    triStream.Append(out3);
    triStream.Append(out0);
    triStream.RestartStrip();
}

float4 PSMain(GSInput input) : SV_Target
{
    return input.Color;
}
