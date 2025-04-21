// TextGlowShader.hlsl

cbuffer TransformationBuffer : register(b0)
{
    row_major matrix transformationMatrix;
};
cbuffer TextGlowSettingsBuffer : register(b1)
{
    float glowOffset;
    float glowTransparency;
    float glowZoomFactor;
    float padding;
};

float4 GetSnappedColor(float4 color)
{
    float3 snappedColor = lerp(color.rgb, float3(0.4, 0.4, 0.7), color.a);
    return float4(snappedColor, color.a);
}

struct VSInput
{
    float3 Position : POSITION;
    float4 Color : COLOR;
    float IsVisible : ISVISIBLE;
    float IsMouseOver : ISMOUSEOVER;
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
    //if (input[0].IsMouseOver == 0 && input[1].IsMouseOver == 0 && input[2].IsMouseOver == 0)
    //{
    //    return;
    //}

    float halfGlowOffset = glowOffset / 2;
    float3 center = (input[0].Position + input[1].Position + input[2].Position) / 3.0;

    for (int i = 0; i < 3; i++)
    {
        float3 dir = normalize(input[i].Position - center);
        float3 offset = dir * halfGlowOffset; // glow expansion

        GSInput output;
        output.Position = mul(float4(input[i].Position + offset, 1.0), transformationMatrix);
        output.Color = float4(input[i].Color.rgb, glowTransparency);
        
        if (input[i].IsMouseOver > 0.5) 
        {
            output.Color = GetSnappedColor(input[i].Color);
            output.Color.a = 0;
        }
        
        triStream.Append(output);
    }

    triStream.RestartStrip();
}

float4 PSMain(GSInput input) : SV_Target
{
    return input.Color;
}
