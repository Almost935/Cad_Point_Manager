using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
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
    }
}
