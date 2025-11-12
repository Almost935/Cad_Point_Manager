// SignificantPointShader.hlsl

cbuffer TransformationBuffer : register(b0)
{
    row_major matrix transformationMatrix;
};

cbuffer CircleSettingsBuffer : register(b1)
{
    float4 color;
    float2 viewportSize;
    float radiusPx;
    float _pad;
};

struct VS_INPUT
{
    float3 position : POSITION;
};

struct GS_OUTPUT
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
    float2 offset : TEXCOORD0;
};

VS_INPUT VSMain(VS_INPUT input)
{
    return input;
}

void EmitCorner(VS_INPUT input, float4 position, float4 color, float2 offset, inout TriangleStream<GS_OUTPUT> output)
{
    GS_OUTPUT o;
    o.position = position;
    o.color = color;
    o.offset = offset;
    output.Append(o);
}

[maxvertexcount(4)]
void GSMain(point VS_INPUT input[1], inout TriangleStream<GS_OUTPUT> output)
{
    float4 center = mul(float4(input[0].position, 1), transformationMatrix);
    float2 pixelRadiusClip = float2(radiusPx / viewportSize.x, radiusPx / viewportSize.y) * 2.0f;
    float radiusX = pixelRadiusClip.x;
    float radiusY = pixelRadiusClip.y;
    
    EmitCorner(input[0], float4(center.x - radiusX, center.y + radiusY, 0, 1), color, float2(-1, 1), output); // TL
    EmitCorner(input[0], float4(center.x - radiusX, center.y - radiusY, 0, 1), color, float2(-1, -1), output); // BL
    EmitCorner(input[0], float4(center.x + radiusX, center.y + radiusY, 0, 1), color, float2(1, 1), output); // TR
    EmitCorner(input[0], float4(center.x + radiusX, center.y - radiusY, 0, 1), color, float2(1, -1), output); // BR
}

float4 PSMain(GS_OUTPUT input) : SV_TARGET
{
    float dist = length(input.offset);
    if (dist > 1.0f)
    {
        discard;
    }
    return input.color;
}