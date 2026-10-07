// PointCircleGlowShader.hlsl

cbuffer TransformationBuffer : register(b0)
{
    row_major matrix transformationMatrix;
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
    
    float CogoPointCircleRadiusPixels;
    float3 _pad2;
};

//--------------------------------------------
// GPU state
//--------------------------------------------

struct PointState
{
    float2 Offset;
    float2 PointInfoOffset;
    uint GroupId;
    uint Flags;
    uint LabelQuadrant;
    uint _padLS;
};
struct GroupState
{
    float4 Color;
    float Scale;
    uint Flags;
    float TextInfoBaseXoffset;
    float _padGS;
};

//--------------------------------------------
// Input
//--------------------------------------------

struct VS_INPUT
{
    uint pointId : POINT_ID;
};

struct GS_OUTPUT
{
    float4 position : SV_POSITION;
    float2 offset : TEXCOORD0;
    float2 circleEdge : TEXCOORD1;
    nointerpolation uint pointId : TEXCOORD2;
};

StructuredBuffer<PointState> PointStates : register(t0);
StructuredBuffer<GroupState> GroupStates : register(t1);

static const uint POINT_VISIBLE = 1u << 0;
static const uint POINT_SELECTED = 1u << 1;
static const uint POINT_MOUSEOVER = 1u << 2;
static const uint GROUP_VISIBLE = 1u << 0;

//--------------------------------------------
// Emit
//--------------------------------------------

void EmitCorner(
uint pointId, float4 position, float2 offset, float2 circleEdge, inout TriangleStream<GS_OUTPUT> output)
{
    GS_OUTPUT o;

    o.position = position;
    o.offset = offset;
    o.circleEdge = circleEdge;
    o.pointId = pointId;

    output.Append(o);
}

//--------------------------------------------
// Vertex shader
//--------------------------------------------

VS_INPUT VSMain(VS_INPUT input)
{
    return input;
}

//--------------------------------------------
// Geometry shader
//--------------------------------------------

[maxvertexcount(4)]
void GSMain(point VS_INPUT input[1], inout TriangleStream<GS_OUTPUT> output)
{
    PointState ps = PointStates[input[0].pointId];
    GroupState gs = GroupStates[ps.GroupId];

    //--------------------------------------------
    // Visibility
    //--------------------------------------------

    if ((gs.Flags & GROUP_VISIBLE) == 0u)
        return;

    if ((ps.Flags & POINT_VISIBLE) == 0u)
        return;

    //--------------------------------------------
    // Interaction
    //--------------------------------------------

    bool selected = (ps.Flags & POINT_SELECTED) != 0u;
    bool mouseOver = (ps.Flags & POINT_MOUSEOVER) != 0u;

    if (!selected && !mouseOver)
    {
        return;
    }

    //--------------------------------------------
    // Live point position
    //--------------------------------------------

    float4 center = mul(float4(ps.Offset.xy, 0.0f, 1.0f), transformationMatrix);

    //--------------------------------------------
    // Actual marker radius
    //--------------------------------------------

    float radiusWorld = CogoPointCircleRadiusPixels * gs.Scale;
    float circleClipX = radiusWorld * transformationMatrix._11;
    float circleClipY = radiusWorld * transformationMatrix._22;

    //--------------------------------------------
    // Maximum glow expansion
    //--------------------------------------------

    float2 glowClip = float2(
        GlowPixelOffset / ViewportSize.x, GlowPixelOffset / ViewportSize.y) * 2.0f;

    float quadClipX = circleClipX + glowClip.x;
    float quadClipY = circleClipY + glowClip.y;

    //--------------------------------------------
    // Original marker boundary relative to
    // expanded quad
    //--------------------------------------------

    float2 circleEdge = float2(
        circleClipX / quadClipX, circleClipY / quadClipY);

    //--------------------------------------------
    // Emit expanded quad
    //--------------------------------------------

    EmitCorner(
        input[0].pointId, float4(center.x - quadClipX, center.y + quadClipY, 0, 1),
        float2(-1, 1), circleEdge, output);
    EmitCorner(
        input[0].pointId, float4(center.x - quadClipX, center.y - quadClipY, 0, 1),
        float2(-1, -1), circleEdge, output);
    EmitCorner(
        input[0].pointId, float4(center.x + quadClipX, center.y + quadClipY, 0, 1),
        float2(1, 1), circleEdge, output);
    EmitCorner(
        input[0].pointId, float4(center.x + quadClipX, center.y - quadClipY, 0, 1),
        float2(1, -1), circleEdge, output);
}

