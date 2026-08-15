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

    float Side : TEXCOORD0;

    float Distance : TEXCOORD1;
    float LineLength : TEXCOORD2;

    nointerpolation uint LayerId : TEXCOORD3;
    nointerpolation uint LineTypeId : TEXCOORD4;

    float AlongPixels : TEXCOORD5;

    nointerpolation float LineLengthPixels : TEXCOORD6;
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

    //--------------------------------------------
    // Transform endpoints
    //--------------------------------------------

    float4 clipStart = mul(float4(instance.Start, 0.0, 1.0), transformationMatrix);

    float4 clipEnd = mul(float4(instance.End, 0.0, 1.0), transformationMatrix);

    float2 ndcStart = clipStart.xy / clipStart.w;
    float2 ndcEnd = clipEnd.xy / clipEnd.w;

    //--------------------------------------------
    // Screen-space line direction
    //--------------------------------------------

    float2 pixelScale = float2(
        ViewportSize.x * 0.5,
        ViewportSize.y * 0.5);

    float2 dirPixels = (ndcEnd - ndcStart) * pixelScale;

    float lineLengthPixels = length(dirPixels);

    if (lineLengthPixels < 1e-6)
    {
        dirPixels = float2(1.0, 0.0);
    }
    else
    {
        dirPixels /= lineLengthPixels;
    }

    float2 normalPixels = float2(-dirPixels.y, dirPixels.x);

    //--------------------------------------------
    // Pixel directions -> NDC
    //--------------------------------------------

    float2 normalNdc = float2(
        normalPixels.x * (2.0 / ViewportSize.x),
        normalPixels.y * (2.0 / ViewportSize.y));

    float2 directionNdc = float2(
        dirPixels.x * (2.0 / ViewportSize.x),
        dirPixels.y * (2.0 / ViewportSize.y));

    //--------------------------------------------
    // Glow dimensions
    //--------------------------------------------

    // Match actual visible thickness from LineShader.
    float visibleLineHalfWidth = LineHalfWidthPixels * 0.5;

    float glowHalfWidth = visibleLineHalfWidth + GlowPixelOffset;

    //--------------------------------------------
    // Position along line
    //--------------------------------------------

    float t = vertex.Local.y;

    float2 ndc = lerp(ndcStart, ndcEnd, t);

    //--------------------------------------------
    // Extend glow past BOTH physical endpoints
    //--------------------------------------------

    float endDirection = vertex.Local.y * 2.0 - 1.0;

    ndc += directionNdc * GlowPixelOffset * endDirection;

    //--------------------------------------------
    // Expand sideways
    //--------------------------------------------

    ndc +=
        normalNdc *
        glowHalfWidth *
        vertex.Local.x;

    //--------------------------------------------
    // Convert back to clip
    //--------------------------------------------

    float4 clip =
        lerp(clipStart, clipEnd, t);

    clip.xy = ndc * clip.w;

    output.Position = clip;
    output.Side = vertex.Local.x;

    //--------------------------------------------
    // Line distances
    //--------------------------------------------

    output.AlongPixels = lerp(
        -GlowPixelOffset, lineLengthPixels + GlowPixelOffset, t);

    output.LineLengthPixels = lineLengthPixels;

    float lineLength = length(instance.End - instance.Start);

    output.LineLength = lineLength;

    float pixelsToWorld = lineLength / max(lineLengthPixels, 1e-6);

    output.Distance = output.AlongPixels * pixelsToWorld;

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

    float distanceToVisibleAlong = 0.0;

    if (lti.PatternCount > 0 && lti.PatternLength > 1e-6)
    {
        float scaledDistance = input.Distance / GlobalLineTypeScale;
        float patternPos = fmod(scaledDistance, lti.PatternLength);

        if (patternPos < 0.0)
            patternPos += lti.PatternLength;

        float accum = 0.0;

        for (uint i = 0; i < lti.PatternCount; i++)
        {
            float segment = PatternData[lti.FirstPatternIndex + i];
            float segmentLength = abs(segment);

            if (patternPos < accum + segmentLength)
            {
                visible = segment > 0.0;

                if (!visible)
                {
                    float distanceToGapStart = patternPos - accum;

                    float distanceToGapEnd = (accum + segmentLength) - patternPos;

                    distanceToVisibleAlong = min(distanceToGapStart, distanceToGapEnd);
                }

                break;
            }

            accum += segmentLength;
        }
    }
    
    float endpointLength = 2.0 * LineHalfWidthPixels;
    endpointLength /= GlobalLineTypeScale;

    if (input.Distance < endpointLength)
    {
        visible = true;
        distanceToVisibleAlong = 0.0;
    }

    if ((input.LineLength - input.Distance) < endpointLength)
    {
        visible = true;
        distanceToVisibleAlong = 0.0;
    }

    float visibleLineHalfWidth = LineHalfWidthPixels * 0.5;
    float glowHalfWidth = visibleLineHalfWidth + GlowPixelOffset;

    float perpendicularDistance = abs(input.Side) * glowHalfWidth;

    float worldToPixels = input.LineLengthPixels / max(input.LineLength, 1e-6);
    float distanceToVisiblePixels = distanceToVisibleAlong * GlobalLineTypeScale * worldToPixels;

    float alongDistance = 0.0;

    if (input.AlongPixels < 0.0)
    {
        alongDistance = -input.AlongPixels;
    }
    else if (input.AlongPixels > input.LineLengthPixels)
    {
        alongDistance = input.AlongPixels - input.LineLengthPixels;
    }
    else if (!visible)
    {
        alongDistance = distanceToVisiblePixels;
    }

    float distanceFromCenterline = length(float2(perpendicularDistance, alongDistance));

    if (visible && distanceFromCenterline <= visibleLineHalfWidth)
    {
        discard;
    }

    float distanceFromLine = max(distanceFromCenterline - visibleLineHalfWidth, 0.0);

    if (distanceFromLine >= GlowPixelOffset)
    {
        discard;
    }

    float glowT = saturate(distanceFromLine / GlowPixelOffset);
    float glowAlpha = 1.0 - smoothstep(0.0, 1.0, glowT);

    const float MaxGlowAlpha = 0.45;

    glowAlpha *= MaxGlowAlpha;

    return float4(0.0, 0.0, 0.0, glowAlpha);
}
