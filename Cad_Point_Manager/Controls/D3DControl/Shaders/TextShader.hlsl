

struct VS_INPUT
{
    float3 Position : POSITION;
    float2 TexCoord : TEXCOORD0;
};

struct VS_OUTPUT
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

// Constant buffer for 2D transformation matrix
cbuffer TransformationBuffer : register(b0)
{
    row_major matrix transformationMatrix; // 2D transformation matrix
};

VS_OUTPUT VSMain(VS_INPUT input)
{
    VS_OUTPUT output;
    
    // Transform the position using world-view-projection matrix
    output.Position = mul(float4(input.Position, 1.0), transformationMatrix);

    output.TexCoord = input.TexCoord; // Pass texture coordinate to pixel shader

    return output;
}




Texture2D TextTexture : register(t0);
SamplerState TextSampler : register(s0);

struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

float4 PSMain(PS_INPUT input) : SV_TARGET
{
    return TextTexture.Sample(TextSampler, input.TexCoord);
}

