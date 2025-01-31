// TextShader.hlsl

cbuffer TransformBuffer : register(b0)
{
    matrix worldViewProjection;
};

struct VS_INPUT
{
    float3 Position : POSITION; // Text quad position
    float2 Size : TEXCOORD0; // Text quad size
    float4 UVCoords : TEXCOORD1; // Texture UV coordinates
    float4 color : COLOR; // Text color
    float IsVisible : TEXCOORD2; // Visibility flag
    matrix RotationMatrix : TEXCOORD3; // Rotation matrix for entire text
};

struct VS_OUTPUT
{
    float4 Position : SV_POSITION;
    float2 UVCoords : TEXCOORD0;
    float4 Color : COLOR;
    float IsVisible : TEXCOORD1;
};

VS_OUTPUT main(VS_INPUT input)
{
    VS_OUTPUT output;

    // Define quad corners
    float3 quadCorners[4] =
    {
        float3(0, 0, 0),
        float3(input.Size.x, 0, 0),
        float3(0, input.Size.y, 0),
        float3(input.Size.x, input.Size.y, 0)
    };

    // Rotate and translate quad
    float4 rotatedPos = mul(float4(quadCorners[input.IsVisible], 1.0f), input.RotationMatrix);
    rotatedPos.xyz += input.Position;

    // Transform to clip space
    output.Position = mul(rotatedPos, worldViewProjection);

    // UV mapping
    float2 uvCorners[4] =
    {
        input.UVCoords.xy,
        float2(input.UVCoords.z, input.UVCoords.y),
        float2(input.UVCoords.x, input.UVCoords.w),
        input.UVCoords.zw
    };

    output.UVCoords = uvCorners[input.IsVisible];
    output.Color = input.color; // Pass color to pixel shader
    output.IsVisible = input.IsVisible;

    return output;
}


// Pixel Shader
Texture2D textAtlas : register(t0);
SamplerState samp : register(s0);

struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float2 UVCoords : TEXCOORD0;
    float4 Color : COLOR;
    float IsVisible : TEXCOORD1;
};

float4 main(PS_INPUT input) : SV_TARGET
{
    if (input.IsVisible < 0.5f)
        discard; // Ignore invisible quads

    float4 sampledColor = textAtlas.Sample(samp, input.UVCoords);
    
    // Apply text color
    return sampledColor * input.Color;
}




