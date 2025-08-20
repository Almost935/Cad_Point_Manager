using SharpDX;
using System.Windows;
using Point = System.Windows.Point;

namespace Cad_Point_Manager.Extensions
{
    public static class PointExtensions
    {
        public static Vector2 ToSharpDXVector2(this Point p)
        {
            return new Vector2((float)p.X, (float)p.Y);
        }

        public static Vector3 ToSharpDXVector3(this Point p, float elevation = 0.0f)
        {
            return new Vector3((float)p.X, (float)p.Y, elevation);
        }

        public static Vector ToVector(this Point p)
        {
            return new Vector(p.X, p.Y);
        }
    }
}
