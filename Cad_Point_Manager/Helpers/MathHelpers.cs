using Cad_Point_Manager.Models.DrawingObjects;
using SharpDX;
using System.Windows;
using Point = System.Windows.Point;

namespace Cad_Point_Manager.Helpers
{
    public static class MathHelpers
    {
        public static bool RectsIntersect(Rect rect1, Rect rect2)
        {
            if (rect1.IntersectsWith(rect2) || rect1.Contains(rect2) || rect2.Contains(rect1))
            {
                return true;
            }
            return false;
        }
        public static double PointToPointDistance(Point p1, Point p2)
        {
            return Math.Sqrt(
                Math.Pow((p2.X - p1.X), 2) + Math.Pow((p2.Y - p1.Y), 2));
        }
        public static double PointToLineDistance(Point p, Point lineStart, Point lineEnd)
        {
            Point p2 = GetClosestPointOnLine(lineStart, lineEnd, p);

            return PointToPointDistance(p, p2);
        }
        public static Point GetClosestPointOnLine(Point start, Point end, Point p)
        {
            double length = (start - end).LengthSquared;
            if (length == 0.0)
            {
                return start;
            }
            Vector v = end - start;
            double param = (p - start) * v / length;
            return (param < 0.0) ? start : (param > 1.0) ? end : (start + param * v);
        }

        // Tessellation Methods
        public static bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            var v0 = c - a;
            var v1 = b - a;
            var v2 = p - a;

            float dot00 = Vector2.Dot(v0, v0);
            float dot01 = Vector2.Dot(v0, v1);
            float dot02 = Vector2.Dot(v0, v2);
            float dot11 = Vector2.Dot(v1, v1);
            float dot12 = Vector2.Dot(v1, v2);

            float denom = dot00 * dot11 - dot01 * dot01;
            if (Math.Abs(denom) < float.Epsilon) return false; // Degenerate triangle

            float invDenom = 1f / denom;
            float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

            return (u >= 0) && (v >= 0) && (u + v <= 1);
        }
        public static float DistanceToTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = DistanceToSegment(p, a, b);
            float d2 = DistanceToSegment(p, b, c);
            float d3 = DistanceToSegment(p, c, a);

            return MathF.Min(d1, MathF.Min(d2, d3));
        }
        public static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            Vector2 ap = p - a;
            float t = Vector2.Dot(ap, ab) / Vector2.Dot(ab, ab);
            t = Math.Clamp(t, 0, 1);
            Vector2 closest = a + t * ab;
            return Vector2.Distance(p, closest);
        }
        public static Vector2 GetIntersection(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
        {
            float x1 = p1.X;
            float y1 = p1.Y;

            float x2 = p2.X;
            float y2 = p2.Y;

            float x3 = p3.X;
            float y3 = p3.Y;

            float x4 = p4.X;
            float y4 = p4.Y;

            float denom =
                (x1 - x2) * (y3 - y4) -
                (y1 - y2) * (x3 - x4);

            float px =
                ((x1 * y2 - y1 * x2) * (x3 - x4) -
                 (x1 - x2) * (x3 * y4 - y3 * x4))
                / denom;

            float py =
                ((x1 * y2 - y1 * x2) * (y3 - y4) -
                 (y1 - y2) * (x3 * y4 - y3 * x4))
                / denom;

            return new Vector2(px, py);
        }
        public static bool LineSegmentsIntersect(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
        {
            float d1 = Direction(p3, p4, p1);
            float d2 = Direction(p3, p4, p2);
            float d3 = Direction(p1, p2, p3);
            float d4 = Direction(p1, p2, p4);

            return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0))
                && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
        }

        public static float Direction(
            Vector3 a,
            Vector3 b,
            Vector3 c)
        {
            return
                (c.X - a.X) * (b.Y - a.Y)
              - (c.Y - a.Y) * (b.X - a.X);
        }

        public static List<Vector2> TessellateBulge(
            Vector2 start,
            Vector2 end,
            float bulge,
            double tolerance = 0.001)
        {
            List<Vector2> points = [];

            if (Math.Abs(bulge) < 1e-6f)
            {
                points.Add(start);
                points.Add(end);
                return points;
            }

            float chord = Vector2.Distance(start, end);

            float includedAngle = 4f * MathF.Atan(MathF.Abs(bulge));

            float radius = chord / (2f * MathF.Sin(includedAngle / 2f));

            Vector2 chordDir = Vector2.Normalize(end - start);

            Vector2 perp = new Vector2(-chordDir.Y, chordDir.X);

            // Sagitta
            float sagitta = bulge * chord / 2f;

            // Distance from midpoint to center
            float h = radius - MathF.Abs(sagitta);

            if (bulge < 0)
                perp = -perp;

            Vector2 midpoint = (start + end) * 0.5f;

            Vector2 center = midpoint + perp * h;

            float startAngle = MathF.Atan2(start.Y - center.Y,
                                           start.X - center.X);

            float endAngle = MathF.Atan2(end.Y - center.Y,
                                         end.X - center.X);

            float sweep = endAngle - startAngle;

            if (bulge > 0 && sweep < 0)
                sweep += MathF.PI * 2;

            if (bulge < 0 && sweep > 0)
                sweep -= MathF.PI * 2;

            int segments = DrawingArc.CalculateSegments(
                radius,
                Math.Abs(sweep * 180f / MathF.PI),
                tolerance);

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;

                float angle = startAngle + sweep * t;

                points.Add(new Vector2(
                    center.X + radius * MathF.Cos(angle),
                    center.Y + radius * MathF.Sin(angle)));
            }

            return points;
        }

        public static Rect GetLocalBounds(List<Vector2> vertices)
        {
            if (vertices.Count == 0) { return Rect.Empty; }

            float minX = vertices.Min(v => v.X);
            float minY = vertices.Min(v => v.Y);

            float maxX = vertices.Max(v => v.X);
            float maxY = vertices.Max(v => v.Y);

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }
    }
}
