using SharpDX;
using SharpDX.Mathematics.Interop;
using System.Windows;

namespace Cad_Point_Manager.Extensions
{
    public static class SharpDXVector3Extensions
    {
        public static Vector2 ToSharpDXVector2(this Vector3 v)
        {
            return new Vector2(v.X, v.Y);
        }

        public static RawVector2 ToRawVector2(this Vector3 v)
        {
            return new RawVector2(v.X, v.Y);
        }

        public static System.Windows.Point ToPoint(this Vector3 v)
        {
            return new System.Windows.Point(v.X, v.Y);
        }

        public static netDxf.Vector3 ToNetDxfVector3(this Vector3 v)
        {
            return new netDxf.Vector3(v.X, v.Y, v.Z);
        }
        public static netDxf.Vector2 ToNetDxfVector2(this Vector3 v)
        {
            return new netDxf.Vector2(v.X, v.Y);
        }
        public static Vector ToVector(this Vector3 v)
        {
            return new Vector(v.X, v.Y);
        }

        public static System.Numerics.Vector2 ToVector2(this Vector3 v)
        {
            return new System.Numerics.Vector2(v.X, v.Y);
        }

        public static System.Numerics.Vector3 ToVector3(this Vector3 v)
        {
            return new System.Numerics.Vector3(v.X, v.Y, v.Z);
        }

        public static bool EqualsWithTolerance(this Vector3 v1, Vector3 v2, float tolerance)
        {
            return Math.Abs(v1.X - v2.X) <= tolerance &&
                   Math.Abs(v1.Y - v2.Y) <= tolerance &&
                   Math.Abs(v1.Z - v2.Z) <= tolerance;
        }

        public static bool EqualsWithTolerance2D(this Vector3 v1, Vector3 v2, float tolerance)
        {
            return Math.Abs(v1.X - v2.X) <= tolerance &&
                   Math.Abs(v1.Y - v2.Y) <= tolerance;
        }
        public static float GetDistance2D(this Vector3 v1, Vector3 v2)
        {
            return Vector2.Distance(v1.ToSharpDXVector2(), v2.ToSharpDXVector2());
        }
    }
}
