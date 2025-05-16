// CircleShader.hlsl

cbuffer TransformationBuffer : register(b0)
{
    matrix WorldViewProjection;
};

cbuffer CircleSettingsBuffer : register(b1)
{
    float RadiusPixels;
    float2 ViewportSize;
    float Padding; // for 16-byte alignment
};

struct DebugOutput
{
    float4 pos;
    float4 color;
    float2 offset;
    float2 pixelSize;
};

RWStructuredBuffer<DebugOutput> DebugBuffer : register(u1);
globallycoherent RWByteAddressBuffer DebugCounter : register(u2);



struct VS_INPUT
{
    float3 position : POSITION;
    float4 color : COLOR;
    float isVisible : ISVISIBLE;
    float isMouseOver : ISMOUSEOVER;
    float isSelected : ISSELECTED;
};

struct VS_OUTPUT
{
    float4 worldPos : SV_POSITION;
    float4 color : COLOR;
    float3 centerWorld : TEXCOORD0;
    float isVisible : TEXCOORD1;
    float isMouseOver : TEXCOORD2;
    float isSelected : TEXCOORD3;
};

struct GS_OUTPUT
{
    float4 position : SV_POSITION;
    float2 offset : TEXCOORD0; // for radial distance in pixel shader
    float4 color : COLOR;
    float isVisible : TEXCOORD1;
    float isMouseOver : TEXCOORD2;
    float isSelected : TEXCOORD3;
};

// =======================
// Vertex Shader
// =======================
VS_OUTPUT VSMain(VS_INPUT input)
{
    VS_OUTPUT output;

    float4 world = float4(input.position, 1.0f);
    //output.worldPos = mul(world, WorldViewProjection);
    output.worldPos = world;
    output.centerWorld = input.position;
    output.color = input.color;
    output.isVisible = input.isVisible;
    output.isMouseOver = input.isMouseOver;
    output.isSelected = input.isSelected;

    return output;
}

// =======================
// Geometry Shader
// =======================
[maxvertexcount(4)]
void GSMain(point VS_OUTPUT input[1], inout TriangleStream<GS_OUTPUT> output)
{
    //if (input[0].isVisible < 0.5f)
    //{
    //    return;
    //}
    
    float4 centerClip = input[0].worldPos;
    float2 pixelSize = 2.0f / ViewportSize;
    //float2 offset = RadiusPixels * pixelSize;
    float2 offset = float2(500, 100);

    float2 offsets[4] =
    {
        float2(-offset.x, offset.y), // top-left
        float2(offset.x, offset.y), // top-right
        float2(-offset.x, -offset.y), // bottom-left
        float2(offset.x, -offset.y) // bottom-right
    };

    int order[4] = { 0, 1, 2, 3 }; // TL, BL, TR, BR

    for (int i = 0; i < 4; ++i)
    {
        //// Test
        //// Emit test quad in screen center
        //float2 testOffsets[4] =
        //{
        //    float2(-0.1, 0.1), // TL
        //    float2(-0.1, -0.1), // BL
        //    float2(0.1, 0.1), // TR
        //    float2(0.1, -0.1) // BR
        //};

        //int order[4] = { 0, 1, 2, 3 };
        //for (int i = 0; i < 4; ++i)
        //{
        //    GS_OUTPUT o;
        //    o.position = float4(0, 0, 0, 1);
        //    o.position.xy += testOffsets[order[i]];
        //    o.color = float4(1, 0, 0, 1);
        //    o.offset = float2(0, 0);
        //    o.isVisible = 1;
        //    o.isMouseOver = 0;
        //    o.isSelected = 0;
        //    output.Append(o);
        //}

        
        float2 offs = offsets[order[i]];

        GS_OUTPUT o;
        o.position = centerClip;
        o.position.xy += offs;
        o.position.w = 1;
        o.position = mul(o.position, WorldViewProjection);
        o.offset = offs / offset;
        o.color = input[0].color;
        o.isVisible = input[0].isVisible;
        o.isMouseOver = input[0].isMouseOver;
        o.isSelected = input[0].isSelected;

        output.Append(o);
    }
}

// =======================
// Pixel Shader
// =======================
float4 PSMain(GS_OUTPUT input) : SV_TARGET
{
    //float dist = length(input.offset);
    //if (dist > 1.0f)
    //{
    //    discard;
    //}

    //float alpha = input.color.a * (1.0f - smoothstep(0.95f, 1.0f, dist));
    //return float4(input.color.rgb, alpha);
    
    DebugOutput debug;
    uint index;
    DebugCounter.InterlockedAdd(0, 4, index);
    index /= 4;

    DebugOutput d;
    d.pos = input.color;
    //d.offset = input.offset;
    d.offset = input.offset;
    d.pixelSize = 2.0f / ViewportSize;
    d.color = input.color; 
    DebugBuffer[index] = d;
    
    return float4(1, 0, 0, 1);

}
