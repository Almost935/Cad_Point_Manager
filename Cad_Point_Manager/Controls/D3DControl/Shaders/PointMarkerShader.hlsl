// PointMarkerShader.hlsl

cbuffer TransformationBuffer : register(b0)
{
    row_major matrix transformationMatrix;
};

cbuffer CircleSettingsBuffer : register(b1)
{
    float4 selectedColor;
    float4 selectedMouseOverColor;
    float HalfWidth;
    float3 Padding;
};

struct PointState
{
    float2 Offset; // world-space drag delta
    float2 PointInfoOffset; // Text info offset in world units
    uint GroupId; // index into GroupState buffer
    uint Flags; // bit0: visible bit1: selected, bit2: mouseOver, bit3: hasLeaderLine, bit4: mouseOverAnchor, bit5: anchorPressed, bit6: isFlippedY, bit7: isFlippedX
    float2 _padLS; // keep 16B stride
};
struct GroupState
{
    float4 Color; // rgba
    float Scale; // point-scale
    uint Flags; // bit0: visible
    float TextInfoBaseXoffset; // distance between base position and text labels
    float _padGS; // keep 16B stride
};

StructuredBuffer<PointState> PointStates : register(t0);
StructuredBuffer<GroupState> GroupStates : register(t1);

static const uint POINT_VISIBLE = 1u << 0;
static const uint POINT_SELECTED = 1u << 1;
static const uint POINT_MOUSEOVR = 1u << 2;
static const uint GROUP_VISIBLE = 1u;

// --- Vertex/Geometry interfaces ---
// Strip per-vertex color and flags; *add* ids
struct VS_INPUT
{
    float3 position : POSITION;
    float radius : RADIUS;
    uint labelId : LABEL_ID;
    uint pointId : POINT_ID;
};

struct GS_OUTPUT
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
    float2 offset : TEXCOORD0;
};

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

    // Visibility
    bool visGrp = (gs.Flags & GROUP_VISIBLE) != 0u;
    bool visPt = (ps.Flags & POINT_VISIBLE) != 0u;
    if (!visGrp || !visPt)
    {
        return;
    }

    // Color from group, then apply hover/selected
    float4 color = gs.Color;
    bool over = (ps.Flags & POINT_MOUSEOVR) != 0u;
    bool sel = (ps.Flags & POINT_SELECTED) != 0u;

    if (sel) { color = over ? selectedMouseOverColor : selectedColor; }

    // Scale radius by group scale (Option A)
    float radiusWorld = input[0].radius * gs.Scale;

    float4 center = mul(float4(input[0].position + float3(ps.Offset.xy, 0), 1), transformationMatrix);
    float radiusX = radiusWorld * transformationMatrix._11;
    float radiusY = radiusWorld * transformationMatrix._22;

    EmitCorner(color, float4(center.x - radiusX, center.y + radiusY, 0, 1), float2(-1, 1), output);
    EmitCorner(color, float4(center.x - radiusX, center.y - radiusY, 0, 1), float2(-1, -1), output);
    EmitCorner(color, float4(center.x + radiusX, center.y + radiusY, 0, 1), float2(1, 1), output);
    EmitCorner(color, float4(center.x + radiusX, center.y - radiusY, 0, 1), float2(1, -1), output);
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
