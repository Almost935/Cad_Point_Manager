using SharpDX;

using Point = System.Windows.Point;

namespace Cad_Point_Manager.Extensions
{
    public static class Vector2Extensions
    {
        public static Point ToVector2(this Vector2 v)
        {
            return new Point(v.X, v.Y);
        }

        public static Vector3 ToVector3(this Vector2 v, float elevation = 0.0f)
        {
            return new Vector3(v.X, v.Y, elevation);
        }
    }
}
