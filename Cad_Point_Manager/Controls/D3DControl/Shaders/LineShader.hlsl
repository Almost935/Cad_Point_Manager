//-----------------------------------------------------------------------------
// LineShader.hlsl
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

cbuffer LineRenderModeBuffer : register(b2)
{
    uint RenderSelectedOnly;
    uint RenderGlowPass;
    float2 _padding2;
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
    uint Flags : FLAGS;
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
    
    // Continuous distance along parent DXF entity.
    float PathDistance : TEXCOORD7;

    // Local distance along this GPU segment.
    float SegmentDistance : TEXCOORD8;

    // Length of this individual GPU segment.
    nointerpolation float SegmentLength : TEXCOORD9;

    // Tells us whether this GPU segment contains a real entity endpoint.
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

static const uint OBJ_VISIBLE = 1u << 0;
static const uint OBJ_SELECTED = 1u << 1;
static const uint OBJ_MOUSEOVER = 1u << 2;
static const uint OBJ_COLOR_BY_LAYER = 1u << 3;

//-----------------------------------------------------------------------------
// Vertex shader
//-----------------------------------------------------------------------------

PSInput VSMain(VSInput vertex, VSInstance instance)
{
    PSInput output;

    //--------------------------------------------
    // Transform endpoints to clip space
    //--------------------------------------------

    float4 clipStart = mul(float4(instance.Start, 0, 1), transformationMatrix);
    float4 clipEnd = mul(float4(instance.End, 0, 1), transformationMatrix);

    //--------------------------------------------
    // Convert to NDC
    //--------------------------------------------

    float2 ndcStart = clipStart.xy / clipStart.w;
    float2 ndcEnd = clipEnd.xy / clipEnd.w;

    //--------------------------------------------
    // Screen-space direction
    //--------------------------------------------

    float2 delta = ndcEnd - ndcStart;
    float len = length(delta);

    if (len < 1e-6)
    {
        delta = float2(1, 0);
        len = 1;
    }

    float2 pixelScale = float2(ViewportSize.x * 0.5, ViewportSize.y * 0.5);

    float2 dirPixels = (ndcEnd - ndcStart) * pixelScale;
    dirPixels = normalize(dirPixels);

    float2 normalPixels = float2(-dirPixels.y, dirPixels.x);

    //--------------------------------------------
    // Pixel directions -> NDC directions
    //--------------------------------------------

    float2 normalNdc = float2(
        normalPixels.x * (2.0 / ViewportSize.x),
        normalPixels.y * (2.0 / ViewportSize.y));

    float2 directionNdc = float2(
        dirPixels.x * (2.0 / ViewportSize.x),
        dirPixels.y * (2.0 / ViewportSize.y));

    //--------------------------------------------
    // Determine width
    //--------------------------------------------
    
    float visibleLineHalfWidth = LineHalfWidthPixels * 0.5;

    float halfWidthPixels = LineHalfWidthPixels;

    if (RenderGlowPass == 1)
        halfWidthPixels = visibleLineHalfWidth + GlowPixelOffset;

    float2 offset = normalNdc * halfWidthPixels;

    //--------------------------------------------
    // Screen-space line length
    //--------------------------------------------

    float lineLengthPixels = length((ndcEnd - ndcStart) * pixelScale);

    //--------------------------------------------
    // Position along segment
    //--------------------------------------------

    float t = vertex.Local.y;
    float2 ndc = lerp(ndcStart, ndcEnd, t);

    //--------------------------------------------
    // Extend glow beyond physical entity endpoints
    //--------------------------------------------

    if (RenderGlowPass == 1)
    {
        float endDirection = vertex.Local.y * 2.0 - 1.0;

        ndc += directionNdc * GlowPixelOffset * endDirection;

        output.AlongPixels =
            lerp(-GlowPixelOffset, lineLengthPixels + GlowPixelOffset, vertex.Local.y);
    }
    else
    {
        output.AlongPixels = vertex.Local.y * lineLengthPixels;
    }

    //--------------------------------------------
    // Expand perpendicular to line
    //--------------------------------------------

    ndc += offset * vertex.Local.x;

    //--------------------------------------------
    // Convert back to clip coordinates
    //--------------------------------------------

    float4 clip = lerp(clipStart, clipEnd, t);
    clip.xy = ndc * clip.w;

    //--------------------------------------------
    // Output
    //--------------------------------------------

    output.Position = clip;
    output.Side = vertex.Local.x;
    output.LineLengthPixels = lineLengthPixels;
    output.ParentSegmentLength = instance.ParentSegmentLength;

    //--------------------------------------------
    // Segment / path distances
    //--------------------------------------------

    float lineLength = length(instance.End - instance.Start);

    output.LineLength = lineLength;
    output.SegmentLength = lineLength;
    output.SegmentDistance = t * lineLength;

    //--------------------------------------------
    // Existing glow-expanded distance
    //--------------------------------------------

    if (RenderGlowPass == 1)
    {
        float pixelsToWorld = lineLength / max(lineLengthPixels, 1e-6);
        float glowLocalDistance = output.AlongPixels * pixelsToWorld;
        output.Distance = glowLocalDistance;
        output.PathDistance = instance.StartDistance + glowLocalDistance;
    }
    else
    {
        output.Distance = output.SegmentDistance;
        output.PathDistance = instance.StartDistance + output.SegmentDistance;
    }

    //--------------------------------------------
    // IDs / flags
    //--------------------------------------------

    output.LayerId = instance.LayerId;
    output.ObjectId = instance.ObjectId;
    output.Flags = instance.Flags;

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

    if ((os.Flags & OBJ_VISIBLE) == 0)
        discard;

    bool selected = (os.Flags & OBJ_SELECTED) != 0;
    bool mouseOver = (os.Flags & OBJ_MOUSEOVER) != 0;

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
                dashCenteredEndStroke =
            min(
                dashCenteredEndPos,
                dashLength - dashCenteredEndPos);
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
        {
            patternPos += lti.PatternLength;
        }

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

    if (RenderGlowPass == 0 && !visible)
        discard;

    //--------------------------------------------
    // Determine render pass
    //--------------------------------------------

    if (RenderGlowPass == 1)
    {
        if (!mouseOver)
            discard;
    }
    else if (RenderSelectedOnly == 1)
    {
        if (!selected)
            discard;
    }
    else
    {
        if (selected)
            discard;
    }

    //-------------------------------------------------------------------------
    // Mouseover glow
    //-------------------------------------------------------------------------

    if (RenderGlowPass == 1)
    {
        float visibleLineHalfWidth = LineHalfWidthPixels * 0.5;
        float glowHalfWidth = visibleLineHalfWidth + GlowPixelOffset;

        //--------------------------------------------
        // Perpendicular distance from line center
        //--------------------------------------------

        float perpendicularDistance = abs(input.Side) * glowHalfWidth;

        //--------------------------------------------
        // Convert linetype distance to screen pixels
        //--------------------------------------------

        float worldToPixels = input.LineLengthPixels / max(input.LineLength, 1e-6);

        float distanceToVisiblePixels =
            distanceToVisibleAlong * GlobalLineTypeScale * worldToPixels;

        //--------------------------------------------
        // Longitudinal distance from nearest visible
        // portion of the line
        //--------------------------------------------

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
            discard;

        float distanceFromLine = max(distanceFromCenterline - visibleLineHalfWidth, 0.0);

        if (distanceFromLine >= GlowPixelOffset)
            discard;

        float glowT = saturate(distanceFromLine / GlowPixelOffset);
        float glowAlpha = 1.0 - smoothstep(0.0, 1.0, glowT);

        const float MaxGlowAlpha = 0.45;
        glowAlpha *= MaxGlowAlpha;

        return float4(0.0, 0.0, 0.0, glowAlpha);
    }

    //-------------------------------------------------------------------------
    // Normal line rendering
    //-------------------------------------------------------------------------

    float4 color = ((os.Flags & OBJ_COLOR_BY_LAYER) != 0) ? ls.Color : os.Color;

    if (selected)
        color = SelectedColor;

    float d = abs(input.Side);
    float w = fwidth(d);
    float visibleSide = abs(input.Side) * 2.0;
    float alpha = 1.0 - smoothstep(1.0 - w, 1.0 + w, visibleSide);

    color.a *= alpha;

    return color;
}