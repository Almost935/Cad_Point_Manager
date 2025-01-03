using SharpDX.Mathematics.Interop;
using SharpDX.Direct2D1;
using System.Windows;
using SharpDX;
using Point = System.Windows.Point;
using System.Diagnostics;

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
        public static bool IsPointOnLine(double px, double py, double x1, double y1, double x2, double y2, double tolerance = 0.01)
        {
            double crossProduct = (py - y1) * (x2 - x1) - (px - x1) * (y2 - y1);
            Debug.WriteLine($"CrossProduct: {crossProduct}");
            if (Math.Abs(crossProduct) > tolerance)
                return false;

            double dotProduct = (px - x1) * (x2 - x1) + (py - y1) * (y2 - y1);
            Debug.WriteLine($"DotProduct: {dotProduct}");
            if (dotProduct < 0)
                return false;

            double squaredLength = Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2);
            Debug.WriteLine($"SquaredLength: {squaredLength}");
            if (dotProduct > squaredLength)
                return false;

            return true;
        }

        //public static bool IsPointOnLine(Point point, Point start, Point end, double tolerance)
        //{
        //    double m = (end.Y - start.Y) / (end.X - start.X);

        //    if (double.IsInfinity(m))
        //    {
        //        return Math.Abs(point.X - start.X) <= tolerance;
        //    }

        //    double c = start.Y - (m * start.X);

        //    double d = Math.Abs(point.Y - ((m * point.X) + c));

        //    //Debug.WriteLine($"m: {m} c: {c} d: {d}");
        //    //Debug.WriteLine($"d: {d} tolerance: {tolerance}");

        //    // If (x, y) satisfies the equation
        //    // of the line
        //    if (d <= tolerance)
        //        return true;

        //    return false;
        //}

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
    }
}
