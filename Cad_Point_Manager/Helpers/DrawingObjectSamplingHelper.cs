using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Models.DrawingObjects3D;
using System.Numerics;

namespace Cad_Point_Manager.Helpers
{
    public static class DrawingObjectSamplingHelper
    {
        public static IReadOnlyList<Vector2> SampleDrawingObject(DrawingObject3D drawingObject, int intermediates = 0)
        {
            return drawingObject switch
            {
                DrawingLine3D line => SampleDrawingLine(line, intermediates),
                DrawingArc3D arc => SampleDrawingArc(arc, intermediates),
                DrawingCircle3D circle => SampleDrawingCircle(circle, intermediates),
                DrawingPolyline3D polyline => SampleDrawingPolyline(polyline, intermediates),
                _ => throw new NotSupportedException($"Sampling not supported for {drawingObject.GetType().Name}"),
            };
        }

        public static IReadOnlyList<Vector2> SampleDrawingLine(DrawingLine3D drawingLine, int intermediates)
        {
            var pts = new List<Vector2>();
            int total = Math.Max(0, intermediates) + 2;
            if (total < 2) return pts;

            var start = drawingLine.Start.ToVector2();
            var end = drawingLine.End.ToVector2();
            var d = end - start;
            for (int i = 0; i < total; i++)
            {
                float t = (total == 1) ? 0f : (float)i / (total - 1);
                pts.Add(start + d * t);
            }
            return pts;
        }

        public static IReadOnlyList<Vector2> SampleDrawingArc(DrawingArc3D drawingArc, int intermediates, bool clockwise = false)
        {
            var pts = new List<Vector2>();
            int total = Math.Max(0, intermediates) + 2;
            if (total < 2 || drawingArc.Radius <= 0f) { return pts; }

            float sweep = NormalizeSweep(drawingArc.StartAngle, drawingArc.EndAngle, clockwise);
            float dir = clockwise ? -1f : 1f;

            for (int i = 0; i < total; i++)
            {
                float t = (float)i / (total - 1);
                float ang = drawingArc.StartAngle + dir * sweep * t;
                pts.Add(drawingArc.RadiusPoint.ToVector2() + new Vector2(drawingArc.Radius * MathF.Cos(ang), drawingArc.Radius * MathF.Sin(ang)));
            }
            return pts;
        }

        // Circles (closed): intermediates = total points around the rim
        // If you prefer "at least 2 points", change Math.Max(0, intermediates) to Math.Max(2, intermediates).
        public static IReadOnlyList<Vector2> SampleDrawingCircle(DrawingCircle3D drawingCircle, int intermediates)
        {
            var count = Math.Max(0, intermediates);
            var pts = new List<Vector2>(count);
            if (count == 0 || drawingCircle.Radius <= 0f) return pts;

            float step = MathF.Tau / count;
            for (int i = 0; i < count; i++)
            {
                float ang = drawingCircle.StartAngle + step * i;
                pts.Add(drawingCircle.RadiusPoint.ToVector2() + new Vector2(drawingCircle.Radius * MathF.Cos(ang), drawingCircle.Radius * MathF.Sin(ang)));
            }
            return pts;
        }

        public static IReadOnlyList<Vector2> SampleDrawingPolyline(DrawingPolyline3D drawingPolyline, int intermediates)
        {
            IReadOnlyList<Vector2> pts = [];
            return pts;
        }

        private static float NormalizeSweep(float start, float end, bool clockwise)
        {
            float twoPi = MathF.Tau;
            float raw = end - start;
            // normalize to [0, 2π)
            raw = (raw % twoPi + twoPi) % twoPi;
            if (raw == 0f) raw = twoPi; // full circle arc when angles equal
            // if clockwise, we still want the positive sweep length; direction handled by 'dir'
            return raw;
        }
    }
}
