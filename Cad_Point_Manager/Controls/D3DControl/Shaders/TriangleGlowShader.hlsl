// TriangleGlowShader.hlsl

cbuffer TransformationBuffer : register(b0)
{
    row_major matrix transformationMatrix;
};
cbuffer TriangleGlowSettingsBuffer : register(b1)
{
    float glowOffset;
    float glowTransparency;
    float padding;
    float4 selectedColor;
    float4 selectedMouseOverColor;
};

float4 GetSnappedColor(float4 color)
{
    float3 lightBlue = float3(0.4, 0.4, 1.0);
    float3 resultRgb = lerp(color.rgb, lightBlue, 0.7);

    return float4(resultRgb, color.a);
}

struct VSInput
{
    float3 Position : POSITION;
    float4 Color : COLOR;
    float IsVisible : TEXCOORD0;
    float IsMouseOver : TEXCOORD1;
    float IsSelected : TEXCOORD2;
};

struct GSInput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
};

VSInput VSMain(VSInput input)
{
    return input;
}

[maxvertexcount(3)]
void GSMain(triangle VSInput input[3], inout TriangleStream<GSInput> triStream)
{
    float halfGlowOffset = glowOffset / 2;
    float3 center = (input[0].Position + input[1].Position + input[2].Position) / 3.0;
    
    float4 color;
    if (input[0].IsSelected > 0.5)
    {
        if (input[0].IsMouseOver)
        {
            color = float4(selectedMouseOverColor.rgb, glowTransparency);
        }
        else
        {
            color = float4(selectedColor.rgb, glowTransparency);
        }
    }
    else
    {
        color = float4(GetSnappedColor(input[0].Color).rgb, glowTransparency);
    }
        
    for (int i = 0; i < 3; i++)
    {
        float3 dir = normalize(input[i].Position - center);
        float3 offset = dir * halfGlowOffset;

        GSInput output;
        output.Position = mul(float4(input[i].Position + offset, 1.0), transformationMatrix);
        output.Color = color;
        
        triStream.Append(output);
    }

    triStream.RestartStrip();
}

float4 PSMain(GSInput input) : SV_Target
{
    return input.Color;
}
