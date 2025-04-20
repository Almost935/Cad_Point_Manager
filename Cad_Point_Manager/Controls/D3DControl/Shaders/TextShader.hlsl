// TextShader.hlsl

// Constant buffer for 2D transformation matrix
cbuffer TransformationBuffer : register(b0)
{
    row_major matrix transformationMatrix; // 2D transformation matrix
};

// Input structure for the Vertex Shader
struct VSInput
{
    float3 Position : POSITION; // 3D position of the vertex
    float4 Color : COLOR; // RGBA color of the vertex
    float2 GlowDirection : GLOWDIRECTION;
    float IsVisible : ISVISIBLE;
    float IsMouseOver : ISMOUSEOVER;
    float Transparency : TRANSPARENCY;
    float GlowOffset : GLOWOFFSET;
};

// Output structure from the Vertex Shader and input for the Pixel Shader
struct PSInput
{
    float4 Position : SV_POSITION; // Transformed position in screen space
    float4 Color : COLOR; // RGBA color passed to the Pixel Shader
    float2 GlowDirection : GLOWDIRECTION;
    float IsMouseOver : ISMOUSEOVER;
    float Transparency : TRANSPARENCY;
};

// Vertex Shader: Transforms input vertex and passes color through
PSInput VSMain(VSInput input)
{
    PSInput output;
    
    if (input.IsVisible < 0.5) // If not visible, skip the vertex
    {
        output.Color.a = 0;
        
        return output; // Return default values or handle accordingly
    }
    
    // Pass the position directly, converting to homogeneous coordinates (w = 1.0)
    output.Position = mul(float4(input.Position, 1.0), transformationMatrix);
    
    if (input.IsMouseOver > 0.5) // If not mouse over, set color alpha to 0
    {
        output.Position.xy += input.GlowOffset * input.GlowDirection * transformationMatrix._11; // Apply glow direction offset
    }

    input.Color.a = input.Transparency;
    output.Color = input.Color;
    output.IsMouseOver = input.IsMouseOver;
    output.GlowDirection = input.GlowDirection;
    output.Transparency = input.Transparency;

    return output;
}

// Pixel Shader: Determines the color of each pixel
float4 PSMain(PSInput input) : SV_TARGET
{
    if (input.IsMouseOver > 0.5)
    {
        //input.Color.rgb = lerp(input.Color.rgb, float3(0.4, 0.4, 0.7), 0.8);
        input.Color = float4(1, 0, 0, 1); // Dim the color if mouse is over
    }
    
    // Return the color passed from the Vertex Shader
    return input.Color;
}
