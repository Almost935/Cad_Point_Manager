Texture2D GlowTexture : register(t0);
SamplerState GlowSampler : register(s0);

struct VSInput
{
    float2 Position : POSITION;
    float2 TexCoord : TEXCOORD;
};

struct PSInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD;
};

PSInput VSMain(VSInput input)
{
    PSInput output;

    output.Position = float4(input.Position, 0.0, 1.0);
    output.TexCoord = input.TexCoord;

    return output;
}

float4 PSMain(PSInput input) : SV_TARGET
{
    return GlowTexture.Sample(GlowSampler, input.TexCoord);
}