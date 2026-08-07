using netDxf;
using SharpDX.Mathematics.Interop;

namespace Cad_Point_Manager.Extensions
{
    public static class NetDxfVector2Extensions
    {
        public static SharpDX.Vector3 ToSharpDXVector3(this Vector2 v, float elevation = 0.0f)
        {
            return new SharpDX.Vector3(v.X.ToFloat(), v.Y.ToFloat(), elevation);
        }
        public static SharpDX.Vector2 ToSharpDXVector2(this Vector2 v)
        {
            return new SharpDX.Vector2(v.X.ToFloat(), v.Y.ToFloat());
        }
        public static RawVector2 ToRawVector2(this Vector2 v)
        {
            return new RawVector2(v.X.ToFloat(), v.Y.ToFloat());
        }

        public static string ToFormattedString(this Vector2 v)
        {
            return $"({v.X.ToFloat():F3}, {v.Y.ToFloat():F3})";
        }
    }
}
