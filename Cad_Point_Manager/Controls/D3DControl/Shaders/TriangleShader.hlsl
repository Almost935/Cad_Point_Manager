// TriangleShader.hlsl

// Constant buffer for 2D transformation matrix
cbuffer TransformationBuffer : register(b0)
{
    row_major matrix transformationMatrix; // 2D transformation matrix
};

cbuffer TriangleSettingsBuffer : register(b1)
{
    float4 SelectedColor;
    float4 SelectedMouseOverColor;
    float2 Padding;
    float GlowOffset;
    float GlowTransparency;
};

float4 GetSnappedColor(float4 color)
{
    float3 lightBlue = float3(0.4, 0.4, 1.0);
    float3 resultRgb = lerp(color.rgb, lightBlue, 0.7);

    return float4(resultRgb, color.a);
}

// Input structure for the Vertex Shader
struct VSInput
{
    float3 Position : POSITION; // 3D position of the vertex
    float4 Color : COLOR; // RGBA color
    float IsVisible : ISVISIBLE;
    float IsMouseOver : ISMOUSEOVER;
    float IsSelected : ISSELECTED; // Float: 0.0 or 1.0
};

// Output structure from the Vertex Shader and input for the Pixel Shader
struct PSInput
{
    float4 Position : SV_POSITION; // Transformed position in screen space
    float4 Color : COLOR; // RGBA color passed to the Pixel Shader
};

// Vertex Shader: Transforms input vertex and passes color through
PSInput VSMain(VSInput input)
{
    PSInput output;
    
    //if (input.IsVisible < 0.5)
    //{
    //    output.Color = float4(0, 0, 0, 0);
    //    return output;
    //}
    
    //if (input.IsMouseOver > 0.5)
    //{
    //    input.Color = GetSnappedColor(input.Color);
    //}
    
    //if (input.IsSelected > 0.5)
    //{
    //    input.Color = SelectedColor;
    //}
    
    output.Position = mul(float4(input.Position, 1.0), transformationMatrix);
    output.Color = input.Color;

    return output;
}

// Pixel Shader: Determines the color of each pixel
float4 PSMain(PSInput input) : SV_TARGET
{
    return input.Color;
}

