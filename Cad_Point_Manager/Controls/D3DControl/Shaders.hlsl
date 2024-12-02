// Shaders.hlsl

// Input structure for the Vertex Shader
struct VSInput
{
    float3 Position : POSITION; // 3D position of the vertex
    float4 Color : COLOR; // RGBA color of the vertex
};

// Output structure from the Vertex Shader and input for the Pixel Shader
struct PSInput
{
    float4 Position : SV_POSITION; // Transformed position in screen space
    float4 Color : COLOR; // RGBA color passed to the Pixel Shader
};

// Constant buffer for 2D transformation matrix
cbuffer TransformationBuffer : register(b0)
{
    matrix transformationMatrix; // 2D transformation matrix
};

// Vertex Shader: Transforms input vertex and passes color through
PSInput VSMain(VSInput input)
{
    PSInput output;

    // Pass the position directly, converting to homogeneous coordinates (w = 1.0)
    //output.Position = float4(input.Position, 1.0);
    output.Position = mul(float4(input.Position, 1.0), transformationMatrix);

    // Pass the color unchanged
    output.Color = input.Color;

    return output;
}

// Pixel Shader: Determines the color of each pixel
float4 PSMain(PSInput input) : SV_TARGET
{
    // Return the color passed from the Vertex Shader
    return input.Color;
}
