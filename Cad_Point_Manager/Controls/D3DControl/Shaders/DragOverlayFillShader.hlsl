// DragOverlayFillShader.hlsl

cbuffer TransformationBuffer : register(b0)
{
    row_major matrix transformationMatrix; // 2D transformation matrix
};

cbuffer DragOverlaySettings : register(b1)
{
    float2 RectMinPx;
    float2 RectMaxPx;

    float2 ViewportSize;
    float ThicknessPx;
    float FeatherPx;

    float4 FillColor;
    float4 BorderColor;
};

struct VSIn
{
    float2 local : LOCAL;
};

struct VSOut
{
    float4 pos : SV_POSITION;
};

VSOut VSMain(VSIn i)
{
    VSOut o;

    float2 worldPos = lerp(RectMinPx, RectMaxPx, i.local);
    o.pos = mul(float4(worldPos, 1.0f, 1.0f), transformationMatrix);

    return o;
}

float4 PSMain(VSOut i) : SV_TARGET
{
    return FillColor;
}