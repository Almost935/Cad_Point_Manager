using Cad_Point_Manager.Common;
using netDxf;
using SharpDX.Mathematics.Interop;

namespace Cad_Point_Manager.Extensions
{
    public static class NetDxfVector2Extensions
    {
        public static SharpDX.Vector3 ToSharpDXVector3(this Vector2 v, float elevation = 0.0f)
        {
            return new SharpDX.Vector3((float)v.X, (float)v.Y, elevation);
        }
        public static SharpDX.Vector2 ToSharpDXVector2(this Vector2 v)
        {
            return new SharpDX.Vector2((float)v.X, (float)v.Y);
        }
        public static RawVector2 ToRawVector2(this Vector2 v)
        {
            return new RawVector2((float)v.X, (float)v.Y);
        }
        public static Vector2d ToVector2d(this Vector2 v)
        {
            return new Vector2d(v.X, v.Y);
        }

        public static string ToFormattedString(this Vector2 v)
        {
            return $"({(float)v.X:F3}, {(float)v.Y:F3})";
        }
    }
}
