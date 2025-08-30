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

// Per-vertex stream (slot 0): glyph mesh vertices in DESIGN UNITS
struct VSInPerVertex
{
    float2 PosDU : POSITION; // triangle-list vertices, DU space (+Y up per DWrite)
};

// Per-instance stream (slot 1)
struct VSInPerInstance
{
    float2 OriginWorld : GLYPH_ORIGIN; // baseline origin in world units
    float DuToWorld : GLYPH_SCALE; // worldUnits per design-unit
    float PenDU : GLYPH_PEN; // horizontal pen advance in DU
    float4 Color : COLOR;
    float IsVisible : ISVISIBLE;
    float IsMouseOver : ISMOUSEOVER;
    float IsSelected : ISSELECTED;
    float YSign : YSIGN; // +1 or -1 (flip Y if needed)
};

struct VSOut
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
};

float2 ComputeSnapNdc(float2 viewportSize)
{
    float4 o = mul(float4(0, 0, 0, 1), transformationMatrix);
    float2 ndc = o.xy / o.w;
    float2 pix = (ndc * 0.5f + 0.5f) * viewportSize;
    float2 tgt = floor(pix) + 0.5f;
    float2 dp = tgt - pix;
    return (dp / viewportSize) * 2.0f;
}

VSOut VSMain(VSInPerVertex v, VSInPerInstance inst)
{
    VSOut o;

    // Convert DU -> world, apply pen advance on X, optional Y sign flip
    float2 world;
    world.x = inst.OriginWorld.x + (inst.PenDU + v.PosDU.x) * inst.DuToWorld;
    world.y = inst.OriginWorld.y + (v.PosDU.y * inst.YSign) * inst.DuToWorld;

    float4 clip = mul(float4(world, 0, 1), transformationMatrix);

    // screen-space pixel snap (uniform)
    float2 snap = ComputeSnapNdc(ViewportSize);
    clip.xy += snap * clip.w;

    float4 col = inst.Color;
    if (inst.IsMouseOver > 0.5)
        col = lerp(col, float4(0.4, 0.4, 1, 1), 0.7);
    if (inst.IsSelected > 0.5)
        col = (inst.IsMouseOver > 0.5) ? selectedMouseOverColor : selectedColor;
    if (inst.IsVisible < 0.5)
        col.a = 0.0;

    o.Position = clip;
    o.Color = col;
    return o;
}

float4 PSMain(VSOut i) : SV_Target
{
    return i.Color; // premultiplied alpha recommended
}
