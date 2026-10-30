cbuffer TransformationBuffer : register(b0)
{
    row_major float4x4 transformationMatrix;
}

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

cbuffer MsdfSettings : register(b2)
{
    float AtlasWidth;
    float AtlasHeight;
    float DistanceRange;
    float CameraZoom;
}

struct LabelState
{
    float2 Offset;
    uint Flags;
};
struct PointState
{
    float2 Offset;
    float2 PointInfoOffset;
    uint GroupId;
    uint Flags;
    uint LabelQuadrant;
};
struct GroupState
{
    float4 Color;
    float Scale;
    uint Flags;
    float TextInfoBaseXoffset;
};

StructuredBuffer<LabelState> LabelStates : register(t0);
StructuredBuffer<PointState> PointStates : register(t1);
StructuredBuffer<GroupState> GroupStates : register(t2);

Texture2D FontAtlas : register(t3);

SamplerState FontSampler : register(s0);

struct VSVertex
{
    float2 Corner : POSITION;
};

struct VSInstance
{
    float EmToWorld : EM_TO_WORLD;
    float PenX : PEN_X;
    float YSign : YSIGN;
    uint LabelId : LABEL_ID;
    uint PointId : POINT_ID;
    float2 PlaneOrigin : PLANE_ORIGIN;
    float2 PlaneSize : PLANE_SIZE;
    float2 UvOrigin : UV_ORIGIN;
    float2 UvSize : UV_SIZE;
};

struct VSOut
{
    float4 Position : SV_POSITION;
    float2 UV : TEXCOORD0;
    float4 Color : COLOR;
    nointerpolation float Visible : TEXCOORD1;
    nointerpolation uint PointFlags : TEXCOORD2;
    nointerpolation float GroupScale : TEXCOORD3;
};

float Median(float r, float g, float b)
{
    return max(min(r, g), min(max(r, g), b));
}
float4 ScreenPxRange(float2 uv)
{
    float2 fw = fwidth(uv);
    return float4(fw.x * 1000, fw.y * 1000, 0, 1);
}
float3 GetContrastingGlowColor(float3 glyphColor)
{
    float luminance = dot(glyphColor, float3(0.2126f, 0.7152f, 0.0722f));
    float brightness = 1.0f - smoothstep(0.25f, 0.65f, luminance);

    return float3(brightness, brightness, brightness);
}
float GetSeparationStrength(float3 glyphColor)
{
    // Detect how dark the color is
    float brightness = max(
        glyphColor.r,
        max(glyphColor.g, glyphColor.b)
    );

    // Detect how close the color is to grayscale
    float minChannel = min(
        glyphColor.r,
        min(glyphColor.g, glyphColor.b)
    );

    float saturation = brightness - minChannel;

    // Only very dark colors should receive separation
    float darkness = 1.0f -
        smoothstep(0.15f, 0.35f, brightness);

    // Suppress separation for saturated colors
    float grayscale = 1.0f -
        smoothstep(0.05f, 0.15f, saturation);

    return darkness * grayscale;
}

// Bit masks for flags (keep in sync with CPU)
static const uint LABEL_VISIBLE = 1u << 0;

static const uint POINT_VISIBLE = 1u << 0;
static const uint POINT_SELECTED = 1u << 1;
static const uint POINT_MOUSEOVER = 1u << 2;
static const uint POINT_ISFLIPPEDY = 1u << 6;
static const uint POINT_ISFLIPPEDX = 1u << 7;

static const uint QUADRANT_UPPER_RIGHT = 0u;
static const uint QUADRANT_UPPER_LEFT = 1u;
static const uint QUADRANT_LOWER_LEFT = 2u;
static const uint QUADRANT_LOWER_RIGHT = 3u;

static const uint GROUP_VISIBLE = 1u << 0;


