using SharpDX;

namespace Cad_Point_Manager.Extensions
{
    public static class SharpDXVector4Extensions
    {
        public static System.Numerics.Vector4 ToVector4(this Vector4 v)
        {
            return new System.Numerics.Vector4(v.X, v.Y, v.Z, v.W);
        }

        public static string ToFormattedString(this Vector4 v)
        {
            return $"(x, y, z, w): ({v.X:F3}, {v.Y:F3}, {v.Z:F3}, {v.W:F3})";
        }
    }
}
