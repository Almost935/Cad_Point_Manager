// GlyphMeshShader.hlsl

cbuffer TransformationBuffer : register(b0)
{
    row_major float4x4 transformationMatrix; // world*view*proj
}

cbuffer TextSettingsBuffer : register(b1)
{
    float4 selectedColor;
    float4 selectedMouseOverColor;
}

cbuffer ViewportBuffer : register(b2)
{
    float2 ViewportSize;
    float2 _pad;
}

// ---------- NEW: Indirection state buffers ----------
struct LabelState
{
    float2 Offset; // world-space drag delta
    uint Flags; // bit0: visible
    float _padLS; // keep 16B stride
};
struct PointState
{
    float2 Offset; // world-space drag delta
    uint Flags; // bit0: visible bit1: selected, bit2: mouseOver, bit3: hasLeaderLine, bit4: mouseOverAnchor, bit5: anchorPressed, bit6: isFlippedY, bit7: isFlippedX
    float _padLS; // keep 16B stride
};
struct GroupState
{
    float4 Color; // rgba
    float Scale; // point-scale
    uint Flags; // bit0: visible
    float TextInfoBaseXoffset; // distance between base position and text labels
    float _padGS; // keep 16B stride
};

// Bind these to t0/t1 (match your C# SetShaderResource slots)
StructuredBuffer<LabelState> LabelStates : register(t0);
StructuredBuffer<PointState> PointStates : register(t1);
StructuredBuffer<GroupState> GroupStates : register(t2);

// Per-vertex stream (slot 0): glyph mesh vertices in DESIGN UNITS
struct VSInPerVertex
{
    float2 PosDU : POSITION; // triangle-list vertices, DU space (+Y up per DWrite)
};

// Per-instance stream (slot 1)
struct VSInPerInstance
{
    float2 OriginWorld : GLYPH_ORIGIN; // baseline origin in world units
    float DuToWorld : GLYPH_SCALE; // base worldUnits per design-unit (pre-group scale)
    float PenDU : GLYPH_PEN; // horizontal pen advance in DU
    float YSign : YSIGN; // +1 or -1 (flip Y if needed)
    uint LabelId : LABEL_ID; // per text line (PN/Elev/Desc)
    uint GroupId : GROUP_ID; // owning PointGroup
    uint PointId : POINT_ID;
};

struct VSOut
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
    float Visible : TEXCOORD0; // carry to PS for clip
};

// Pixel-snap helper (unchanged)
float2 ComputeSnapNdc(float2 viewportSize)
{
    float4 o = mul(float4(0, 0, 0, 1), transformationMatrix);
    float2 ndc = o.xy / o.w;
    float2 pix = (ndc * 0.5f + 0.5f) * viewportSize;
    float2 tgt = floor(pix) + 0.5f;
    float2 dp = tgt - pix;
    return (dp / viewportSize) * 2.0f;
}

// Bit masks for flags (keep in sync with CPU)
static const uint LABEL_VISIBLE = 1u << 0;

static const uint POINT_VISIBLE = 1u << 0;
static const uint POINT_SELECTED = 1u << 1;
static const uint POINT_MOUSEOVR = 1u << 2;
static const uint POINT_ISFLIPPEDY = 1u << 6;
static const uint POINT_ISFLIPPEDX = 1u << 7;

static const uint GROUP_VISIBLE = 1u << 0;

VSOut VSMain(VSInPerVertex v, VSInPerInstance inst)
{
    VSOut o;

    // --- Fetch dynamic state ---
    LabelState ls = LabelStates[inst.LabelId];
    PointState ps = PointStates[inst.PointId];
    GroupState gs = GroupStates[inst.GroupId];

    float visLbl = ((ls.Flags & LABEL_VISIBLE) != 0u) ? 1.0f : 0.0f;
    float visPt = ((ps.Flags & POINT_VISIBLE) != 0u) ? 1.0f : 0.0f;
    float visGrp = ((gs.Flags & GROUP_VISIBLE) != 0u) ? 1.0f : 0.0f;
    float visible = visLbl * visGrp * visPt;
    
    // Selection / hover
    float sel = ((ps.Flags & POINT_SELECTED) != 0u) ? 1.0f : 0.0f;
    float mo = ((ps.Flags & POINT_MOUSEOVR) != 0u) ? 1.0f : 0.0f;
    
    // Flipped axis
    float isFlippedY = ((ps.Flags & POINT_ISFLIPPEDY) != 0u) ? -1.0f : 1.0f;
    float isFlippedX = ((ps.Flags & POINT_ISFLIPPEDX) != 0u) ? -1.0f : 1.0f;
    
    // Set text info offset sign based on flippedX
    float textInfoOffset = gs.TextInfoBaseXoffset * isFlippedY;
    
    // --- Position math ---
    // Apply label drag offset and group scale. Group scale only applicable to label y offset.
    float x = inst.OriginWorld.x + ls.Offset.x + textInfoOffset + ps.Offset.x;
    float y = inst.OriginWorld.y + (ls.Offset.y * gs.Scale) + ps.Offset.y;
    float2 originWorld = float2(x, y);

    float duToWorld = inst.DuToWorld * gs.Scale;

    // Convert DU -> world, apply pen advance on X, optional Y sign flip
    float2 world;
    world.x = originWorld.x + (inst.PenDU + v.PosDU.x) * duToWorld;
    world.y = originWorld.y + (v.PosDU.y * inst.YSign) * duToWorld;

    float4 clip = mul(float4(world, 0, 1), transformationMatrix);

    // screen-space pixel snap (uniform)
    float2 snap = ComputeSnapNdc(ViewportSize);
    clip.xy += snap * clip.w;

    // --- Color/tint ---
    float4 col = gs.Color;
    col.rgb = col.rgb;
    col.a = col.a;

    if (sel > 0.5f)
    {
        col = selectedColor;
    }

    o.Position = clip;
    o.Color = col;
    o.Visible = visible;
    return o;
}

float4 PSMain(VSOut i) : SV_Target
{
    // Hard-clip invisible glyphs early
    if (i.Visible < 0.5f)
    {
        clip(-1);
    }
    return i.Color; // premultiplied alpha recommended
}
