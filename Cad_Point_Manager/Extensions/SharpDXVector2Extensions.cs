using SharpDX;
using System.Windows;
using Point = System.Windows.Point;

namespace Cad_Point_Manager.Extensions
{
    public static class SharpDXVector2Extensions
    {
        public static float Magnitude(this Vector2 v)
        {
            return (float)Math.Sqrt(v.X * v.X + v.Y * v.Y);
        }

        public static float MagnitudeSquared(this Vector2 v)
        {
            return v.X * v.X + v.Y * v.Y;
        }

        public static Point ToPoint(this Vector2 v)
        {
            return new Point(v.X, v.Y);
        }

        public static Vector3 ToSharpDXVector3(this Vector2 v, float elevation = 0.0f)
        {
            return new Vector3(v.X, v.Y, elevation);
        }

        public static Vector ToVector(this Vector2 v)
        {
            return new Vector(v.X, v.Y);
        }

        public static netDxf.Vector2 ToNetDxfVector2(this Vector2 v)
        {
            return new netDxf.Vector2(v.X, v.Y);
        }

        public static bool EqualsWithTolerance(this Vector2 v1, Vector2 v2, float tolerance)
        {
            return Math.Abs(v1.X - v2.X) <= tolerance &&
                   Math.Abs(v1.Y - v2.Y) <= tolerance;
        }
    }
}
