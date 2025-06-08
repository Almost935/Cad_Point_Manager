using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Models.DrawingObjects3D;
using SharpDX;
using System.Collections.Concurrent;
using System.Windows;

namespace Cad_Point_Manager.Helpers
{
    public static class GeometryHelpers
    {
        public static List<Vector> GetSignificantPointsList(List<DrawingSegment3D> segments)
        {
            var allPoints = new ConcurrentBag<Vector>();

            Parallel.ForEach(Enumerable.Range(0, segments.Count), () => new List<Vector>(),
                (i, state, localList) =>
                {
                    var segment1 = segments[i];

                    switch (segment1)
                    {
                        case DrawingLine3D line:
                            localList.AddRange(new[] { line.Start.ToVector(), line.End.ToVector(), line.MidPoint.ToVector() });
                            break;
                        case DrawingArc3D arc:
                            localList.AddRange(new[] { arc.RadiusPoint.ToVector(), arc.Start.ToVector(), arc.End.ToVector(), arc.MidPoint.ToVector() });
                            break;
                        case DrawingCircle3D circle:
                            localList.Add(circle.RadiusPoint.ToVector());
                            break;
                    }

                    for (int j = i + 1; j < segments.Count; j++)
                    {
                        var segment2 = segments[j];
                        var intersectionsExists = GeometryHelpers.IntersectGeometries(segment1, segment2, out var intersectionPoints);

                        if (intersectionsExists)
                        {
                            localList.AddRange(intersectionPoints);
                        }
                    }

                    return localList;
                },
                localList => { foreach (var pt in localList) allPoints.Add(pt); });

            return allPoints.Distinct(new VectorEqualityComparer(1e-5f)).ToList();
        }


        public static Vector GetNearestPointOnGeometry(DrawingGeometry3D geometry, Vector point)
        {
            return geometry switch
            {
                DrawingLine3D line => NearestPointOnLine(point, line.Start.ToVector(), line.End.ToVector()),
                DrawingArc3D arc => NearestPointOnLine(point, arc.Start.ToVector(), arc.End.ToVector()),
                DrawingCircle3D circle => NearestPointOnCircle(point, circle.RadiusPoint.ToVector(), circle.Radius),
                _ => throw new NotImplementedException()
            };
        }
        public static Vector NearestPointOnLine(Vector p, Vector a, Vector b)
        {
            Vector ap = new((p.X - a.X), (p.Y - a.Y));
            Vector ab = new((b.X - a.X), (b.Y - a.Y));

            double abLengthSquared = ab.LengthSquared;
            if (abLengthSquared == 0) { return a; }

            double t = Vector.Multiply(ap, ab) / abLengthSquared;
            t = Math.Clamp(t, 0, 1);

            return new(a.X + t * ab.X, a.Y + t * ab.Y);
        }
        public static Vector NearestPointOnArc(Vector p, Vector center, double radius, double startAngle, double endAngle)
        {
            // Vector from center to point
            double dx = p.X - center.X;
            double dy = p.Y - center.Y;

            double angleToPoint = Math.Atan2(dy, dx);
            double clampedAngle = ClampAngle(angleToPoint, startAngle, endAngle);

            double nearestX = center.X + radius * Math.Cos(clampedAngle);
            double nearestY = center.Y + radius * Math.Sin(clampedAngle);

            return new(nearestX, nearestY);
        }
        public static Vector NearestPointOnCircle(Vector p, Vector center, double radius)
        {
            double dx = p.X - center.X;
            double dy = p.Y - center.Y;
            double length = Math.Sqrt(dx * dx + dy * dy);

            if (length == 0) { return new(center.X + radius, center.Y); }

            double scale = radius / length;

            return new(center.X + dx * scale, center.Y + dy * scale);
        }


