using SharpDX.Mathematics.Interop;
using SharpDX.Direct2D1;
using System.Windows;
using SharpDX;
using Point = System.Windows.Point;

namespace Cad_Point_Manager.Helpers
{
    public static class MathHelpers
    {
        public static bool IsGeometryInRect(RawRectangleF viewport, Geometry geometry, float strokeThickness)
        {
            // Attempt to get the bounds of the geometry
            var bounds = geometry.GetWidenedBounds(strokeThickness);

            // Check if the bounds intersect with the viewport
            return bounds.Left < viewport.Right &&
                   bounds.Right > viewport.Left &&
                   bounds.Top < viewport.Bottom &&
                   bounds.Bottom > viewport.Top;
        }

        public static bool IsLineInRect(Rect rect, Point startPoint, Point endPoint)
        {
            // Check if either of the line's endpoints are within the rectangle
            if (rect.Contains(startPoint) || rect.Contains(endPoint))
            {
                return true;
            }

            // Check if the line intersects any of the rectangle's sides
            return LineIntersectsRect(rect, startPoint, endPoint);
        }

        public static bool LineIntersectsRect(Rect rect, Point startPoint, Point endPoint)
        {
            // Define the rectangle's corners
            var topLeft = new Point(rect.Left, rect.Top);
            var topRight = new Point(rect.Right, rect.Top);
            var bottomLeft = new Point(rect.Left, rect.Bottom);
            var bottomRight = new Point(rect.Right, rect.Bottom);

            // Check for intersection with each side of the rectangle
            return LinesIntersect(startPoint, endPoint, topLeft, topRight) ||
                   LinesIntersect(startPoint, endPoint, topRight, bottomRight) ||
                   LinesIntersect(startPoint, endPoint, bottomRight, bottomLeft) ||
                   LinesIntersect(startPoint, endPoint, bottomLeft, topLeft);
        }

        public static bool LinesIntersect(Point p1, Point p2, Point q1, Point q2)
        {
            double d1 = CrossProduct(p1, p2, q1);
            double d2 = CrossProduct(p1, p2, q2);
            double d3 = CrossProduct(q1, q2, p1);
            double d4 = CrossProduct(q1, q2, p2);

            if (d1 * d2 < 0 && d3 * d4 < 0)
            {
                return true;
            }

            return false;
        }

        public static double CrossProduct(Point a, Point b, Point c)
        {
            return (b.Y - a.Y) * (c.X - b.X) - (b.X - a.X) * (c.Y - b.Y);
        }

        public static float GetZoom(float zoomFactor, int zoomStep, int numOfDigits)
        {
            return (float)Math.Round(Math.Pow(zoomFactor, zoomStep), numOfDigits);
        }

