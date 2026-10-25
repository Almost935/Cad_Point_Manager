using Cad_Point_Manager.Common;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Models.DrawingObjects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Cad_Point_Manager.Helpers
{
    public static class GeometryHelpers
    {
        private const double TwoPi = Math.PI * 2;
        public static bool RectsIntersect(Rect a, Rect b) => a.IntersectsWith(b);

        public static List<(SignificantPointType pointType, Vector2d position)> GetSignificantPointsList(
            List<DrawingSegment> segments)
        {
            var results = new ConcurrentBag<List<(SignificantPointType pointType, Vector2d position)>>();
            Parallel.For(0, segments.Count,
                () => new List<(SignificantPointType pointType, Vector2d position)>(),
                (i, _, local) =>
                {
                    var a = segments[i];
                    switch (a)
                    {
                        case DrawingLine line:
                            local.Add((SignificantPointType.EndPoint, line.Start));
                            local.Add((SignificantPointType.EndPoint, line.End));
                            local.Add((SignificantPointType.MidPoint, line.MidPoint));
                            break;
                        case DrawingArc arc:
                            local.Add((SignificantPointType.EndPoint, arc.Start));
                            local.Add((SignificantPointType.EndPoint, arc.End));
                            local.Add((SignificantPointType.MidPoint, arc.MidPoint));
                            break;
                    }
                    for (int j = i + 1; j < segments.Count; j++)
                    {
                        if (!IntersectGeometries(a, segments[j], out var hits)) continue;
                        foreach (var p in hits) local.Add((SignificantPointType.Intersection, p));
                    }
                    return local;
                },
                local => results.Add(local));
            var output = new List<(SignificantPointType pointType, Vector2d position)>();
            foreach (var local in results) output.AddRange(local);
            return output;
        }

        public static Vector2d GetNearestPointOnGeometry(DrawingGeometry geometry, Vector2d point)
            => geometry switch
            {
                DrawingLine line => NearestPointOnLine(point, line.Start, line.End),
                DrawingArc arc => NearestPointOnArc(point, arc.RadiusPoint, arc.Radius,
                    DegreeToRadian(arc.StartAngle), DegreeToRadian(arc.EndAngle)),
                DrawingCircle circle => NearestPointOnCircle(point, circle.RadiusPoint, circle.Radius),
                _ => throw new NotImplementedException()
            };

        public static Vector2d NearestPointOnLine(Vector2d p, Vector2d a, Vector2d b)
            => Vector2d.GetClosestPointOnLine(a, b, p);

        // Angles in radians; increasing counterclockwise, wrapping through 2*pi.
        public static Vector2d NearestPointOnArc(Vector2d p, Vector2d center, double radius,
            double startAngle, double endAngle)
        {
            double angle = Math.Atan2(p.Y - center.Y, p.X - center.X);
            angle = ClampAngle(angle, startAngle, endAngle);
            return center + new Vector2d(radius * Math.Cos(angle), radius * Math.Sin(angle));
        }

        public static Vector2d NearestPointOnCircle(Vector2d p, Vector2d center, double radius)
        {
            Vector2d d = p - center;
            double squared = d.LengthSquared();
            if (squared == 0) return new Vector2d(center.X + radius, center.Y);
            return center + d * (radius / Math.Sqrt(squared));
        }

        public static bool IntersectGeometries(DrawingGeometry a, DrawingGeometry b,
            out List<Vector2d> intersections)
        {
            if (a is DrawingLine lineA)
            {
                if (b is DrawingLine lineB)
                {
                    intersections = new List<Vector2d>(1);
                    if (!TryIntersectLineLine(lineA, lineB, out Vector2d hit)) return false;
                    intersections.Add(hit);
                    return true;
                }
                if (b is DrawingArc arcB) return TryIntersectLineArc(lineA, arcB, out intersections);
                if (b is DrawingCircle circleB) return TryIntersectLineCircle(lineA, circleB, out intersections);
            }
            else if (a is DrawingArc arcA)
            {
                if (b is DrawingLine lineB) return TryIntersectLineArc(lineB, arcA, out intersections);
                if (b is DrawingArc arcB) return TryIntersectArcArc(arcA, arcB, out intersections);
                if (b is DrawingCircle circleB) return TryIntersectArcCircle(arcA, circleB, out intersections);
            }
            else if (a is DrawingCircle circleA)
            {
                if (b is DrawingLine lineB) return TryIntersectLineCircle(lineB, circleA, out intersections);
                if (b is DrawingArc arcB) return TryIntersectArcCircle(arcB, circleA, out intersections);
                if (b is DrawingCircle circleB) return TryIntersectCircleCircle(circleA, circleB, out intersections);
            }
            intersections = new List<Vector2d>(0);
            return false;
        }

        public static bool TryIntersectLineLine(DrawingLine a, DrawingLine b, out Vector2d intersection)
        {
            intersection = default;
            Vector2d p = a.Start, r = a.End - a.Start;
            Vector2d q = b.Start, s = b.End - b.Start;
            double denominator = Vector2d.Cross(r, s);
            // Relative tolerance in the cross product; collinear overlaps aren't returned.
            double scale = Math.Sqrt(r.LengthSquared() * s.LengthSquared());
            if (scale == 0 || Math.Abs(denominator) <= 1e-12 * scale) return false;
            Vector2d qp = q - p;
            double t = Vector2d.Cross(qp, s) / denominator;
            double u = Vector2d.Cross(qp, r) / denominator;
            if (t < 0 || t > 1 || u < 0 || u > 1) return false;
            intersection = p + t * r;
            return true;
        }

        public static bool TryIntersectLineCircle(DrawingLine line, DrawingCircle circle,
            out List<Vector2d> intersections)
            => GetLineCircleIntersection(line.Start, line.End, circle.RadiusPoint,
                circle.Radius, out intersections);

        public static bool TryIntersectLineArc(DrawingLine line, DrawingArc arc,
            out List<Vector2d> intersections)
        {
            GetLineCircleIntersection(line.Start, line.End, arc.RadiusPoint, arc.Radius,
                out var candidates);
            intersections = new List<Vector2d>(2);
            double start = DegreeToRadian(arc.StartAngle), end = DegreeToRadian(arc.EndAngle);
            foreach (var p in candidates)
                if (IsAngleBetween(GetAngle(arc.RadiusPoint, p), start, end)) intersections.Add(p);
            return intersections.Count != 0;
        }

        public static bool TryIntersectCircleCircle(DrawingCircle a, DrawingCircle b,
            out List<Vector2d> intersections)
            => GetCircleCircleIntersection(a.RadiusPoint, a.Radius, b.RadiusPoint, b.Radius,
                out intersections);

        public static bool TryIntersectArcCircle(DrawingArc arc, DrawingCircle circle,
            out List<Vector2d> intersections)
        {
            GetCircleCircleIntersection(arc.RadiusPoint, arc.Radius, circle.RadiusPoint,
                circle.Radius, out var candidates);
            intersections = new List<Vector2d>(2);
            double start = DegreeToRadian(arc.StartAngle), end = DegreeToRadian(arc.EndAngle);
            foreach (var p in candidates)
                if (IsAngleBetween(GetAngle(arc.RadiusPoint, p), start, end)) intersections.Add(p);
            return intersections.Count != 0;
        }

        public static bool TryIntersectArcArc(DrawingArc a, DrawingArc b,
            out List<Vector2d> intersections)
        {
            GetCircleCircleIntersection(a.RadiusPoint, a.Radius, b.RadiusPoint, b.Radius,
                out var candidates);
            intersections = new List<Vector2d>(2);
            double aStart = DegreeToRadian(a.StartAngle), aEnd = DegreeToRadian(a.EndAngle);
            double bStart = DegreeToRadian(b.StartAngle), bEnd = DegreeToRadian(b.EndAngle);
            foreach (var p in candidates)
                if (IsAngleBetween(GetAngle(a.RadiusPoint, p), aStart, aEnd) &&
                    IsAngleBetween(GetAngle(b.RadiusPoint, p), bStart, bEnd)) intersections.Add(p);
            return intersections.Count != 0;
        }

        public static bool GetLineCircleIntersection(Vector2d start, Vector2d end,
            Vector2d center, double radius, out List<Vector2d> intersections)
        {
            intersections = new List<Vector2d>(2);
            if (radius < 0) return false;
            Vector2d d = end - start, f = start - center;
            double a = Vector2d.Dot(d, d);
            if (a == 0)
            {
                if (Math.Abs(f.LengthSquared() - radius * radius) <= 1e-12 * Math.Max(1, radius * radius))
                    intersections.Add(start);
                return intersections.Count != 0;
            }
            double b = 2 * Vector2d.Dot(f, d);
            double c = Vector2d.Dot(f, f) - radius * radius;
            double determinant = b * b - 4 * a * c;
            if (determinant < 0) return false;
            double sqrt = Math.Sqrt(determinant);
            double t1 = (-b - sqrt) / (2 * a);
            double t2 = (-b + sqrt) / (2 * a);
            if (t1 >= 0 && t1 <= 1) intersections.Add(start + d * t1);
            if (sqrt > 0 && t2 >= 0 && t2 <= 1) intersections.Add(start + d * t2);
            return intersections.Count != 0;
        }

        public static bool GetCircleCircleIntersection(Vector2d c0, double r0,
            Vector2d c1, double r1, out List<Vector2d> intersections)
        {
            intersections = new List<Vector2d>(2);
            if (r0 < 0 || r1 < 0) return false;
            Vector2d d = c1 - c0;
            double distanceSquared = d.LengthSquared();
            if (distanceSquared == 0) return false; // Coincident circles not represented as finite points.
            double sum = r0 + r1, diff = Math.Abs(r0 - r1);
            if (distanceSquared > sum * sum || distanceSquared < diff * diff) return false;
            double distance = Math.Sqrt(distanceSquared);
            double a = (r0 * r0 - r1 * r1 + distanceSquared) / (2 * distance);
            double hSquared = r0 * r0 - a * a;
            if (hSquared < 0)
            {
                if (hSquared < -1e-12 * Math.Max(1, r0 * r0)) return false;
                hSquared = 0; // Floating-point roundoff near tangency.
            }
            Vector2d middle = c0 + d * (a / distance);
            Vector2d offset = d.Perpendicular() * (Math.Sqrt(hSquared) / distance);
            intersections.Add(middle + offset);
            if (hSquared > 0) intersections.Add(middle - offset);
            return true;
        }

        private static double DegreeToRadian(double degrees) => degrees * (Math.PI / 180.0);
        private static double NormalizeAngle(double angle)
        {
            angle %= TwoPi;
            return angle < 0 ? angle + TwoPi : angle;
        }
        private static bool IsAngleBetween(double angle, double start, double end)
        {
            angle = NormalizeAngle(angle);
            start = NormalizeAngle(start);
            end = NormalizeAngle(end);
            return start <= end ? angle >= start && angle <= end : angle >= start || angle <= end;
        }
        private static double GetAngle(Vector2d center, Vector2d point)
            => NormalizeAngle(Math.Atan2(point.Y - center.Y, point.X - center.X));
        private static double GetAngleDistance(double a, double b)
        {
            double d = Math.Abs(NormalizeAngle(a) - NormalizeAngle(b));
            return Math.Min(d, TwoPi - d);
        }
        private static double ClampAngle(double angle, double start, double end)
        {
            angle = NormalizeAngle(angle);
            start = NormalizeAngle(start);
            end = NormalizeAngle(end);
            if (IsAngleBetween(angle, start, end)) return angle;
            return GetAngleDistance(angle, start) <= GetAngleDistance(angle, end) ? start : end;
        }
    }
}
