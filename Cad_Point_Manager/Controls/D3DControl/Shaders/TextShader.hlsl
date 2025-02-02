Texture2DArray TextAtlasArray : register(t0);
SamplerState TextSampler : register(s0);

cbuffer TransformBuffer : register(b0)
{
    matrix WorldViewProjection;
};

struct VS_INPUT
{
    float3 Position : POSITION;
    float2 Size : SIZE;
    float4 UVCoords : TEXCOORD;
    float4 Color : COLOR;
    float IsVisible : ISVISIBLE;
    matrix RotationMatrix : ROTATION;
    uint AtlasIndex : ATLASINDEX;
};

struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD;
    float4 Color : COLOR;
    uint AtlasIndex : ATLASINDEX;
};

PS_INPUT VSMain(VS_INPUT input)
{
    PS_INPUT output;
    
    // Transform position using rotation and projection
    float4 worldPos = mul(float4(input.Position, 1.0f), input.RotationMatrix);
    worldPos = mul(worldPos, WorldViewProjection);
    
    output.Position = worldPos;
    
    // Calculate texture coordinates
    output.TexCoord = input.UVCoords.xy; // Only using Top-Left UV
    output.Color = input.Color;
    output.AtlasIndex = input.AtlasIndex;

    return output;
}

float4 PSMain(PS_INPUT input) : SV_TARGET
{
    // Sample the correct texture from the Texture2DArray
    float4 textColor = TextAtlasArray.Sample(TextSampler, float3(input.TexCoord, input.AtlasIndex));
    
    // Multiply texture color with input color for tinting
    return textColor * input.Color;
}