        public static bool IntersectGeometries(DrawingGeometry3D geometry1, DrawingGeometry3D geometry2, out List<Vector> intersections)
        {
            intersections = [];

            if (geometry1 is DrawingLine3D line1)
            {
                if (geometry2 is DrawingLine3D line2)
                {
                    if (TryIntersectLineLine(line1, line2, out Vector intersection))
                    {
                        intersections.Add(intersection);
                        return true;
                    }
                    return false;
                }
                if (geometry2 is DrawingArc3D arc2)
                {
                    if (TryIntersectLineArc(line1, arc2, out intersections))
                    {
                        return true;
                    }
                    return false;
                }
                if (geometry2 is DrawingCircle3D circle2)
                {
                    if (TryIntersectLineCircle(line1, circle2, out intersections))
                    {
                        return true;
                    }
                    return false;
                }
            }

            if (geometry1 is DrawingArc3D arc1)
            {
                if (geometry2 is DrawingLine3D line2)
                {
                    if (TryIntersectLineArc(line2, arc1, out intersections))
                    {
                        return true;
                    }
                    return false;
                }
                if (geometry2 is DrawingArc3D arc2)
                {
                    if (TryIntersectArcArc(arc1, arc2, out intersections))
                    {
                        return true;
                    }
                    return false;
                }
                if (geometry2 is DrawingCircle3D circle2)
                {
                    if (TryIntersectArcCircle(arc1, circle2, out intersections))
                    {
                        return true;
                    }
                    return false;
                }
            }

            if (geometry1 is DrawingCircle3D circle1)
            {
                if (geometry2 is DrawingLine3D line2)
                {
                    if (TryIntersectLineCircle(line2, circle1, out intersections))
                    {
                        return true;
                    }
                    return false;
                }
                if (geometry2 is DrawingArc3D arc2)
                {
                    if (TryIntersectArcCircle(arc2, circle1, out intersections))
                    {
                        return true;
                    }
                    return false;
                }
                if (geometry2 is DrawingCircle3D circle2)
                {
                    if (TryIntersectCircleCircle(circle1, circle2, out intersections))
                    {
                        return true;
                    }
                    return false;
                }
            }

            return false;
        }
        public static bool TryIntersectLineLine(DrawingLine3D line1, DrawingLine3D line2, out Vector intersection)
        {
            intersection = default;

            Vector p1 = line1.Start.ToVector();
            Vector p2 = line1.End.ToVector();
            Vector p3 = line2.Start.ToVector();
            Vector p4 = line2.End.ToVector();

            double A1 = p2.Y - p1.Y;
            double B1 = p1.X - p2.X;
            double C1 = A1 * p1.X + B1 * p1.Y;

            double A2 = p4.Y - p3.Y;
            double B2 = p3.X - p4.X;
            double C2 = A2 * p3.X + B2 * p3.Y;

            double det = A1 * B2 - A2 * B1;
            if (Math.Abs(det) < 1e-6f) { return false; } // Lines are parallel or coincident

            intersection = new Vector(
                (B2 * C1 - B1 * C2) / det,
                (A1 * C2 - A2 * C1) / det
            );

            // Check that intersection lies on both segments (not just the lines)
            bool pointOnSegment1 = IsPointOnSegment(p1, p2, intersection);
            bool pointOnSegment2 = IsPointOnSegment(p3, p4, intersection);

            return pointOnSegment1 && pointOnSegment2;
        }

        public static bool TryIntersectLineCircle(DrawingLine3D line, DrawingCircle3D circle, out List<Vector> intersections)
        {
            intersections = [];

            Vector d = line.End.ToVector() - line.Start.ToVector();
            Vector f = line.Start.ToVector() - circle.RadiusPoint.ToVector();

            double a = Vector.Multiply(d, d);
            double b = 2 * Vector.Multiply(f, d);
            double c = Vector.Multiply(f, f) - circle.Radius * circle.Radius;

            double discriminant = b * b - 4 * a * c;
            if (discriminant < 0) { return false; }

            discriminant = Math.Sqrt(discriminant);

            double t1 = (-b - discriminant) / (2 * a);
            double t2 = (-b + discriminant) / (2 * a);

            bool found = false;

            if (t1 >= 0 && t1 <= 1)
            {
                intersections.Add(line.Start.ToVector() + t1 * d);
                found = true;
            }

            if (t2 >= 0 && t2 <= 1 && t2 != t1)
            {
                intersections.Add(line.Start.ToVector() + t2 * d);
                found = true;
            }

            return found;
        }

        public static bool TryIntersectLineArc(DrawingLine3D line, DrawingArc3D arc, out List<Vector> intersections)
        {
            intersections = [];
            if (!GetLineCircleIntersection(line.Start.ToVector(), line.End.ToVector(), arc.RadiusPoint.ToVector(), arc.Radius, out var circlePoints))
            { return false; }

            double startRad = DegreeToRadian(arc.StartAngle);
            double endRad = DegreeToRadian(arc.EndAngle);

            foreach (var pt in circlePoints)
            {
                Vector dir = pt - arc.RadiusPoint.ToVector();
                double angle = Math.Atan2(dir.Y, dir.X);
                if (angle < 0) { angle += 2 * Math.PI; }

                if (IsAngleBetween(angle, startRad, endRad))
                { intersections.Add(pt); }
            }

            return intersections.Count > 0;
        }

        public static bool TryIntersectCircleCircle(DrawingCircle3D circle1, DrawingCircle3D circle2, out List<Vector> intersections)
        {
            bool intersects = GetCircleCircleIntersection(circle1.RadiusPoint.ToVector(), circle1.Radius, circle2.RadiusPoint.ToVector(),
                circle2.Radius, out intersections);

            return intersects;
        }

        public static bool TryIntersectArcCircle(DrawingArc3D arc, DrawingCircle3D circle, out List<Vector> intersections)
        {
            intersections = [];
            if (!GetCircleCircleIntersection(arc.RadiusPoint.ToVector(), arc.Radius, circle.RadiusPoint.ToVector(), circle.Radius, out var points))
            { return false; }

            double startRad = DegreeToRadian(arc.StartAngle);
            double endRad = DegreeToRadian(arc.EndAngle);

            foreach (var pt in points)
            {
                Vector dir = pt - arc.RadiusPoint.ToVector();
                double angle = Math.Atan2(dir.Y, dir.X);
                if (angle < 0) { angle += 2 * Math.PI; }

                if (IsAngleBetween(angle, startRad, endRad)) { intersections.Add(pt); }
            }
            return intersections.Count > 0;
        }

