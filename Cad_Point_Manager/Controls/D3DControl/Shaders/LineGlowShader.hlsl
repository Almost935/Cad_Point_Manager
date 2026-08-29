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
    uint ObjectId : OBJECTID;
    float StartDistance : STARTDISTANCE;
    uint Flags : FLAGS; // FORCE_START_VISIBLE / FORCE_END_VISIBLE
    float ParentSegmentLength : PARENTSEGMENTLENGTH;
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
    nointerpolation uint ObjectId : TEXCOORD4;
    float AlongPixels : TEXCOORD5;
    nointerpolation float LineLengthPixels : TEXCOORD6;
    float PathDistance : TEXCOORD7;
    float SegmentDistance : TEXCOORD8;
    nointerpolation float SegmentLength : TEXCOORD9;
    nointerpolation uint Flags : TEXCOORD10;
    nointerpolation float ParentSegmentLength : TEXCOORD11;
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
struct ObjectState
{
    uint Flags;
    uint LineTypeId;
    float2 Padding;
    float4 Color;
};
struct LineTypeInfo
{
    uint FirstPatternIndex;
    uint PatternCount;
    float PatternLength;
    float DashLength;
};

StructuredBuffer<LayerState> LayerStates : register(t0);
StructuredBuffer<ObjectState> ObjectStates : register(t1);
StructuredBuffer<LineTypeInfo> LineTypeInfos : register(t2);
StructuredBuffer<float> PatternData : register(t3);

static const uint LAYER_VISIBLE = 1u << 0;
static const uint OBJECT_MOUSEOVER = 1u << 2;

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
        ViewportSize.x * 0.5, ViewportSize.y * 0.5);

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

    float visibleLineHalfWidth = LineHalfWidthPixels * 0.5;
    float glowHalfWidth = visibleLineHalfWidth + GlowPixelOffset;

    //--------------------------------------------
    // Position along physical segment
    //--------------------------------------------

    float t = vertex.Local.y;
    float2 ndc = lerp(ndcStart, ndcEnd, t);

    //--------------------------------------------
    // Extend glow beyond BOTH physical endpoints
    //--------------------------------------------

    float endDirection = vertex.Local.y * 2.0 - 1.0;
    ndc += directionNdc * GlowPixelOffset * endDirection;

    //--------------------------------------------
    // Expand sideways
    //--------------------------------------------

    ndc += normalNdc * glowHalfWidth * vertex.Local.x;

    //--------------------------------------------
    // Convert back to clip space
    //--------------------------------------------

    float4 clip = lerp(clipStart, clipEnd, t);
    clip.xy = ndc * clip.w;
    output.Position = clip;
    output.Side = vertex.Local.x;

    //--------------------------------------------
    // Pixel-space distances
    //--------------------------------------------

    output.AlongPixels = lerp(-GlowPixelOffset, lineLengthPixels + GlowPixelOffset, t);
    output.LineLengthPixels = lineLengthPixels;

    //--------------------------------------------
    // World-space physical segment distances
    //--------------------------------------------

    float lineLength = length(instance.End - instance.Start);

    output.LineLength = lineLength;
    output.SegmentLength = lineLength;

    float pixelsToWorld = lineLength / max(lineLengthPixels, 1e-6);
    float glowLocalDistance = output.AlongPixels * pixelsToWorld;

    output.Distance = glowLocalDistance;
    output.SegmentDistance = glowLocalDistance;
    output.PathDistance = instance.StartDistance + glowLocalDistance;

    //--------------------------------------------
    // IDs / flags
    //--------------------------------------------

    output.LayerId = instance.LayerId;
    output.ObjectId = instance.ObjectId;
    output.Flags = instance.Flags;
    output.ParentSegmentLength = instance.ParentSegmentLength;

    return output;
}

//-----------------------------------------------------------------------------
// Pixel shader
//-----------------------------------------------------------------------------