//--------------------------------------------
// Pixel shader
//--------------------------------------------

float4 PSMain(GS_OUTPUT input) : SV_TARGET
{
    PointState ps = PointStates[input.pointId];
    GroupState gs = GroupStates[ps.GroupId];

    //--------------------------------------------
    // Visibility
    //--------------------------------------------

    if ((gs.Flags & GROUP_VISIBLE) == 0u)
        discard;

    if ((ps.Flags & POINT_VISIBLE) == 0u)
        discard;

    //--------------------------------------------
    // Interaction
    //--------------------------------------------

    bool selected = (ps.Flags & POINT_SELECTED) != 0u;
    bool mouseOver = (ps.Flags & POINT_MOUSEOVER) != 0u;

    if (!selected && !mouseOver)
        discard;

    //--------------------------------------------
    // Marker-relative distance
    //--------------------------------------------

    float2 markerPosition = input.offset / input.circleEdge;
    float markerDistance = length(markerPosition);
    bool insideMarker = markerDistance <= 1.0f;

    //--------------------------------------------
    // Expanded-quad distance
    //--------------------------------------------

    float outerDistance = length(input.offset);

    if (outerDistance > 1.0f)
        discard;

    //--------------------------------------------
    // Normalize distance outside marker:
    //
    // 0 = original marker edge
    // 1 = full GlowPixelOffset
    //--------------------------------------------

    float markerEdge = length(input.circleEdge);
    float glowT = saturate((outerDistance - markerEdge) / max(1.0f - markerEdge, 1e-6f));

    //--------------------------------------------
    // Selected
    //--------------------------------------------

    if (selected)
    {
        float3 selectionColor = SelectedColor.rgb;

        //----------------------------------------
        // Selected + mouseover
        //----------------------------------------

        if (mouseOver)
            selectionColor = lerp(selectionColor, float3(0.0f, 0.0f, 0.0f), 0.35f);

        //----------------------------------------
        // Interior selection tint
        //----------------------------------------

        if (insideMarker)
            return float4(selectionColor, SelectedColor.a);

        //----------------------------------------
        // Selection = half-width halo
        // Mouseover = full-width halo
        //----------------------------------------

        float mouseOverGlowRadius = 1.0f;
        float selectionGlowRadius = mouseOverGlowRadius * 0.5f;
        float glowRadius = mouseOver ? mouseOverGlowRadius : selectionGlowRadius;

        if (glowT >= glowRadius)
        {
            discard;
        }

        float normalizedGlow = saturate(glowT / glowRadius);
        float glowAlpha = 1.0f - smoothstep(0.0f, 1.0f, normalizedGlow);

        glowAlpha = pow(glowAlpha, 0.5f);
        glowAlpha *= SelectedColor.a;

        return float4(selectionColor, glowAlpha);
    }

    //--------------------------------------------
    // Mouseover only
    //--------------------------------------------

    if (insideMarker)
        discard;

    float glowAlpha = 1.0f - smoothstep(0.0f, 1.0f, glowT);
    glowAlpha = pow(glowAlpha, 0.5f);
    glowAlpha *= 0.4f;

    return float4(0.0f, 0.0f, 0.0f, glowAlpha);
}