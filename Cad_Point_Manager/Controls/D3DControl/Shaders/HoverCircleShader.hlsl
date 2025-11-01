// HoverCircleShader.hlsl

cbuffer TransformationBuffer : register(b0)
{
    row_major matrix transformationMatrix; // 2D transformation matrix
};
cbuffer CircleGlowSettingsBuffer : register(b1)
{
    float glowOffset;
    float3 padding;
    float4 hoverColor;
    float4 selectedColor;
    float4 selectedMouseOverColor;
};

struct VS_INPUT
{
    float3 position : POSITION;
    float radius : RADIUS;
    float isSelected : ISSELECTED;
};

struct GS_OUTPUT
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
    float2 offset : RADIUS;
    float isSelected : ISSELECTED;
};

void EmitCorner(VS_INPUT input, float4 position, float2 offset, float4 color, inout TriangleStream<GS_OUTPUT> output)
{
    GS_OUTPUT o;
    o.position = position;
    o.offset = offset;
    o.color = color;
    o.isSelected = input.isSelected;
    output.Append(o);
}

// =======================
// Vertex Shader
// =======================
VS_INPUT VSMain(VS_INPUT input)
{
    return input;
}

// =======================
// Geometry Shader
// =======================
[maxvertexcount(4)]
void GSMain(point VS_INPUT input[1], inout TriangleStream<GS_OUTPUT> output)
{
    float4 position = mul(float4(input[0].position, 1), transformationMatrix);
    float radiusX = input[0].radius * transformationMatrix._11;
    float radiusY = input[0].radius * transformationMatrix._22;

    EmitCorner(input[0], float4(position.x - radiusX, position.y + radiusY, 0, 1), float2(-1, 1), hoverColor, output); // TL
    EmitCorner(input[0], float4(position.x - radiusX, position.y - radiusY, 0, 1), float2(-1, -1), hoverColor, output); // BL
    EmitCorner(input[0], float4(position.x + radiusX, position.y + radiusY, 0, 1), float2(1, 1), hoverColor, output); // TR
    EmitCorner(input[0], float4(position.x + radiusX, position.y - radiusY, 0, 1), float2(1, -1), hoverColor, output); // BR
}

// =======================
// Pixel Shader
// =======================
float4 PSMain(GS_OUTPUT input) : SV_TARGET
{
    float dist = length(input.offset);
    if (dist > 1.0f)
    {
        discard;
    }

    return input.color;
}
