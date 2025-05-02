// TextShader.hlsl

// Constant buffer for 2D transformation matrix
cbuffer TransformationBuffer : register(b0)
{
    row_major matrix transformationMatrix; // 2D transformation matrix
};

cbuffer TextSettingsBuffer : register(b1)
{
    float4 selectedColor;
    float4 selectedMouseOverColor;
};

float4 GetSnappedColor(float4 color)
{
    // A light blue color to blend towards (adjust to taste)
    float3 lightBlue = float3(0.4, 0.4, 1.0);

    // Lerp from original RGB to the blueish color
    float3 resultRgb = lerp(color.rgb, lightBlue, 0.7); // 0.4 means 40% blueish tint

    // Return the new color with original alpha
    return float4(resultRgb, color.a);
}

// Input structure for the Vertex Shader
struct VSInput
{
    float3 Position : POSITION; // 3D position of the vertex
    float4 Color : COLOR; // RGBA color of the vertex
    float IsVisible : ISVISIBLE; // Visibility flag (0 or 1)
    float IsMouseOver : ISMOUSEOVER; // Indicates if the mouse is over the vertex (0 or 1)
    float IsSelected : ISSELECTED; // Indicates if the vertex is selected (0 or 1)
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
    
    if (input.IsVisible < 0.5) // If not visible, skip the vertex
    {
        output.Color = float4(0, 0, 0, 0);
        return output;
    }
    
    if (input.IsMouseOver > 0.5)
    {
        input.Color = GetSnappedColor(input.Color);
    }
    
    if (input.IsSelected > 0.5)
    {
        if (input.IsMouseOver > 0.5) // If mouse is over the vertex and selected
        {
            input.Color = selectedMouseOverColor; // Use mouse over color for selected vertex
        }
        else
        {
            input.Color = selectedColor; // Use selected color
        }
    }
     
    output.Position = mul(float4(input.Position, 1.0), transformationMatrix);
    output.Color = input.Color;

    return output;
}

// Pixel Shader: Determines the color of each pixel
float4 PSMain(PSInput input) : SV_TARGET
{
    return input.Color;
}
