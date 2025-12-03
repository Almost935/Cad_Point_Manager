using Point = System.Windows.Point;
using Vector2 = System.Numerics.Vector2;

namespace Cad_Point_Manager.Extensions
{
    public static class Vector2Extensions
    {
        public static Point ToPoint(this Vector2 v)
        {
            return new Point(v.X, v.Y);
        }

        public static SharpDX.Vector3 ToSharpDXVector3(this Vector2 v, float elevation = 0.0f)
        {
            return new SharpDX.Vector3((float)v.X, (float)v.Y, elevation);
        }
        public static SharpDX.Vector2 ToSharpDXVector2(this Vector2 v)
        {
            return new SharpDX.Vector2((float)v.X, (float)v.Y);
        }

        public static bool EqualsWithTolerance(this Vector2 v1, Vector2 v2, float tolerance)
        {
            return Math.Abs(v1.X - v2.X) <= tolerance &&
                   Math.Abs(v1.Y - v2.Y) <= tolerance;
        }
    }
}