        public static bool RectsIntersect(Rect rect1, Rect rect2)
        {
            if (rect1.IntersectsWith(rect2) || rect1.Contains(rect2) || rect2.Contains(rect1))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Calculates the number of times a size must be divided by 2 to smaller than a certain length.
        /// </summary>
        /// <param name="overallSize">The size to be divided.</param>
        /// <param name="maxQuadTreeNodeSize">The max length any one dimension of the size can be.</param>
        /// <returns></returns>
        public static (int x, int y) GetRequiredQuadTreeLevels(Size2F overallSize, float maxQuadTreeNodeSize)
        {
            // Calculate number of divisions required to be below the min in each direction
            double levelsX = Math.Log((overallSize.Width / maxQuadTreeNodeSize), 2);
            if (levelsX < 0) { levelsX = 0; }
            else { levelsX = Math.Ceiling(levelsX); }

            double levelsY = Math.Log((overallSize.Height / maxQuadTreeNodeSize), 2);
            if (levelsY < 0) { levelsY = 0; }
            else { levelsY = Math.Ceiling(levelsY); }

            return ((int)levelsX, (int)levelsY);
        }

        /// <summary>
        /// Determines if a point lies on a line segment defined by two endpoints.
        /// </summary>
        /// <param name="px">X-coordinate of the point.</param>
        /// <param name="py">Y-coordinate of the point.</param>
        /// <param name="x1">X-coordinate of the first endpoint of the line.</param>
        /// <param name="y1">Y-coordinate of the first endpoint of the line.</param>
        /// <param name="x2">X-coordinate of the second endpoint of the line.</param>
        /// <param name="y2">Y-coordinate of the second endpoint of the line.</param>
        /// <param name="tolerance">Allowed tolerance for floating-point comparison.</param>
        /// <returns>True if the point is on the line; otherwise, false.</returns>
        public static bool IsPointOnLine(Point p, Point s, Point e, double tolerance = 0.01)
        {
            double SE = PointToPointDistance(s, e);
            double SP = PointToPointDistance(s, p);
            double EP = PointToPointDistance(e, p);

            if (SP + EP - SE < tolerance) { return true; }
            if (SP <= tolerance || EP <= tolerance) { return true; }
            return false;
        }

        /// <summary>
        /// Determines if a point lies on a circle.
        /// </summary>
        /// <param name="px">X-coordinate of the point.</param>
        /// <param name="py">Y-coordinate of the point.</param>
        /// <param name="cx">X-coordinate of the circle's center.</param>
        /// <param name="cy">Y-coordinate of the circle's center.</param>
        /// <param name="radius">Radius of the circle.</param>
        /// <param name="tolerance">Allowed tolerance for floating-point comparison.</param>
        /// <returns>True if the point is on the circle; otherwise, false.</returns>
        public static bool IsPointOnCircle(double px, double py, double cx, double cy, double radius, double tolerance = 0.01)
        {
            // Calculate the distance from the point to the circle's center
            double distance = Math.Sqrt(Math.Pow(px - cx, 2) + Math.Pow(py - cy, 2));

            // Check if the distance is approximately equal to the radius
            return Math.Abs(distance - radius) <= tolerance;
        }

        /// <summary>
        /// Determines if a point lies on a circular arc defined by its center, radius, and angular range.
        /// </summary>
        /// <param name="px">X-coordinate of the point to check.</param>
        /// <param name="py">Y-coordinate of the point to check.</param>
        /// <param name="cx">X-coordinate of the center of the arc.</param>
        /// <param name="cy">Y-coordinate of the center of the arc.</param>
        /// <param name="radius">Radius of the arc.</param>
        /// <param name="startAngle">Start angle of the arc, in degrees (0 degrees is along the positive X-axis).</param>
        /// <param name="endAngle">End angle of the arc, in degrees (measured counterclockwise).</param>
        /// <param name="tolerance">Allowed tolerance for distance and angle checks, to account for floating-point inaccuracies.</param>
        /// <returns>True if the point lies on the arc; otherwise, false.</returns>
        /// <remarks>
        /// This method performs two checks:
        /// 1. It verifies if the point lies on the circle defined by the arc's center and radius, within the specified tolerance.
        /// 2. It checks if the point's angle relative to the arc's center falls within the arc's angular bounds.
        /// The angular bounds consider cases where the arc spans across the 360-degree boundary.
        /// </remarks>
        public static bool IsPointOnArc(double px, double py, double cx, double cy, double radius, double startAngle, double endAngle, double tolerance = 0.01)
        {
            // Step 1: Check if the point is on the circle
            double distance = Math.Sqrt(Math.Pow(px - cx, 2) + Math.Pow(py - cy, 2));
            if (Math.Abs(distance - radius) > tolerance)
                return false;

            // Step 2: Calculate the angle of the point relative to the arc's center
            double angle = Math.Atan2(py - cy, px - cx) * (180.0 / Math.PI); // Convert to degrees
            angle = (angle + 360) % 360; // Normalize angle to [0, 360)

            // Normalize the start and end angles to [0, 360)
            startAngle = (startAngle + 360) % 360;
            endAngle = (endAngle + 360) % 360;

            // Check if the angle falls within the arc's bounds
            if (startAngle <= endAngle)
            {
                // Arc does not cross 360 boundary
                return angle >= startAngle && angle <= endAngle;
            }
            else
            {
                // Arc crosses 360 boundary
                return angle >= startAngle || angle <= endAngle;
            }
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

        public static double PointToRectDistance(Rect rect, Point point)
        {
            // Clamp point to rect bounds
            double clampedX = Math.Max(rect.Left, Math.Min(point.X, rect.Right));
            double clampedY = Math.Max(rect.Top, Math.Min(point.Y, rect.Bottom));

            // If the point is inside the rect, distance is 0
            if (rect.Contains(point))
                return 0;

            // Compute Euclidean distance from the point to the closest point on the rect
            double dx = point.X - clampedX;
            double dy = point.Y - clampedY;

            return Math.Sqrt(dx * dx + dy * dy);
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
