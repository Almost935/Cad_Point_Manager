using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Models.DrawingObjects3D;
using SharpDX;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Windows;
using Point = System.Windows.Point;

namespace Cad_Point_Manager.Helpers
{
    public static class GeometryHelpers
    {
        public static List<Vector2> GetSignificantPointsList(List<DrawingSegment3D> segments)
        {
            var allPoints = new ConcurrentBag<Vector2>();
            Debug.WriteLine($"\n");

            Parallel.ForEach(Enumerable.Range(0, segments.Count), () => new List<Vector2>(),
                (i, state, localList) =>
                {
                    var segment1 = segments[i];

                    switch (segment1)
                    {
                        case DrawingLine3D line:
                            localList.AddRange(new[] { line.Start.ToVector2(), line.End.ToVector2(), line.MidPoint.ToVector2() });
                            break;
                        case DrawingArc3D arc:
                            localList.AddRange(new[] { arc.RadiusPoint.ToVector2(), arc.Start.ToVector2(), arc.End.ToVector2(), arc.MidPoint.ToVector2() });
                            break;
                        case DrawingCircle3D circle:
                            localList.Add(circle.RadiusPoint.ToVector2());
                            break;
                    }

                    for (int j = i + 1; j < segments.Count; j++)
                    {
                        var segment2 = segments[j];
                        var intersectionsExists = GeometryHelpers.IntersectGeometries(segment1, segment2, out var intersectionPoints);

                        Debug.WriteLine($"segment1.GetType(): {segment1.GetType()} segment2.GetType(): {segment2.GetType()}");
                        Debug.WriteLine($"segment1.Start: {segment1.Start} segment1.End: {segment1.End}");
                        Debug.WriteLine($"segment2.Start: {segment2.Start} segment2.End: {segment2.End}");
                        Debug.WriteLine($"intersectionsExists: {intersectionsExists}");

                        if (intersectionsExists)
                        {
                            foreach (var intersection in intersectionPoints)
                            {
                                Debug.WriteLine($"intersection: {intersection}");
                            }

                            localList.AddRange(intersectionPoints);
                        }
                    }

                    return localList;
                },
                localList => { foreach (var pt in localList) allPoints.Add(pt); });

            return allPoints.Distinct(new Vector2EqualityComparer(1e-5f)).ToList();
        }


        public static Vector2 GetNearestPiontOnGeometry(DrawingGeometry3D geometry, Vector2 point)
        {
            return geometry switch
            {
                DrawingLine3D line => NearestPointOnLine(point, line.Start.ToVector2(), line.End.ToVector2()),
                DrawingArc3D arc => NearestPointOnLine(point, arc.Start.ToVector2(), arc.End.ToVector2()),
                DrawingCircle3D circle => NearestPointOnCircle(point, circle.RadiusPoint.ToVector2(), circle.Radius),
            };
        }
        public static Vector2 NearestPointOnLine(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ap = new((float)(p.X - a.X), (float)(p.Y - a.Y));
            Vector2 ab = new((float)(b.X - a.X), (float)(b.Y - a.Y));

            float abLengthSquared = ab.LengthSquared();
            if (abLengthSquared == 0) return a;

            float t = Vector2.Dot(ap, ab) / abLengthSquared;
            t = Math.Clamp(t, 0, 1);

            return new(a.X + t * ab.X, a.Y + t * ab.Y);
        }
        public static Vector2 NearestPointOnArc(Vector2 p, Vector2 center, float radius, float startAngle, float endAngle)
        {
            // Vector from center to point
            float dx = p.X - center.X;
            float dy = p.Y - center.Y;

            float angleToPoint = (float)(Math.Atan2(dy, dx));
            float clampedAngle = ClampAngle(angleToPoint, startAngle, endAngle);

            float nearestX = center.X + radius * (float)Math.Cos(clampedAngle);
            float nearestY = center.Y + radius * (float)Math.Sin(clampedAngle);

            return new(nearestX, nearestY);
        }
        public static Vector2 NearestPointOnCircle(Vector2 p, Vector2 center, float radius)
        {
            float dx = p.X - center.X;
            float dy = p.Y - center.Y;
            float length = (float)(Math.Sqrt(dx * dx + dy * dy));

            if (length == 0) { return new(center.X + radius, center.Y); } // Arbitrary direction

            float scale = radius / length;

            return new(center.X + dx * scale, center.Y + dy * scale);
        }


