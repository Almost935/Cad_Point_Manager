
using Cad_Point_Manager.Models.DrawingObjects.HelperClasses;

namespace Cad_Point_Manager.Services.Exporting
{
    public static class LineTypePatternEvaluator
    {
        private const double Epsilon = 1e-6;

        public static IEnumerable<(double Start, double End)> GetVisibleIntervals(
            LineType lineType,
            double length,
            double globalScale,
            double startDistance = 0,
            double parentSegmentLength = 0)
        {
            if (length <= 0)
                yield break;

            if (lineType == null ||
                lineType.Pattern.Count == 0 ||
                lineType.PatternLength <= Epsilon ||
                globalScale <= Epsilon)
            {
                yield return (0, length);
                yield break;
            }

            var pattern = lineType.Pattern;
            double patternLength = lineType.PatternLength;

            // Same special centering behavior as LineShader.hlsl.
            double phaseOffset = 0;

            if (parentSegmentLength > Epsilon &&
                pattern.Count == 2)
            {
                double dashLength = pattern[0];
                double gapLength = Math.Abs(pattern[1]);

                double halfSegmentLength =
                    parentSegmentLength / globalScale * 0.5;

                double dashCenteredPhase = dashLength * 0.5;
                double gapCenteredPhase =
                    dashLength + gapLength * 0.5;

                double dashEnd = PositiveModulo(
                    halfSegmentLength + dashCenteredPhase,
                    patternLength);

                double gapEnd = PositiveModulo(
                    halfSegmentLength + gapCenteredPhase,
                    patternLength);

                double dashEndStroke = 0;

                if (dashEnd < dashLength)
                {
                    dashEndStroke = Math.Min(
                        dashEnd,
                        dashLength - dashEnd);
                }

                double gapEndStroke = 0;

                if (gapEnd < dashLength)
                {
                    gapEndStroke = Math.Min(
                        gapEnd,
                        dashLength - gapEnd);
                }

                double centerPhase =
                    gapEndStroke > dashEndStroke
                    ? gapCenteredPhase
                    : dashCenteredPhase;

                phaseOffset = centerPhase -
                    parentSegmentLength / (2 * globalScale);
            }

            double position = 0;

            while (position < length)
            {
                // Equivalent to:
                // scaledDistance = pathDistance / GlobalLineTypeScale
                // plus the optional centered phase.
                double scaledDistance =
                    (startDistance + position) / globalScale
                    + phaseOffset;

                double patternPosition = PositiveModulo(
                    scaledDistance,
                    patternLength);

                double accumulated = 0;
                double nextPosition = length;
                bool visible = true;
                bool found = false;

                foreach (float segment in pattern)
                {
                    double segmentLength = Math.Abs(segment);
                    double segmentEnd =
                        accumulated + segmentLength;

                    if (patternPosition < segmentEnd)
                    {
                        visible = segment > 0;

                        double remaining =
                            segmentEnd - patternPosition;

                        nextPosition = Math.Min(
                            length,
                            position + remaining * globalScale);

                        found = true;
                        break;
                    }

                    accumulated = segmentEnd;
                }

                if (!found)
                    break;

                if (nextPosition <= position)
                    break;

                if (visible)
                    yield return (position, nextPosition);

                position = nextPosition;
            }
        }

        private static double PositiveModulo(
            double value,
            double divisor)
        {
            double result = value % divisor;

            return result < 0
                ? result + divisor
                : result;
        }
    }
}
