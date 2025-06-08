using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Point = System.Windows.Point;

namespace Cad_Point_Manager.Extensions
{
    public static class VectorExtensions
    {
        public static Point ToPoint(this Vector v)
        {
            return new Point(v.X, v.Y);
        }

        public static Vector3 ToVector3(this Vector v, float elevation = 0.0f)
        {
            return new Vector3((float)v.X, (float)v.Y, elevation);
        }

        public static Vector2 ToVector2(this Vector v)
        {
            return new Vector2((float)v.X, (float)v.Y);
        }

        public static bool EqualsWithTolerance(this Vector2 v1, Vector2 v2, float tolerance)
        {
            return Math.Abs(v1.X - v2.X) <= tolerance &&
                   Math.Abs(v1.Y - v2.Y) <= tolerance;
        }
    }
}
