// PanShader.hlsl

cbuffer PanSettings : register(b0)
{
    float2 OffsetUv;
    float PanCacheFactorX;
    float PanCacheFactorY;
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

    float2 cacheFactor = float2(PanCacheFactorX, PanCacheFactorY);
    float2 visibleUvSize = 1.0f / cacheFactor;
    float2 centeredUvOrigin = (1.0f - visibleUvSize) * 0.5f;

    output.TexCoord =
    centeredUvOrigin + input.TexCoord * visibleUvSize + OffsetUv;

    return output;
}

float4 PSMain(PSInput input) : SV_TARGET
{
    return PanTexture.Sample(PanSampler, input.TexCoord);
}