        public static bool IntersectGeometries(DrawingGeometry3D geometry1, DrawingGeometry3D geometry2, out List<Vector2> intersections)
        {
            intersections = [];

            if (geometry1 is DrawingLine3D line1)
            {
                if (geometry2 is DrawingLine3D line2)
                {
                    if (TryIntersectLineLine(line1, line2, out Vector2 intersection))
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
        public static bool TryIntersectLineLine(DrawingLine3D line1, DrawingLine3D line2, out Vector2 intersection)
        {
            intersection = default;

            Vector2 p1 = line1.Start.ToVector2();
            Vector2 p2 = line1.End.ToVector2();
            Vector2 p3 = line2.Start.ToVector2();
            Vector2 p4 = line2.End.ToVector2();

            float A1 = p2.Y - p1.Y;
            float B1 = p1.X - p2.X;
            float C1 = A1 * p1.X + B1 * p1.Y;

            float A2 = p4.Y - p3.Y;
            float B2 = p3.X - p4.X;
            float C2 = A2 * p3.X + B2 * p3.Y;

            float det = A1 * B2 - A2 * B1;
            if (Math.Abs(det) < 1e-6f) { return false; } // Lines are parallel or coincident

            intersection = new Vector2(
                (B2 * C1 - B1 * C2) / det,
                (A1 * C2 - A2 * C1) / det
            );

            // Check that intersection lies on both segments (not just the lines)
            bool pointOnSegment1 = IsPointOnSegment(p1, p2, intersection);
            bool pointOnSegment2 = IsPointOnSegment(p3, p4, intersection);

            bool testPointOnSegment1 = IsPointOnSegment(p1.ToVector(), p2.ToVector(), intersection.ToVector());
            bool testPointOnSegment2 = IsPointOnSegment(p3.ToVector(), p4.ToVector(), intersection.ToVector());

            return pointOnSegment1 && pointOnSegment2;
        }

        public static bool TryIntersectLineCircle(DrawingLine3D line, DrawingCircle3D circle, out List<Vector2> intersections)
        {
            intersections = [];

            Vector2 d = (Vector2)(line.End - line.Start);
            Vector2 f = (Vector2)(line.Start - circle.RadiusPoint);

            float a = Vector2.Dot(d, d);
            float b = 2 * Vector2.Dot(f, d);
            float c = Vector2.Dot(f, f) - circle.Radius * circle.Radius;

            float discriminant = b * b - 4 * a * c;
            if (discriminant < 0) { return false; }

            discriminant = (float)Math.Sqrt(discriminant);

            float t1 = (-b - discriminant) / (2 * a);
            float t2 = (-b + discriminant) / (2 * a);

            bool found = false;

            if (t1 >= 0 && t1 <= 1)
            {
                intersections.Add((Vector2)line.Start + t1 * d);
                found = true;
            }

            if (t2 >= 0 && t2 <= 1 && t2 != t1)
            {
                intersections.Add((Vector2)line.Start + t2 * d);
                found = true;
            }

            return found;
        }

        public static bool TryIntersectLineArc(DrawingLine3D line, DrawingArc3D arc, out List<Vector2> intersections)
        {
            intersections = [];
            if (!GetLineCircleIntersection(line.Start.ToVector2(), line.End.ToVector2(), arc.RadiusPoint.ToVector2(), arc.Radius, out var circlePoints))
            { return false; }

            float startRad = DegreeToRadian(arc.StartAngle);
            float endRad = DegreeToRadian(arc.EndAngle);

            foreach (var pt in circlePoints)
            {
                Vector2 dir = pt - arc.RadiusPoint.ToVector2();
                float angle = (float)Math.Atan2(dir.Y, dir.X);
                if (angle < 0) { angle += 2 * MathF.PI; }

                if (IsAngleBetween(angle, startRad, endRad))
                { intersections.Add(pt); }
            }

            return intersections.Count > 0;
        }

        public static bool TryIntersectCircleCircle(DrawingCircle3D circle1, DrawingCircle3D circle2, out List<Vector2> intersections)
        {
            bool intersects = GetCircleCircleIntersection((Vector2)circle1.RadiusPoint, circle1.Radius, (Vector2)circle2.RadiusPoint, circle2.Radius, out intersections);

            return intersects;
        }

        public static bool TryIntersectArcCircle(DrawingArc3D arc, DrawingCircle3D circle, out List<Vector2> intersections)
        {
            intersections = [];
            if (!GetCircleCircleIntersection(arc.RadiusPoint.ToVector2(), arc.Radius, circle.RadiusPoint.ToVector2(), circle.Radius, out var points)) { return false; }

            float startRad = DegreeToRadian(arc.StartAngle);
            float endRad = DegreeToRadian(arc.EndAngle);

            foreach (var pt in points)
            {
                Vector2 dir = pt - arc.RadiusPoint.ToVector2();
                float angle = (float)Math.Atan2(dir.Y, dir.X);
                if (angle < 0) { angle += 2 * MathF.PI; }

                if (IsAngleBetween(angle, startRad, endRad)) { intersections.Add(pt); }
            }
            return intersections.Count > 0;
        }

        public static bool TryIntersectArcArc(DrawingArc3D arc1, DrawingArc3D arc2, out List<Vector2> intersections)
        {
            intersections = [];
            if (!GetCircleCircleIntersection(arc1.RadiusPoint.ToVector2(), arc1.Radius, arc2.RadiusPoint.ToVector2(), arc2.Radius, out var circleIntersections)) { return false; }

            float start0 = DegreeToRadian(arc1.StartAngle);
            float end0 = DegreeToRadian(arc1.EndAngle);
            float start1 = DegreeToRadian(arc2.StartAngle);
            float end1 = DegreeToRadian(arc2.EndAngle);

            foreach (var pt in circleIntersections)
            {
                float a0 = GetAngle(arc1.RadiusPoint.ToVector2(), pt);
                float a1 = GetAngle(arc2.RadiusPoint.ToVector2(), pt);

                if (IsAngleBetween(a0, start0, end0) && IsAngleBetween(a1, start1, end1))
                { intersections.Add(pt); }
            }

            return intersections.Count > 0;
        }


        // Helpers
        private static float ClampAngle(float angle, float min, float max)
        {
            // Normalize angle to [0, 2π)
            float twoPi = (float)(Math.PI * 2);
            angle = (angle % twoPi + twoPi) % twoPi;
            min = (min % twoPi + twoPi) % twoPi;
            max = (max % twoPi + twoPi) % twoPi;

            if (min <= max)
            { return Math.Clamp(angle, min, max); }
            else
            {
                // Handle arcs that wrap around 2π
                return (angle >= min || angle <= max) ? angle :
                       (GetAngleDistance(angle, min) < GetAngleDistance(angle, max) ? min : max);
            }
        }
        private static float GetAngleDistance(float a, float b)
        {
            return (float)(Math.Min(Math.Abs(a - b), 2 * Math.PI - Math.Abs(a - b)));
        }
        public static bool GetLineCircleIntersection(Vector2 lineStart, Vector2 lineEnd, Vector2 circleCenter, float radius, out List<Vector2> intersections)
        {
            intersections = [];

            Vector2 d = lineEnd - lineStart;
            Vector2 f = lineStart - circleCenter;

            float a = Vector2.Dot(d, d);
            float b = 2 * Vector2.Dot(f, d);
            float c = Vector2.Dot(f, f) - radius * radius;

            float discriminant = b * b - 4 * a * c;
            if (discriminant < 0) { return false; }

            discriminant = (float)Math.Sqrt(discriminant);

            float t1 = (-b - discriminant) / (2 * a);
            float t2 = (-b + discriminant) / (2 * a);

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

        public static bool GetCircleCircleIntersection(Vector2 c0, float r0, Vector2 c1, float r1, out List<Vector2> intersections)
        {
            intersections = [];

            Vector2 d = c1 - c0;
            float dist = d.Length();

            if (dist > r0 + r1 || dist < Math.Abs(r0 - r1) || dist == 0f)
            { return false; } // No intersection or coincident

            float a = (r0 * r0 - r1 * r1 + dist * dist) / (2 * dist);
            float h = MathF.Sqrt(r0 * r0 - a * a);

            Vector2 p2 = c0 + a / dist * d;
            Vector2 offset = h / dist * new Vector2(-d.Y, d.X); // Perpendicular vector

            intersections.Add(p2 + offset);
            if (h > 1e-5f) // Avoid duplicate points
            { intersections.Add(p2 - offset); }

            return true;
        }
        private static bool IsPointOnSegment(Vector2 a, Vector2 b, Vector2 p, float epsilon = 0.01f)
        {
            Vector2 ab = b - a;
            Vector2 ap = p - a;

            // 1. Collinearity check via cross product
            float cross = ab.X * ap.Y - ab.Y * ap.X;
            if (Math.Abs(cross) > epsilon) { return false; }

            // 2. Bounds check via dot product
            float dot = Vector2.Dot(ap, ab);
            if (dot < -epsilon) { return false; } // Before 'a'
            if (dot > Vector2.Dot(ab, ab) + epsilon) { return false; } // Beyond 'b'

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

        private static float DegreeToRadian(float degrees) => degrees * MathF.PI / 180f;

        private static bool IsAngleBetween(float angle, float start, float end)
        {
            if (start <= end) { return angle >= start && angle <= end; }
            else { return angle >= start || angle <= end; }
        }

        private static float GetAngle(Vector2 center, Vector2 point)
        {
            float angle = (float)Math.Atan2(point.Y - center.Y, point.X - center.X);
            if (angle < 0) angle += 2 * MathF.PI;
            return angle;
        }
    }
}
