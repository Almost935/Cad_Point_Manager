// Input structure for the Vertex Shader
struct VSInput
{
    float3 Position : POSITION; // 3D position of the vertex
    float4 Color : COLOR; // RGBA color of the vertex
    float HighlightFlag : TEXCOORD0; // Highlight flag (1.0 for highlighted, 0.0 otherwise)
    float GlowFlag : TEXCOORD1; // Glow flag (1.0 for glowing, 0.0 otherwise)
};

// Output structure from the Vertex Shader and input for the Geometry Shader
struct GSInput
{
    float4 Position : SV_POSITION; // Transformed position in screen space
    float4 Color : COLOR; // RGBA color
    float HighlightFlag : TEXCOORD0; // Highlight flag
    float GlowFlag : TEXCOORD1; // Glow flag
};

// Output structure from the Geometry Shader and input for the Pixel Shader
struct PSInput
{
    float4 Position : SV_POSITION; // Final transformed position
    float4 Color : COLOR; // RGBA color
    float GlowFlag : TEXCOORD1; // Glow flag
};

// Constant buffer for transformation
cbuffer TransformationBuffer : register(b0)
{
    row_major matrix transformationMatrix; // Transformation matrix
}

// Constant buffer for highlight and glow parameters
cbuffer HighlightBuffer : register(b1)
{
    float LineWidth; // Base line width
    float HighlightWidthBoost; // Additional width for highlighted lines
    float GlowWidthBoost; // Additional width for glowing lines
    float GlowOpacity; // Opacity for glowing lines
    float4 HighlightColor; // Highlight color (RGBA)
    float4 GlowColor; // Glow color (RGBA)
}

// Vertex Shader: Transforms vertices and passes flags
GSInput VSMain(VSInput input)
{
    GSInput output;

    // Apply transformation to position
    output.Position = mul(float4(input.Position, 1.0), transformationMatrix);

    // Pass color and flags unchanged
    output.Color = input.Color;
    output.HighlightFlag = input.HighlightFlag;
    output.GlowFlag = input.GlowFlag;

    return output;
}

// Geometry Shader: Generates quads for lines, with optional highlight and glow
[maxvertexcount(8)]
void GSMain(line GSInput input[2], inout TriangleStream<PSInput> triStream)
{
    float2 direction = normalize(input[1].Position.xy - input[0].Position.xy);
    float2 perpendicular = float2(-direction.y, direction.x);

    // Handle glow rendering
    if (input[0].GlowFlag > 0.0 || input[1].GlowFlag > 0.0)
    {
        float glowWidth = LineWidth + GlowWidthBoost;
        float4 glowColor = GlowColor;
        glowColor.a *= GlowOpacity; // Apply transparency

        perpendicular *= glowWidth * 0.5;

        PSInput vertices[4];
        vertices[0].Position = input[0].Position + float4(-perpendicular, 0, 0);
        vertices[1].Position = input[0].Position + float4(perpendicular, 0, 0);
        vertices[2].Position = input[1].Position + float4(-perpendicular, 0, 0);
        vertices[3].Position = input[1].Position + float4(perpendicular, 0, 0);

        vertices[0].Color = vertices[1].Color = vertices[2].Color = vertices[3].Color = glowColor;
        vertices[0].GlowFlag = vertices[1].GlowFlag = vertices[2].GlowFlag = vertices[3].GlowFlag = 1.0;

        triStream.Append(vertices[0]);
        triStream.Append(vertices[1]);
        triStream.Append(vertices[2]);
        triStream.RestartStrip();
        triStream.Append(vertices[1]);
        triStream.Append(vertices[3]);
        triStream.Append(vertices[2]);
    }

    // Handle normal line rendering
    float effectiveLineWidth = LineWidth + max(input[0].HighlightFlag, input[1].HighlightFlag) * HighlightWidthBoost;
    float4 effectiveColor = lerp(input[0].Color, HighlightColor, max(input[0].HighlightFlag, input[1].HighlightFlag));

    perpendicular = float2(-direction.y, direction.x) * effectiveLineWidth * 0.5;

    PSInput vertices[4];
    vertices[0].Position = input[0].Position + float4(-perpendicular, 0, 0);
    vertices[1].Position = input[0].Position + float4(perpendicular, 0, 0);
    vertices[2].Position = input[1].Position + float4(-perpendicular, 0, 0);
    vertices[3].Position = input[1].Position + float4(perpendicular, 0, 0);

    vertices[0].Color = vertices[1].Color = vertices[2].Color = vertices[3].Color = effectiveColor;
    vertices[0].GlowFlag = vertices[1].GlowFlag = vertices[2].GlowFlag = vertices[3].GlowFlag = 0.0;

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
