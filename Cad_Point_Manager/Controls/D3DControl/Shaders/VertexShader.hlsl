cbuffer ConstantBuffer : register(b0)
{
    matrix ProjectionMatrix;
};

struct VSInput
{
    float3 Position : POSITION;
    float4 Color : COLOR;
};

struct PSInput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
};

PSInput VSMain(VSInput input)
{
    PSInput output;
    output.Position = mul(float4(input.Position, 1.0f), ProjectionMatrix);
    output.Color = input.Color;
    return output;
}
