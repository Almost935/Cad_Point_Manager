// PanShader.hlsl

cbuffer PanSettings : register(b0)
{
    float2 OffsetUv;
    float2 Padding;
};

Texture2D PanTexture : register(t0);
SamplerState PanSampler : register(s0);

struct VSInput
{
    float2 Position : POSITION;
    float2 TexCoord : TEXCOORD0;
};

struct PSInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

PSInput VSMain(VSInput input)
{
    PSInput output;

    output.Position = float4(input.Position, 0.0f, 1.0f);
    output.TexCoord = 0.25f + input.TexCoord * 0.5f + OffsetUv;

    return output;
}

float4 PSMain(PSInput input) : SV_TARGET
{
    return PanTexture.Sample(PanSampler, input.TexCoord);
}