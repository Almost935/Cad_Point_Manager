// CircleShader.hlsl

// Constant buffer for 2D transformation matrix
cbuffer TransformationBuffer : register(b0)
{
    row_major matrix transformationMatrix; // 2D transformation matrix
};

cbuffer SigPointSettingsBuffer : register(b1)
{
    float4 baseColor;
    float4 selectedColor;
    float radius;
    float2 viewportSize;
};

float4 GetSnappedColor(float4 color)
{
    float3 lightBlue = float3(0.4, 0.4, 1.0);
    float3 resultRgb = lerp(color.rgb, lightBlue, 0.7); 

    return float4(resultRgb, color.a);
}

struct VS_INPUT
{
    float3 position : POSITION;
    float isVisible : ISVISIBLE;
    float isSelected : ISSELECTED;
};

struct GS_OUTPUT
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
    float2 offset : TEXCOORD0;
    float isVisible : TEXCOORD1;
    float isSelected : TEXCOORD2;
};

void EmitCorner(VS_INPUT input, float4 position, float4 color, float2 offset, inout TriangleStream<GS_OUTPUT> output)
{
    GS_OUTPUT o;
    o.position = position;
    o.color = color;
    o.offset = offset;
    o.isVisible = input.isVisible;
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
    if (input[0].isVisible < 0.5f)
    {
        return;
    }
    
    float4 color = baseColor;
    
    if (input[0].isSelected > 0.5)
    {
        color = selectedColor;
    }

    float4 center = mul(float4(input[0].position, 1), transformationMatrix);
    float2 pixelRadiusClip = float2(radius / viewportSize.x, radius / viewportSize.y) * 2.0f;
    float radiusX = pixelRadiusClip.x;
    float radiusY = pixelRadiusClip.y;
    
    EmitCorner(input[0], float4(center.x - radiusX, center.y + radiusY, 0, 1), color, float2(-1, 1) , output); // TL
    EmitCorner(input[0], float4(center.x - radiusX, center.y - radiusY, 0, 1), color, float2(-1, -1), output); // BL
    EmitCorner(input[0], float4(center.x + radiusX, center.y + radiusY, 0, 1), color, float2(1, 1), output); // TR
    EmitCorner(input[0], float4(center.x + radiusX, center.y - radiusY, 0, 1), color, float2(1, -1), output); // BR
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
