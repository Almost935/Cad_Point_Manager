// DragOverlayOutlineShader.hlsl

cbuffer TransformationBuffer : register(b0) // you already have this
{
    row_major float4x4 transformationMatrix;
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
    float2 uv : TEXCOORD0;
};

VSOut VSMain(VSIn i)
{
    VSOut o;

    float2 worldPos = lerp(RectMinPx, RectMaxPx, i.local);

    o.pos = mul(float4(worldPos, 1.0f, 1.0f), transformationMatrix);
    o.uv = i.local;

    return o;
}

float4 PSMain(VSOut i) : SV_TARGET
{
    float2 dEdge = min(i.uv, 1.0f - i.uv);

    float du = max(length(float2(ddx(i.uv.x), ddy(i.uv.x))), 1e-6f);
    float dv = max(length(float2(ddx(i.uv.y), ddy(i.uv.y))), 1e-6f);

    float2 dPx = float2(dEdge.x / du, dEdge.y / dv);

    float minPx = min(dPx.x, dPx.y);

    float edge = smoothstep(ThicknessPx + FeatherPx, ThicknessPx, minPx);

    return float4(BorderColor.rgb, BorderColor.a * edge);
}
