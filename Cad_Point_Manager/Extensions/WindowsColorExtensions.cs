using SharpDX;
using System.Windows.Media;

using Color = System.Windows.Media.Color;

namespace Cad_Point_Manager.Extensions
{
    public static class WindowsColorExtensions
    {
        public static Vector4 ToSharpDXVector4(this Color color)
        {
            return new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
        }
    }
}