        public static bool TryIntersectArcArc(DrawingArc3D arc1, DrawingArc3D arc2, out List<Vector> intersections)
        {
            intersections = [];
            if (!GetCircleCircleIntersection(arc1.RadiusPoint.ToVector(), arc1.Radius, arc2.RadiusPoint.ToVector(), arc2.Radius,
                out var circleIntersections)) { return false; }

            double start0 = DegreeToRadian(arc1.StartAngle);
            double end0 = DegreeToRadian(arc1.EndAngle);
            double start1 = DegreeToRadian(arc2.StartAngle);
            double end1 = DegreeToRadian(arc2.EndAngle);

            foreach (var pt in circleIntersections)
            {
                double a0 = GetAngle(arc1.RadiusPoint.ToVector(), pt);
                double a1 = GetAngle(arc2.RadiusPoint.ToVector(), pt);

                if (IsAngleBetween(a0, start0, end0) && IsAngleBetween(a1, start1, end1))
                { intersections.Add(pt); }
            }

            return intersections.Count > 0;
        }


        // Helpers
        private static double ClampAngle(double angle, double min, double max)
        {
            // Normalize angle to [0, 2π)
            double twoPi = Math.PI * 2;
            angle = (angle % twoPi + twoPi) % twoPi;
            min = (min % twoPi + twoPi) % twoPi;
            max = (max % twoPi + twoPi) % twoPi;

            if (min <= max) { return Math.Clamp(angle, min, max); }
            else
            {
                // Handle arcs that wrap around 2π
                return (angle >= min || angle <= max) ? angle :
                       (GetAngleDistance(angle, min) < GetAngleDistance(angle, max) ? min : max);
            }
        }
        private static double GetAngleDistance(double a, double b)
        {
            return Math.Min(Math.Abs(a - b), 2 * Math.PI - Math.Abs(a - b));
        }
        public static bool GetLineCircleIntersection(Vector lineStart, Vector lineEnd, Vector circleCenter, double radius, out List<Vector> intersections)
        {
            intersections = [];

            Vector d = lineEnd - lineStart;
            Vector f = lineStart - circleCenter;

            double a = Vector.Multiply(d, d);
            double b = 2 * Vector.Multiply(f, d);
            double c = Vector.Multiply(f, f) - radius * radius;

            double discriminant = b * b - 4 * a * c;
            if (discriminant < 0) { return false; }

            discriminant = Math.Sqrt(discriminant);

            double t1 = (-b - discriminant) / (2 * a);
            double t2 = (-b + discriminant) / (2 * a);

            bool found = false;

            if (t1 >= 0 && t1 <= 1)
            {
                intersections.Add(lineStart + t1 * d);
                found = true;
            }

            if (t2 >= 0 && t2 <= 1 && t2 != t1)
            {
                intersections.Add(lineStart + t2 * d);
                found = true;
            }

            return found;
        }

        public static bool GetCircleCircleIntersection(Vector c0, double r0, Vector c1, double r1, out List<Vector> intersections)
        {
            intersections = [];

            Vector d = c1 - c0;
            double dist = d.Length;

            if (dist > r0 + r1 || dist < Math.Abs(r0 - r1) || dist == 0f)
            { return false; } // No intersection or coincident

            double a = (r0 * r0 - r1 * r1 + dist * dist) / (2 * dist);
            double h = Math.Sqrt(r0 * r0 - a * a);

            Vector p2 = c0 + a / dist * d;
            Vector offset = h / dist * new Vector(-d.Y, d.X); // Perpendicular vector

            intersections.Add(p2 + offset);
            if (h > 1e-5f) // Avoid duplicate points
            { intersections.Add(p2 - offset); }

            return true;
        }
        private static bool IsPointOnSegment(Vector a, Vector b, Vector p, double epsilon = 0.0001)
        {
            Vector ab = b - a;
            Vector ap = p - a;

            // 1. Collinearity check via cross product
            double cross = ab.X * ap.Y - ab.Y * ap.X;
            if (Math.Abs(cross) > epsilon) { return false; }

            // 2. Bounds check via dot product
            double dot = Vector.Multiply(ap, ab);
            if (dot < -epsilon) { return false; } // Before 'a'
            if (dot > Vector.Multiply(ab, ab) + epsilon) { return false; } // Beyond 'b'

            return true;
        }

        private static double DegreeToRadian(double degrees) => degrees * Math.PI / 180;

        private static bool IsAngleBetween(double angle, double start, double end)
        {
            if (start <= end) { return angle >= start && angle <= end; }
            else { return angle >= start || angle <= end; }
        }

        private static double GetAngle(Vector center, Vector point)
        {
            double angle = Math.Atan2(point.Y - center.Y, point.X - center.X);
            if (angle < 0) angle += 2 * Math.PI;
            return angle;
        }
    }
}
