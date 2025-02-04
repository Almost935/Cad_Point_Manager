

cbuffer TransformBuffer : register(b0)
{
    matrix ViewProjection;
};

struct TextVertex
{
    float3 Position : POSITION;
    float2 TexCoord : TEXCOORD0;
    uint TexIndex : TEXINDEX;
    float4 Color : COLOR;
    float Rotation : ROTATION;
    bool IsVisible : VISIBLE;
};

struct PixelInputType
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
    uint TexIndex : TEXINDEX;
    float4 Color : COLOR;
};

// Texture Array for text rendering
Texture2DArray Textures : register(t0);
SamplerState SampleType : register(s0);

PixelInputType VS(TextVertex input)
{
    PixelInputType output;

    if (!input.IsVisible) // Skip processing if text is not visible
        discard;

    // Compute rotation about anchor point
    float cosTheta = cos(input.Rotation);
    float sinTheta = sin(input.Rotation);

    Texture2D Texture = Textures[input.TexIndex];
    Texture.Sample()
    
    float2 rotatedPosition;
    rotatedPosition.x = input.Position.x * cosTheta - input.Position.y * sinTheta;
    rotatedPosition.y = input.Position.x * sinTheta + input.Position.y * cosTheta;

    // Apply transformation matrix
    output.Position = mul(float4(rotatedPosition, input.Position.z, 1.0f), ViewProjection);
    output.TexCoord = input.TexCoord;
    output.TexIndex = input.TexIndex;
    output.Color = input.Color;
    
    return output;
}

float4 PS(PixelInputType input) : SV_TARGET
{
    // Sample from the correct texture in the array
    float4 sampledColor = Textures.Sample(SampleType, float3(input.TexCoord, input.TexIndex));

    // Multiply by the provided color
    return sampledColor * input.Color;
}




//Texture2DArray TextAtlasArray : register(t0);
//SamplerState TextSampler : register(s0);

//cbuffer TransformBuffer : register(b0)
//{
//    matrix WorldViewProjection;
//};

//struct VS_INPUT
//{
//    float3 Position : POSITION;
//    float2 Size : SIZE;
//    float4 UVCoords : TEXCOORD;
//    float4 Color : COLOR;
//    float IsVisible : ISVISIBLE;
//    matrix RotationMatrix : ROTATION;
//    uint AtlasIndex : ATLASINDEX;
//};

//struct PS_INPUT
//{
//    float4 Position : SV_POSITION;
//    float2 TexCoord : TEXCOORD;
//    float4 Color : COLOR;
//    uint AtlasIndex : ATLASINDEX;
//};

//PS_INPUT VSMain(VS_INPUT input)
//{
//    PS_INPUT output;
    
//    // Transform position using rotation and projection
//    float4 worldPos = mul(float4(input.Position, 1.0f), input.RotationMatrix);
//    worldPos = mul(worldPos, WorldViewProjection);
    
//    output.Position = worldPos;
    
//    // Calculate texture coordinates
//    output.TexCoord = input.UVCoords.xy; // Only using Top-Left UV
//    output.Color = input.Color;
//    output.AtlasIndex = input.AtlasIndex;

//    return output;
//}

//float4 PSMain(PS_INPUT input) : SV_TARGET
//{
//    // Sample the correct texture from the Texture2DArray
//    float4 textColor = TextAtlasArray.Sample(TextSampler, float3(input.TexCoord, input.AtlasIndex));
    
//    // Multiply texture color with input color for tinting
//    return textColor * input.Color;
//}
