// LineGlowShader.hlsl

//-----------------------------------------------------------------------------
// Constant buffers
//-----------------------------------------------------------------------------

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
};

//-----------------------------------------------------------------------------
// Vertex input
//-----------------------------------------------------------------------------

struct VSInput
{
    float2 Local : LOCAL;
};

struct VSInstance
{
    float2 Start : START;
    float2 End : END;

    uint LayerId : LAYERID;
    uint LineTypeId : LINETYPEID;
};

//-----------------------------------------------------------------------------
// Pixel input
//-----------------------------------------------------------------------------

struct PSInput
{
    float4 Position : SV_POSITION;

    // -1 at one edge of the expanded glow quad,
    // +1 at the opposite edge.
    float Side : TEXCOORD0;

    // Distance along the original line in world units.
    float Distance : TEXCOORD1;

    // Original line length in world units.
    float LineLength : TEXCOORD2;

    nointerpolation uint LayerId : TEXCOORD3;
    nointerpolation uint LineTypeId : TEXCOORD4;
};

//-----------------------------------------------------------------------------
// GPU state
//-----------------------------------------------------------------------------

struct LayerState
{
    float4 Color;
    uint Flags;
    float3 Padding;
};

struct LineTypeInfo
{
    uint FirstPatternIndex;
    uint PatternCount;
    float PatternLength;
    float Padding;
};

StructuredBuffer<LayerState> LayerStates : register(t0);
StructuredBuffer<LineTypeInfo> LineTypeInfos : register(t1);
StructuredBuffer<float> PatternData : register(t2);

static const uint LAYER_VISIBLE = 1u << 0;

//-----------------------------------------------------------------------------
// Vertex shader
//-----------------------------------------------------------------------------

PSInput VSMain(VSInput vertex, VSInstance instance)
{
    PSInput output;

    float4 clipStart = mul(float4(instance.Start, 0.0, 1.0), transformationMatrix);

    float4 clipEnd = mul(float4(instance.End, 0.0, 1.0), transformationMatrix);

    float2 ndcStart = clipStart.xy / clipStart.w;
    float2 ndcEnd = clipEnd.xy / clipEnd.w;

    float2 pixelScale = float2(
        ViewportSize.x * 0.5,
        ViewportSize.y * 0.5);

    float2 dirPixels = (ndcEnd - ndcStart) * pixelScale;

    float pixelLength = length(dirPixels);

    if (pixelLength < 1e-6)
    {
        dirPixels = float2(1.0, 0.0);
    }
    else
    {
        dirPixels /= pixelLength;
    }

    // Perpendicular direction in screen space.
    float2 normalPixels = float2(-dirPixels.y, dirPixels.x);

    float2 normalNdc = float2(
        normalPixels.x * (2.0 / ViewportSize.x),
        normalPixels.y * (2.0 / ViewportSize.y));

    float glowHalfWidthPixels = LineHalfWidthPixels + GlowPixelOffset;

    float2 sideOffset = normalNdc * glowHalfWidthPixels;

    float t = vertex.Local.y;

    float2 ndc = lerp(ndcStart, ndcEnd, t);

    // Expand perpendicular to the line.
    ndc += sideOffset * vertex.Local.x;

    float4 clip = lerp(clipStart, clipEnd, t);

    clip.xy = ndc * clip.w;

    output.Position = clip;
    output.Side = vertex.Local.x;

    float lineLength = length(instance.End - instance.Start);

    output.Distance = t * lineLength;

    output.LineLength = lineLength;

    output.LayerId = instance.LayerId;

    output.LineTypeId = instance.LineTypeId;

    return output;
}

//-----------------------------------------------------------------------------
// Pixel shader
//-----------------------------------------------------------------------------

float4 PSMain(PSInput input) : SV_TARGET
{
    LayerState ls = LayerStates[input.LayerId];

    if ((ls.Flags & LAYER_VISIBLE) == 0)
        discard;
    
    LineTypeInfo lti = LineTypeInfos[input.LineTypeId];
    bool visible = true;

    // Protect against malformed/empty patterns.
    if (lti.PatternCount > 0 &&
        lti.PatternLength > 1e-6)
    {
        float scaledDistance = input.Distance / GlobalLineTypeScale;

        float patternPos = fmod(scaledDistance, lti.PatternLength);

        if (patternPos < 0.0)
            patternPos += lti.PatternLength;

        float accum = 0.0;

        visible = true;

        for (uint i = 0; i < lti.PatternCount; i++)
        {
            float segment = PatternData[lti.FirstPatternIndex + i];

            float segmentLength = abs(segment);

            if (patternPos < accum + segmentLength)
            {
                visible = segment > 0.0;
                break;
            }

            accum += segmentLength;
        }
    }

    if (!visible)
        discard;

    float glowHalfWidthPixels = LineHalfWidthPixels + GlowPixelOffset;

    float distanceFromCenterPixels = abs(input.Side) * glowHalfWidthPixels;

    if (distanceFromCenterPixels <= LineHalfWidthPixels)
        discard;
    
    float distanceFromLineEdge = distanceFromCenterPixels - LineHalfWidthPixels;

    float glowT = saturate(distanceFromLineEdge / GlowPixelOffset);
    float glowAlpha = 1.0 - smoothstep(0.0, 1.0, glowT);
    const float MaxGlowAlpha = 0.45;

    glowAlpha *= MaxGlowAlpha;

    float edgeWidth = max(fwidth(glowT), 1e-5);
    float outerAA = 1.0 - smoothstep(1.0 - edgeWidth, 1.0, glowT);

    glowAlpha *= outerAA;

    return float4(0.0, 0.0, 0.0, glowAlpha);
}
