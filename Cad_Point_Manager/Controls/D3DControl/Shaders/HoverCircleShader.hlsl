// HoverCircleShader.hlsl

cbuffer TransformationBuffer : register(b0)
{
    row_major matrix transformationMatrix; // 2D transformation matrix
};
cbuffer DrawingSettingsBuffer : register(b1)
{
    float2 ViewportSize;
    float2 _pad1;

    float LineHalfWidthPixels;
    float GlobalLineTypeScale;
    float AnnotationScale;
    float GlowPixelOffset;

    float4 SelectedColor;
    float4 SelectedMouseOverColor;
};

struct VS_INPUT
{
    float3 position : POSITION;
    float pointMarkerRadius : TEXCOORD0; // Radius of the point marker in pixels
    float isSelected : TEXCOORD1; // Whether the point is selected (1.0) or not (0.0)
};

struct GS_OUTPUT
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
    float2 offset : TEXCOORD0;
    float circleEdge : TEXCOORD1;
};

void EmitCorner(VS_INPUT input, float4 position, float4 color, float2 offset, float2 circleEdge, inout TriangleStream<GS_OUTPUT> output)
{
    GS_OUTPUT o;
    o.position = position;
    o.color = color;
    o.offset = offset;
    o.circleEdge = circleEdge;
    output.Append(o);
}

VS_INPUT VSMain(VS_INPUT input)
{
    return input;
}

[maxvertexcount(4)]
void GSMain(point VS_INPUT input[1], inout TriangleStream<GS_OUTPUT> output)
{
    float4 center = mul(float4(input[0].position, 1), transformationMatrix);
    float2 pixelRadiusClip = float2(GlowPixelOffset / ViewportSize.x, GlowPixelOffset / ViewportSize.y) * 2.0f;
    
    float radiusWorld = input[0].pointMarkerRadius;
    
    float radiusX = pixelRadiusClip.x + radiusWorld * transformationMatrix._11;
    float radiusY = pixelRadiusClip.y + radiusWorld * transformationMatrix._22;
    
    float circleClipX = radiusWorld * transformationMatrix._11;
    float circleClipY = radiusWorld * transformationMatrix._22;

    float quadClipX = radiusX;
    float quadClipY = radiusY;

    float2 circleEdge = float2(circleClipX / quadClipX, circleClipY / quadClipY);
    
    float4 hoverColor = float4(0, 0, 0, 0.4);
    
    EmitCorner(input[0], float4(center.x - radiusX, center.y + radiusY, 0, 1), hoverColor, float2(-1, 1), circleEdge, output); // TL
    EmitCorner(input[0], float4(center.x - radiusX, center.y - radiusY, 0, 1), hoverColor, float2(-1, -1), circleEdge, output); // BL
    EmitCorner(input[0], float4(center.x + radiusX, center.y + radiusY, 0, 1), hoverColor, float2(1, 1), circleEdge, output); // TR
    EmitCorner(input[0], float4(center.x + radiusX, center.y - radiusY, 0, 1), hoverColor, float2(1, -1), circleEdge, output); // BR
}

float4 PSMain(GS_OUTPUT input) : SV_TARGET
{
    float2 p = input.offset / input.circleEdge;

    float dist = length(p);

    // Outside the outer glow
    if (length(input.offset) > 1.0f)
    {
        discard;
    }

    // Inside the original ellipse
    if (dist <= 1.0f)
    {
        return input.color;
    }

    // Glow region
    float glowDist = length(input.offset);

    float t = (glowDist - length(input.circleEdge)) / (1.0f - length(input.circleEdge));

    float alpha = pow(1.0f - smoothstep(0.0f, 1.0f, t), 0.5f);

    return float4(input.color.rgb, input.color.a * alpha);
}

