// TextShader_Instanced.hlsl
// D3D11 / HLSL 5.0
// Draws tessellated glyph meshes via instancing.
// Slot 0: per-vertex glyph geometry in DESIGN UNITS (float2 POSITION)
// Slot 1: per-instance placement/appearance (see VSInInst below)

// -----------------------------------------------------------------------------
// Constant buffers (keep register bindings identical to your app)
// -----------------------------------------------------------------------------
cbuffer TransformationBuffer : register(b0)
{
    row_major float4x4 transformationMatrix; // world * view * proj
};

cbuffer TextSettingsBuffer : register(b1)
{
    float4 selectedColor; // color when selected
    float4 selectedMouseOverColor; // color when selected + mouseover
};

cbuffer ViewportBuffer : register(b2)
{
    float2 ViewportSize; // (width, height) in pixels
    float2 _padViewport;
};

// -----------------------------------------------------------------------------
// Helpers
// -----------------------------------------------------------------------------
float4 GetSnappedColor(float4 color)
{
    // Lighten toward bluish to indicate hover; tweak as desired
    float3 hoverTint = float3(0.4, 0.4, 1.0);
    float3 rgb = lerp(color.rgb, hoverTint, 0.7);
    return float4(rgb, color.a);
}

// Compute a uniform NDC offset so that the world origin is aligned to pixel centers.
// This yields stable 1px edges without per-vertex snapping.
float2 ComputeSnapDeltaNdc(float2 viewportSize)
{
    float4 originClip = mul(float4(0, 0, 0, 1), transformationMatrix);
    float2 originNdc = originClip.xy / originClip.w;
    float2 originPix = (originNdc * 0.5f + 0.5f) * viewportSize;

    float2 targetPix = floor(originPix) + 0.5f;
    float2 deltaPix = targetPix - originPix;

    // pixels -> NDC
    return (deltaPix / viewportSize) * 2.0f;
}

// -----------------------------------------------------------------------------
// Vertex / Instance inputs
// -----------------------------------------------------------------------------

// Slot 0: per-vertex glyph triangles in design units (DU)
struct VSInVertex
{
    float2 DuPos : POSITION; // glyph-local xy in design units
};

// Slot 1: per-instance data for each glyph placement
// IMPORTANT: Your InputLayout must map these semantics on slot=1 with
// InputClassification.PerInstanceData and step rate = 1.
struct VSInInst
{
    float2 Origin : ORIGIN; // baseline origin in WORLD UNITS
    float DuToWorld : DUTOWORLD; // world units per design unit (scale)
    float YSign : YSIGN; // +1 or -1 to flip Y if needed
    float PenDU : PENDU; // horizontal pen advance (in DU) before this glyph
    float4 Color : COLOR; // base color
    float IsVisible : ISVISIBLE; // 1/0
    float IsMouseOver : ISMOUSEOVER; // 1/0
    float IsSelected : ISSELECTED; // 1/0
};

struct VSOut
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
};

// -----------------------------------------------------------------------------
// Vertex Shader
// -----------------------------------------------------------------------------
VSOut VSMain(VSInVertex v, VSInInst inst)
{
    VSOut o;

    // Convert design units to world units and place relative to label origin.
    // X uses (pen advance + glyph local X), Y uses glyph local Y (optionally flipped by YSign).
    float2 worldXY;
    worldXY.x = inst.Origin.x + (inst.PenDU + v.DuPos.x) * inst.DuToWorld;
    worldXY.y = inst.Origin.y + (v.DuPos.y * inst.YSign) * inst.DuToWorld;

    // World -> Clip
    float4 clip = mul(float4(worldXY, 0.0, 1.0), transformationMatrix);

    // Color/selection/visibility
    float4 col = inst.Color;
    if (inst.IsMouseOver > 0.5)
        col = GetSnappedColor(col);
    if (inst.IsSelected > 0.5)
        col = (inst.IsMouseOver > 0.5) ? selectedMouseOverColor : selectedColor;
    if (inst.IsVisible < 0.5)
        col.a = 0.0;

    // Uniform pixel snap for the whole draw (stable edges)
    float2 snapNdc = ComputeSnapDeltaNdc(ViewportSize);
    clip.xy += snapNdc * clip.w;

    o.Position = clip;
    o.Color = col;
    return o;
}

// -----------------------------------------------------------------------------
// Pixel Shader
// -----------------------------------------------------------------------------
float4 PSMain(VSOut i) : SV_TARGET
{
    // If you use premultiplied alpha, ensure blend state is set accordingly.
    // Optional edge softening (remove if you prefer crisp triangles):
    // float edgeFade = smoothstep(0.0, 0.1, i.Color.a);
    // return float4(i.Color.rgb, i.Color.a * edgeFade);
    return i.Color;
}
