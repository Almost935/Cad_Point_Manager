// CircleGlowShader.hlsl

cbuffer TransformationBuffer : register(b0)
{
    row_major matrix transformationMatrix; // 2D transformation matrix
};
cbuffer CircleGlowSettingsBuffer : register(b1)
{
    float glowOffset;
    float glowTransparency;
    float padding;
    float4 selectedColor;
    float4 selectedMouseOverColor;
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
    float4 color : COLOR;
    float radius : RADIUS;
    float isVisible : ISVISIBLE;
    float isMouseOver : ISMOUSEOVER;
    float isSelected : ISSELECTED;
};

struct GS_OUTPUT
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
    float2 offset : TEXCOORD0; 
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
[maxvertexcount(4)]
void GSMain(point VS_INPUT input[1], inout TriangleStream<GS_OUTPUT> output)
{
    if (input[0].isVisible < 0.5f)
    {
        return;
    }
    
    float4 center = mul(float4(input[0].position, 1), transformationMatrix);
    float radiusX = input[0].radius * transformationMatrix._11;
    float radiusY = input[0].radius * transformationMatrix._22;
    
    if (input[0].isMouseOver > 0.5)
    {
        float radiusX = (input[0].radius + glowOffset + 10) * transformationMatrix._11;
        float radiusY = (input[0].radius + glowOffset + 10) * transformationMatrix._22;
        
        input[0].color = GetSnappedColor(input[0].color);
    }
    
    if (input[0].isSelected > 0.5)
    {
        if (input[0].isMouseOver > 0.5) 
        {
            input[0].color = selectedMouseOverColor; 
        }
        else
        {
            input[0].color = selectedColor; 
        }
    }

    EmitCorner(input[0], float4(center.x - radiusX, center.y + radiusY, 0, 1), float2(-1, 1), output); // TL
    EmitCorner(input[0], float4(center.x - radiusX, center.y - radiusY, 0, 1), float2(-1, -1), output); // BL
    EmitCorner(input[0], float4(center.x + radiusX, center.y + radiusY, 0, 1), float2(1, 1), output); // TR
    EmitCorner(input[0], float4(center.x + radiusX, center.y - radiusY, 0, 1), float2(1, -1), output); // BR
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
