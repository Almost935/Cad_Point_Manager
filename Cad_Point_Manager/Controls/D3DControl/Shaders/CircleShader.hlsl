// CircleShader.hlsl

// Constant buffer for 2D transformation matrix
cbuffer TransformationBuffer : register(b0)
{
    row_major matrix transformationMatrix; // 2D transformation matrix
};

cbuffer CircleSettingsBuffer : register(b1)
{
    float RadiusPixels;
    float2 ViewportSize;
    float Padding; // for 16-byte alignment
};

struct VS_INPUT
{
    float3 position : POSITION;
    float4 color : COLOR;
    float isVisible : ISVISIBLE;
    float isMouseOver : ISMOUSEOVER;
    float isSelected : ISSELECTED;
};

struct GS_OUTPUT
{
    float4 position : SV_POSITION;
    float2 offset : TEXCOORD0; // for radial distance in pixel shader
    float4 color : COLOR;
    float isVisible : TEXCOORD1;
    float isMouseOver : TEXCOORD2;
    float isSelected : TEXCOORD3;
};

void EmitCorner(VS_INPUT input, float4 position, float2 offset, inout TriangleStream<GS_OUTPUT> output)
{
    GS_OUTPUT o;
    o.position = position;
    o.offset = offset;
    o.color = input.color;
    o.isVisible = input.isVisible;
    o.isMouseOver = input.isMouseOver;
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
[maxvertexcount(6)]
void GSMain(point VS_INPUT input[1], inout TriangleStream<GS_OUTPUT> output)
{
    if (input[0].isVisible < 0.5f)
    {
        return;
    }
    
    float4 center = mul(float4(input[0].position, 1), transformationMatrix);
    float2 pixelSize = 2.0f / ViewportSize;
    float2 offset = RadiusPixels * pixelSize;

    float2 offsets[4] =
    {
        float2(-offset.x, offset.y), // top-left
        float2(offset.x, offset.y), // top-right
        float2(-offset.x, -offset.y), // bottom-left
        float2(offset.x, -offset.y) // bottom-right
    };

    float left = center.x - offset.x;
    float right = center.x + offset.x;
    float top = center.y + offset.y;
    float bottom = center.y - offset.y;
    
    GS_OUTPUT oTL;
    
    EmitCorner(input[0], float4(left, top, 0, 1), offset / 2, output);
    EmitCorner(input[0], float4(left, bottom, 0, 1), offset / 2, output);
    EmitCorner(input[0], float4(right, top, 0, 1), offset / 2, output);
    
    EmitCorner(input[0], float4(right, top, 0, 1), offset / 2, output);
    EmitCorner(input[0], float4(left, bottom, 0, 1), offset / 2, output);
    EmitCorner(input[0], float4(right, bottom, 0, 1), offset / 2, output);
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

    float alpha = input.color.a * (1.0f - smoothstep(0.95f, 1.0f, dist));
    
    return float4(input.color.rgb, alpha);
}
