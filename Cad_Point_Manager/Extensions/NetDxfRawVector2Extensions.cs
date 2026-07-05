using SharpDX.Mathematics.Interop;

namespace Cad_Point_Manager.Extensions
{
    public static class NetDxfRawVector2Extensions
    {
        public static string ToFormattedString(this RawVector2 v)
        {
            return $"(x, y): ({v.X:F3}, {v.Y:F3})";
        }
    }
}