float4 PSMain(PSInput input) : SV_TARGET
{
    LayerState ls = LayerStates[input.LayerId];
    ObjectState os = ObjectStates[input.ObjectId];
    LineTypeInfo lti = LineTypeInfos[os.LineTypeId];
    
    //--------------------------------------------
    // Visibility
    //--------------------------------------------

    if ((ls.Flags & LAYER_VISIBLE) == 0)
        discard;
    if ((os.Flags & OBJECT_MOUSEOVER) == 0)
        discard;

    //--------------------------------------------
    // Linetype
    //--------------------------------------------

    bool visible = true;
    float distanceToVisibleAlong = 0.0;

    if (lti.PatternCount > 0 && lti.PatternLength > 1e-6 && GlobalLineTypeScale > 1e-6)
    {
        float scaledDistance;

        if (input.ParentSegmentLength > 1e-6 && lti.PatternCount == 2)
        {
            float dashLength = PatternData[lti.FirstPatternIndex];
            float gapLength = abs(PatternData[lti.FirstPatternIndex + 1]);

            float scaledSegmentLength = input.ParentSegmentLength / GlobalLineTypeScale;
            float halfSegmentLength = scaledSegmentLength * 0.5;

            float dashCenteredPhase = dashLength * 0.5;
            float gapCenteredPhase = dashLength + gapLength * 0.5;

            float dashCenteredEndPos = fmod(halfSegmentLength + dashCenteredPhase, lti.PatternLength);
            float gapCenteredEndPos = fmod(halfSegmentLength + gapCenteredPhase, lti.PatternLength);

            if (dashCenteredEndPos < 0.0)
                dashCenteredEndPos += lti.PatternLength;

            if (gapCenteredEndPos < 0.0)
                gapCenteredEndPos += lti.PatternLength;

            float dashCenteredEndStroke = 0.0;

            if (dashCenteredEndPos < dashLength)
            {
                dashCenteredEndStroke = min(dashCenteredEndPos, dashLength - dashCenteredEndPos);
            }

            float gapCenteredEndStroke = 0.0;

            if (gapCenteredEndPos < dashLength)
            {
                gapCenteredEndStroke = min(gapCenteredEndPos, dashLength - gapCenteredEndPos);
            }

            float centerPhase = gapCenteredEndStroke > dashCenteredEndStroke ? gapCenteredPhase : dashCenteredPhase;

            float segmentCenter = input.ParentSegmentLength * 0.5;
            float distanceFromCenter = input.PathDistance - segmentCenter;

            scaledDistance = distanceFromCenter / GlobalLineTypeScale + centerPhase;
        }
        else
        {
            scaledDistance = input.PathDistance / GlobalLineTypeScale;
        }

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

    //--------------------------------------------
    // Visible line / glow dimensions
    //--------------------------------------------

    float visibleLineHalfWidth = LineHalfWidthPixels * 0.5;
    float glowHalfWidth = visibleLineHalfWidth + GlowPixelOffset;

    //--------------------------------------------
    // Convert linetype gap distance to pixels
    //--------------------------------------------

    float worldToPixels = input.LineLengthPixels / max(input.LineLength, 1e-6);
    float distanceToVisiblePixels = distanceToVisibleAlong * GlobalLineTypeScale * worldToPixels;
    float patternAlongDistance;

    if (input.AlongPixels < 0.0)
    {
        patternAlongDistance = -input.AlongPixels;
    }
    else if (input.AlongPixels > input.LineLengthPixels)
    {
        patternAlongDistance = input.AlongPixels - input.LineLengthPixels;
    }
    else if (visible)
    {
        patternAlongDistance = 0.0;
    }
    else
    {
        patternAlongDistance = distanceToVisiblePixels;
    }

    //--------------------------------------------
    // Distance to nearest visible centerline
    //--------------------------------------------

    float alongDistance = patternAlongDistance;
    float perpendicularDistance = abs(input.Side) * glowHalfWidth;
    float centerlineDistance = length(float2(perpendicularDistance, alongDistance));

    //--------------------------------------------
    // Distance to outside of stroke
    //--------------------------------------------

    float distanceFromStroke = centerlineDistance - visibleLineHalfWidth;
    float glowDistance = max(distanceFromStroke, 0.0);

    //--------------------------------------------
    // Outside glow radius
    //--------------------------------------------

    if (glowDistance >= GlowPixelOffset)
    {
        discard;
    }

    //--------------------------------------------
    // Glow
    //--------------------------------------------

    float glowT = saturate(glowDistance / GlowPixelOffset);
    float glowAlpha = 1.0 - smoothstep(0.0, 1.0, glowT);
    const float MaxGlowAlpha = 0.45;
    glowAlpha *= MaxGlowAlpha;

    return float4(0.0, 0.0, 0.0, glowAlpha);
}