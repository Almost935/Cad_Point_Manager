
using Cad_Point_Manager.Common;
using Cad_Point_Manager.Models.DrawingObjects.HelperClasses;
using PdfSharpCore.Drawing;
using System.Windows.Media;

namespace Cad_Point_Manager.Services.Exporting
{
    public static class PdfLineTypeRenderer
    {
        public static void DrawLine(
            XGraphics gfx,
            Matrix worldToPdf,
            XPen pen,
            Vector2d start,
            Vector2d end,
            LineType lineType,
            double globalScale,
            double startDistance = 0,
            double parentSegmentLength = 0)
        {
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;

            double length = Math.Sqrt(
                dx * dx + dy * dy);

            if (length <= 0)
                return;

            foreach (var (from, to) in
                LineTypePatternEvaluator.GetVisibleIntervals(
                    lineType,
                    length,
                    globalScale,
                    startDistance,
                    parentSegmentLength))
            {
                double t0 = from / length;
                double t1 = to / length;

                var a = new Vector2d(
                    start.X + dx * t0,
                    start.Y + dy * t0);

                var b = new Vector2d(
                    start.X + dx * t1,
                    start.Y + dy * t1);

                gfx.DrawLine(
                    pen,
                    PdfTransform.WorldToPdf(a, worldToPdf),
                    PdfTransform.WorldToPdf(b, worldToPdf));
            }
        }


        public static void DrawPath(
            XGraphics gfx,
            Matrix worldToPdf,
            XPen pen,
            IReadOnlyList<Vector2d> points,
            LineType lineType,
            double globalScale,
            bool closed = false,
            double? parentSegmentLength = null)
        {
            if (points == null || points.Count < 2)
                return;

            int segmentCount = closed
                ? points.Count
                : points.Count - 1;

            // Calculate the actual tessellated path length.
            double pathLength = 0;

            for (int i = 0; i < segmentCount; i++)
            {
                Vector2d a = points[i];
                Vector2d b = points[(i + 1) % points.Count];

                pathLength += Vector2d.DistanceTo(a, b);
            }

            if (pathLength <= 0)
                return;

            double parentLength =
                parentSegmentLength ?? pathLength;

            double distance = 0;

            for (int i = 0; i < segmentCount; i++)
            {
                Vector2d a = points[i];
                Vector2d b = points[(i + 1) % points.Count];

                double segmentLength =
                    Vector2d.DistanceTo(a, b);

                if (segmentLength <= 0)
                    continue;

                DrawLine(
                    gfx,
                    worldToPdf,
                    pen,
                    a,
                    b,
                    lineType,
                    globalScale,
                    distance,
                    parentLength);

                distance += segmentLength;
            }
        }

    }
}
