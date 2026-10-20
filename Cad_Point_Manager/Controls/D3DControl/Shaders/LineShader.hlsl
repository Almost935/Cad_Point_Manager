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
    
    float CogoPointCircleRadiusPixels;
    float3 _pad2;
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
    float LocalT : TEXCOORD0;
    nointerpolation float LengthPixels : TEXCOORD1;
    nointerpolation float LengthWorld : TEXCOORD2;
    nointerpolation float StartDistance : TEXCOORD3;
    nointerpolation float ParentLength : TEXCOORD4;
    nointerpolation uint LayerId : TEXCOORD5;
    nointerpolation uint ObjectId : TEXCOORD6;
    float Side : TEXCOORD7;
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

    float4 clipStart = mul(float4(instance.Start, 0, 1), transformationMatrix);
    float4 clipEnd = mul(float4(instance.End, 0, 1), transformationMatrix);

    float2 ndcStart = clipStart.xy / clipStart.w;
    float2 ndcEnd = clipEnd.xy / clipEnd.w;

    float2 pixelScale = ViewportSize * 0.5;
    float2 segmentPixels = (ndcEnd - ndcStart) * pixelScale;
    float lengthPixels = length(segmentPixels);
    float2 dirPixels = lengthPixels > 1e-6
        ? segmentPixels / lengthPixels
        : float2(1.0, 0.0);
    float2 normalPixels = float2(-dirPixels.y, dirPixels.x);

    float2 pixelsToNdc = 2.0 / ViewportSize;
    float2 normalNdc = normalPixels * pixelsToNdc;
    float2 directionNdc = dirPixels * pixelsToNdc;

    float halfWidthPixels = LineHalfWidthPixels;

    float t = vertex.Local.y;
    float2 ndc = lerp(ndcStart, ndcEnd, t);
    float alongPixels = t * lengthPixels;

    ndc += normalNdc * halfWidthPixels * vertex.Local.x;

    float4 clip = lerp(clipStart, clipEnd, t);
    clip.xy = ndc * clip.w;

    output.Position = clip;
    output.Side = vertex.Local.x;
    output.LocalT = alongPixels / max(lengthPixels, 1e-6);
    output.LengthPixels = lengthPixels;
    output.LengthWorld = length(instance.End - instance.Start);
    output.StartDistance = instance.StartDistance;
    output.ParentLength = instance.ParentSegmentLength;
    output.LayerId = instance.LayerId;
    output.ObjectId = instance.ObjectId;

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

    //--------------------------------------------
    // Reconstruct world-space distance from local segment position.
    // Zoom changes only the rendered pixel length, not the dash pattern.
    //--------------------------------------------

    float pathDistance = input.StartDistance + input.LocalT * input.LengthWorld;

    //--------------------------------------------
    // Linetype
    //--------------------------------------------

    bool visible = true;
    float distanceToVisibleAlong = 0.0;

    if (lti.PatternCount > 0 && lti.PatternLength > 1e-6 && GlobalLineTypeScale > 1e-6)
    {
        float scaledDistance;

        if (input.ParentLength > 1e-6 && lti.PatternCount == 2)
        {
            float dashLength = PatternData[lti.FirstPatternIndex];
            float gapLength = abs(PatternData[lti.FirstPatternIndex + 1]);
            float scaledSegmentLength = input.ParentLength / GlobalLineTypeScale;
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
            float segmentCenter = input.ParentLength * 0.5;
            float distanceFromCenter = pathDistance - segmentCenter;

            scaledDistance = distanceFromCenter / GlobalLineTypeScale + centerPhase;
        }
        else
        {
            scaledDistance = pathDistance / GlobalLineTypeScale;
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
    
    if (!visible)
    {
        discard;
    }

    //-------------------------------------------------------------------------
    // Normal line rendering
    //-------------------------------------------------------------------------

    float4 color = ((os.Flags & OBJ_COLOR_BY_LAYER) != 0) ? ls.Color : os.Color;

    float d = abs(input.Side);
    float w = fwidth(d);
    float visibleSide = abs(input.Side) * 2.0;
    float alpha = 1.0 - smoothstep(1.0 - w, 1.0 + w, visibleSide);

    color.a *= alpha;

    return color;
}