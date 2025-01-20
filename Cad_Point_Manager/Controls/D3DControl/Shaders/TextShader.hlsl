// TextShader.hlsl

// Vertex Shader for Text
cbuffer TransformationBuffer : register(b0)
{
    matrix WorldViewProjection;
};

struct VSInput
{
    float3 Position : POSITION;
    float4 Color : COLOR;
    float2 TexCoord : TEXCOORD;
    float IsVisible : ISVISIBLE; // IsVisible flag for text
};

struct VSOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
    float2 TexCoord : TEXCOORD;
    float IsVisible : ISVISIBLE;
};

VSOutput VSMain(VSInput input)
{
    VSOutput output;
    output.Position = mul(float4(input.Position, 1.0f), WorldViewProjection);
    output.Color = input.Color;
    output.TexCoord = input.TexCoord;
    output.IsVisible = input.IsVisible;
    return output;
}

// Pixel Shader for Text
Texture2D fontTexture : register(t0);
SamplerState samplerState : register(s0);

struct PSInput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
    float2 TexCoord : TEXCOORD;
    float IsVisible : ISVISIBLE;
};

float4 PSMain(PSInput input) : SV_Target
{
    if (input.IsVisible < 0.5f)
    {
        discard;
    }
    float4 texColor = fontTexture.Sample(samplerState, input.TexCoord);
    return texColor * input.Color;
}


