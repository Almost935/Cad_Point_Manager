using Cad_Point_Manager.Common;
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
        public static double PointToPointDistance(Vector2d p1, Vector2d p2)
        {
            return Math.Sqrt(
                Math.Pow((p2.X - p1.X), 2) + Math.Pow((p2.Y - p1.Y), 2));
        }
        public static double PointToLineDistance(Vector2d p, Vector2d lineStart, Vector2d lineEnd)
        {
            Vector2d p2 = GetClosestPointOnLine(lineStart, lineEnd, p);

            return PointToPointDistance(p, p2);
        }
        public static Vector2d GetClosestPointOnLine(Vector2d start, Vector2d end, Vector2d p)
        {
            double length = (start - end).LengthSquared();
            if (length == 0.0)
            {
                return start;
            }
            Vector2d v = end - start;
            double param = (p - start) * v / length;

            return (param < 0.0) ? start : (param > 1.0) ? end : (start + param * v);
        }

        // Tessellation Methods
        public static bool IsPointInTriangle(Vector2d p, Vector2d a, Vector2d b, Vector2d c)
        {
            var v0 = c - a;
            var v1 = b - a;
            var v2 = p - a;

            double dot00 = Vector2d.Dot(v0, v0);
            double dot01 = Vector2d.Dot(v0, v1);
            double dot02 = Vector2d.Dot(v0, v2);
            double dot11 = Vector2d.Dot(v1, v1);
            double dot12 = Vector2d.Dot(v1, v2);

            double denom = dot00 * dot11 - dot01 * dot01;

            if (Math.Abs(denom) < double.Epsilon) 
                return false; 

            double invDenom = 1.0 / denom;
            double u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            double v = (dot00 * dot12 - dot01 * dot02) * invDenom;

            return (u >= 0) && (v >= 0) && (u + v <= 1);
        }
        public static double DistanceToTriangle(Vector2d p, Vector2d a, Vector2d b, Vector2d c)
        {
            double d1 = DistanceToSegment(p, a, b);
            double d2 = DistanceToSegment(p, b, c);
            double d3 = DistanceToSegment(p, c, a);

            return Math.Min(d1, Math.Min(d2, d3));
        }
        public static double DistanceToSegment(Vector2d p, Vector2d a, Vector2d b)
        {
            Vector2d ab = b - a;
            Vector2d ap = p - a;
            double t = Vector2d.Dot(ap, ab) / Vector2d.Dot(ab, ab);
            t = Math.Clamp(t, 0, 1);
            Vector2d closest = a + t * ab;

            return Vector2d.DistanceTo(p, closest);
        }
        public static Vector2d GetIntersection(Vector2d p1, Vector2d p2, Vector2d p3, Vector2d p4)
        {
            double x1 = p1.X;
            double y1 = p1.Y;

            double x2 = p2.X;
            double y2 = p2.Y;

            double x3 = p3.X;
            double y3 = p3.Y;

            double x4 = p4.X;
            double y4 = p4.Y;

            double denom =
                (x1 - x2) * (y3 - y4) -
                (y1 - y2) * (x3 - x4);

            double px =
                ((x1 * y2 - y1 * x2) * (x3 - x4) -
                 (x1 - x2) * (x3 * y4 - y3 * x4))
                / denom;

            double py =
                ((x1 * y2 - y1 * x2) * (y3 - y4) -
                 (y1 - y2) * (x3 * y4 - y3 * x4))
                / denom;

            return new Vector2d(px, py);
        }
        public static bool LineSegmentsIntersect(Vector2d p1, Vector2d p2, Vector2d p3, Vector2d p4)
        {
            double d1 = Direction(p3, p4, p1);
            double d2 = Direction(p3, p4, p2);
            double d3 = Direction(p1, p2, p3);
            double d4 = Direction(p1, p2, p4);

            return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0))
                && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
        }

        public static double Direction(Vector2d a, Vector2d b, Vector2d c)
        {
            return
                (c.X - a.X) * (b.Y - a.Y)
              - (c.Y - a.Y) * (b.X - a.X);
        }

        public static List<Vector2d> TessellateBulge(
            Vector2d start, Vector2d end, double bulge, double tolerance = 0.001)
        {
            List<Vector2d> points = [];

            if (Math.Abs(bulge) < 1e-6f)
            {
                points.Add(start);
                points.Add(end);
                return points;
            }

            double chord = Vector2d.Distance(start, end);
            double includedAngle = 4 * Math.Atan(Math.Abs(bulge));
            double radius = chord / (2 * Math.Sin(includedAngle / 2));

            Vector2d chordDir = Vector2d.Normalize(end - start);
            Vector2d perp = new(-chordDir.Y, chordDir.X);

            double sagitta = bulge * chord / 2;
            double h = radius - Math.Abs(sagitta);

            if (bulge < 0)
                perp = -perp;

            Vector2d midpoint = (start + end) * 0.5;
            Vector2d center = midpoint + perp * h;
            double startAngle = Math.Atan2(start.Y - center.Y, start.X - center.X);
            double endAngle = Math.Atan2(end.Y - center.Y, end.X - center.X);
            double sweep = endAngle - startAngle;

            if (bulge > 0 && sweep < 0)
                sweep += Math.PI * 2;

            if (bulge < 0 && sweep > 0)
                sweep -= Math.PI * 2;

            int segments = DrawingArc.CalculateSegments(
                radius, Math.Abs(sweep * 180 / Math.PI), tolerance);

            for (int i = 0; i <= segments; i++)
            {
                double t = (double)i / segments;
                double angle = startAngle + sweep * t;

                points.Add(new Vector2d(
                    center.X + radius * Math.Cos(angle),
                    center.Y + radius * Math.Sin(angle)));
            }

            return points;
        }

        public static Rect GetLocalBounds(List<Vector2d> vertices)
        {
            if (vertices.Count == 0) { return Rect.Empty; }

            double minX = vertices.Min(v => v.X);
            double minY = vertices.Min(v => v.Y);

            double maxX = vertices.Max(v => v.X);
            double maxY = vertices.Max(v => v.Y);

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }
    }
}
