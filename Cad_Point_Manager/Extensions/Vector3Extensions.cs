using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Extensions
{
    public static class Vector3Extensions
    {
        public static Vector2 ToVector2(this Vector3 v)
        {
            return new Vector2(v.X, v.Y);
        }

        public static System.Windows.Point ToPoint(this Vector3 v)
        {
            return new System.Windows.Point(v.X, v.Y);
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
            return Vector2.Distance(v1.ToVector2(), v2.ToVector2());
        }
    }
}
