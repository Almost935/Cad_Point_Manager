// Input structure for the Vertex Shader
struct VSInput
{
    float3 Position : POSITION; // 3D position of the vertex
    float4 Color : COLOR; // RGBA color of the vertex
    float HighlightFlag : TEXCOORD0; // Highlight flag (1.0 for highlighted, 0.0 otherwise)
};

// Output structure from the Vertex Shader and input for the Geometry Shader
struct GSInput
{
    float4 Position : SV_POSITION; // Transformed position in screen space
    float4 Color : COLOR; // RGBA color
    float HighlightFlag : TEXCOORD0; // Highlight flag
};

// Output structure from the Geometry Shader and input for the Pixel Shader
struct PSInput
{
    float4 Position : SV_POSITION; // Final transformed position
    float4 Color : COLOR; // RGBA color
};

// Constant buffer for transformation
cbuffer TransformationBuffer : register(b0)
{
    row_major matrix transformationMatrix; // Transformation matrix
}

// Constant buffer for highlight parameters
cbuffer HighlightBuffer : register(b1)
{
    float LineWidth; // Base line width
    float HighlightWidthBoost; // Additional width for highlighted lines
    float4 HighlightColor; // Highlight color (RGBA)
}

// Vertex Shader: Transforms vertices and passes through highlight flag
GSInput VSMain(VSInput input)
{
    GSInput output;

    // Apply transformation to position
    output.Position = mul(float4(input.Position, 1.0), transformationMatrix);

    // Pass color and highlight flag unchanged
    output.Color = input.Color;
    output.HighlightFlag = input.HighlightFlag;

    return output;
}

// Geometry Shader: Generates quads for lines, applying highlight effects
[maxvertexcount(4)]
void GSMain(line GSInput input[2], inout TriangleStream<PSInput> triStream)
{
    float2 direction = normalize(input[1].Position.xy - input[0].Position.xy);
    float2 perpendicular = float2(-direction.y, direction.x);

    // Determine effective line width and color
    float highlightFactor = max(input[0].HighlightFlag, input[1].HighlightFlag);
    float effectiveLineWidth = LineWidth + highlightFactor * HighlightWidthBoost;
    float4 effectiveColor = lerp(input[0].Color, HighlightColor, highlightFactor);

    // Scale perpendicular vector by half the effective width
    perpendicular *= effectiveLineWidth * 0.5;

    // Create vertices for the quad
    PSInput vertices[4];
    vertices[0].Position = input[0].Position + float4(-perpendicular, 0, 0);
    vertices[1].Position = input[0].Position + float4(perpendicular, 0, 0);
    vertices[2].Position = input[1].Position + float4(-perpendicular, 0, 0);
    vertices[3].Position = input[1].Position + float4(perpendicular, 0, 0);

    // Set the effective color for all vertices
    vertices[0].Color = vertices[1].Color = vertices[2].Color = vertices[3].Color = effectiveColor;

    // Emit two triangles to form the quad
    triStream.Append(vertices[0]);
    triStream.Append(vertices[1]);
    triStream.Append(vertices[2]);
    triStream.RestartStrip();
    triStream.Append(vertices[1]);
    triStream.Append(vertices[3]);
    triStream.Append(vertices[2]);
}

// Pixel Shader: Outputs the final color
float4 PSMain(PSInput input) : SV_TARGET
{
    return input.Color;
}