VSOut VSMain(VSVertex v, VSInstance inst)
{
    VSOut o;

    LabelState ls = LabelStates[inst.LabelId];
    PointState ps = PointStates[inst.PointId];
    GroupState gs = GroupStates[ps.GroupId];

    float visLbl = ((ls.Flags & LABEL_VISIBLE) != 0u) ? 1.0f : 0.0f;
    float visPt = ((ps.Flags & POINT_VISIBLE) != 0u) ? 1.0f : 0.0f;
    float visGrp = ((gs.Flags & GROUP_VISIBLE) != 0u) ? 1.0f : 0.0f;
    float visible = visLbl * visGrp * visPt;

    if (visible < 0.5f)
    {
        o.Position = float4(0, 0, 0, 0);
        o.UV = 0;
        o.Color = 0;
        o.Visible = 0;
        o.GroupScale = 0;
        return o;
    }

    o.Visible = visible;
    o.PointFlags = ps.Flags;
    o.GroupScale = gs.Scale;

    float isLabelLeft = (ps.LabelQuadrant == QUADRANT_UPPER_LEFT || ps.LabelQuadrant == QUADRANT_LOWER_LEFT) ? -1.0f : 1.0f;

    float2 corner = v.Corner + 0.5;
    float2 local = inst.PlaneOrigin + corner * inst.PlaneSize;

    local.x += inst.PenX;
    local.y *= inst.YSign;

    local *= (inst.EmToWorld * gs.Scale);

    float textInfoOffset = gs.TextInfoBaseXoffset * isLabelLeft;

    float2 origin;
    origin.x = ps.Offset.x + ps.PointInfoOffset.x + ls.Offset.x + textInfoOffset;
    origin.y = ps.Offset.y + ps.PointInfoOffset.y + ls.Offset.y * gs.Scale;

    local += origin;

    o.Position = mul(float4(local, 0, 1), transformationMatrix);
    o.UV = lerp(inst.UvOrigin, inst.UvOrigin + inst.UvSize, corner);
    o.Color = gs.Color;

    return o;
}


float4 PSMain(VSOut input) : SV_Target
{
    if (input.Visible < 0.5f)
    {
        clip(-1);
    }

    float3 msd = FontAtlas.Sample(FontSampler, input.UV).rgb;
    float sd = Median(msd.r, msd.g, msd.b);
    float d = (sd - 0.5f) * DistanceRange;

    bool selected = (input.PointFlags & POINT_SELECTED) != 0u;
    bool mouseOver = (input.PointFlags & POINT_MOUSEOVER) != 0u;

    if (!selected && !mouseOver)
    {
        discard;
    }

    float fill = smoothstep(-0.25f, 0.5f, d);

    //---------------------------------------
    // Glow radius
    //---------------------------------------

    float effectiveScale = max(input.GroupScale, 0.01f);
    float mouseOverGlowRadius = clamp(
        120.0f / (CameraZoom * effectiveScale),
        0.01f, DistanceRange * 0.5f);

    float selectionGlowRadius = mouseOverGlowRadius * 0.5f;
    float glowRadius = mouseOver ? mouseOverGlowRadius : selectionGlowRadius;

    //---------------------------------------
    // Original glow falloff
    //---------------------------------------

    float outsideDistance = max(-d, 0.0f);
    float halo = 1.0f - smoothstep(0.0f, glowRadius, outsideDistance);
    float outsideMask = 1.0f - smoothstep(-0.25f, 0.25f, d);

    halo *= outsideMask;

    //---------------------------------------
    // Selected point
    //---------------------------------------

    if (selected)
    {
        float3 selectionColor = SelectedColor.rgb;

        if (mouseOver)
        {
            selectionColor = lerp(selectionColor, float3(0, 0, 0), 0.35f);
        }

        float tintAlpha = fill * SelectedColor.a;
        float glowAlpha = halo * SelectedColor.a;

        float alpha = max(tintAlpha, glowAlpha);

        return float4(selectionColor, alpha);
    }

   
    //---------------------------------------
    // Unselected mouseover
    //---------------------------------------

    float separationStrength =
        GetSeparationStrength(input.Color.rgb);

    // Black for most colors, dark gray for
    // black and very dark gray glyphs.
    float3 glowColor = lerp(
        float3(0.0f, 0.0f, 0.0f),
        float3(0.45f, 0.45f, 0.45f),
        separationStrength
    );

    // Increase glow opacity for dark grayscale glyphs.
    float glowOpacity = lerp(
        0.55f,
        0.90f,
        separationStrength
    );

    // Increase the interior tint for dark glyphs.
    float fillOpacity = lerp(
        0.20f,
        0.65f,
        separationStrength
    );

    // Preserve original glow geometry and falloff.
    float alpha = halo * glowOpacity
                + fill * fillOpacity;

    alpha = saturate(alpha);

    return float4(glowColor, alpha);

}
