// PointCircleShader.hlsl

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
    float2 _pad2;
};

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
    float4 Color; // rgba
    float Scale; // point-scale
    uint Flags; // bit0: visible
    float TextInfoBaseXoffset; // distance between base position and text labels
    float _padGS; // keep 16B stride
};

// --- Vertex/Geometry interfaces ---
// Strip per-vertex color and flags; *add* ids
struct VS_INPUT
{
    uint pointId : POINT_ID;
};

struct GS_OUTPUT
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
    float2 offset : TEXCOORD0;
};

StructuredBuffer<PointState> PointStates : register(t0);
StructuredBuffer<GroupState> GroupStates : register(t1);

static const uint POINT_VISIBLE = 1u << 0;
static const uint POINT_SELECTED = 1u << 1;
static const uint POINT_MOUSEOVR = 1u << 2;
static const uint GROUP_VISIBLE = 1u;

VS_INPUT VSMain(VS_INPUT input)
{
    return input;
}

// Same EmitCorner as before, minus the flag fields
void EmitCorner(float4 color, float4 position, float2 offset, inout TriangleStream<GS_OUTPUT> output)
{
    GS_OUTPUT o;
    o.position = position;
    o.offset = offset;
    o.color = color;
    output.Append(o);
}

[maxvertexcount(4)]
void GSMain(point VS_INPUT input[1], inout TriangleStream<GS_OUTPUT> output)
{
    PointState ps = PointStates[input[0].pointId];
    GroupState gs = GroupStates[ps.GroupId];

    bool visGrp = (gs.Flags & GROUP_VISIBLE) != 0u;
    bool visPt = (ps.Flags & POINT_VISIBLE) != 0u;

    if (!visGrp || !visPt)
        return;

    float4 color = gs.Color;

    float radiusWorld = CogoPointCircleRadiusPixels * gs.Scale;

    float4 center = mul(float4(ps.Offset.xy, 0.0f, 1.0f),transformationMatrix);

    float radiusX =radiusWorld * transformationMatrix._11;
    float radiusY =radiusWorld * transformationMatrix._22;

    EmitCorner(
        color,float4(center.x - radiusX,center.y + radiusY, 0, 1),float2(-1, 1), output);
    EmitCorner(
        color,float4(center.x - radiusX,center.y - radiusY, 0, 1),float2(-1, -1), output);
    EmitCorner(
        color,float4(center.x + radiusX,center.y + radiusY, 0, 1),float2(1, 1), output);
    EmitCorner(
        color,float4(center.x + radiusX,center.y - radiusY, 0, 1),float2(1, -1), output);
}

float4 PSMain(GS_OUTPUT input) : SV_TARGET
{
    float dist = length(input.offset);
    if (dist > 1.0f)
    {
        discard;
    }
    return input.color;
}
