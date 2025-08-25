cbuffer TransformationBuffer : register(b0)
{
    row_major matrix transformationMatrix; // 2D transformation matrix
};

struct VSIn
{
    float3 pos : POSITION;
    float4 col : COLOR;
};
struct VSOut
{
    float4 pos : SV_POSITION;
    float4 col : COLOR;
};

VSOut VSMain(VSIn i)
{
    VSOut o;
    o.pos = mul(float4(i.pos, 1.0f), transformationMatrix);
    o.col = i.col;
    return o;
}

float4 PSMain(VSOut i) : SV_TARGET
{
    return i.col; // expect premultiplied or standard alpha
}
