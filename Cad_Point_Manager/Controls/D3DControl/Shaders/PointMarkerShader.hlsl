// PointMarkerShader.hlsl

cbuffer TransformationBuffer : register(b0)
{
    row_major matrix transformationMatrix;
};

cbuffer CircleSettingsBuffer : register(b1)
{
    float4 selectedColor;
    float4 selectedMouseOverColor;
};

// NEW: state buffers (same slots as glyphs)
struct LabelState
{
    float2 Offset;
    uint Flags;
    float Pad;
};
struct GroupState
{
    float4 Color;
    float Scale;
    uint Flags;
    float2 Pad;
};

StructuredBuffer<LabelState> LabelStates : register(t0);
StructuredBuffer<GroupState> GroupStates : register(t1);

static const uint LABEL_VISIBLE = 1u;
static const uint LABEL_SELECTED = 2u;
static const uint LABEL_MOUSEOVER = 4u;
static const uint GROUP_VISIBLE = 1u;


// --- Vertex/Geometry interfaces ---
// Strip per-vertex color and flags; *add* ids
struct VS_INPUT
{
    float3 position : POSITION;
    float radius : RADIUS;
    uint labelId : LABEL_ID;
    uint groupId : GROUP_ID;
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
    uint lid = input[0].labelId;
    uint gid = input[0].groupId;
    GroupState gs = GroupStates[gid];
    LabelState ls = LabelStates[lid];

    // Visibility
    bool visGrp = (gs.Flags & GROUP_VISIBLE) != 0u;
    bool visLbl = (ls.Flags & LABEL_VISIBLE) != 0u;
    if (!visGrp || !visLbl)
    {
        return;
    }

    // Color from group, then apply hover/selected
    float4 color = gs.Color;
    bool over = (ls.Flags & LABEL_MOUSEOVER) != 0u;
    bool sel = (ls.Flags & LABEL_SELECTED) != 0u;

    if (sel)
    {
        color = over ? selectedMouseOverColor : selectedColor;
    }

    // Scale radius by group scale (Option A)
    float radiusWorld = input[0].radius * gs.Scale;

    float4 center = mul(float4(input[0].position, 1), transformationMatrix);
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
