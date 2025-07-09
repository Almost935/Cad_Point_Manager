using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace Cad_Point_Manager.Extensions
{
    public static class ColorExtensions
    {
        public static SolidColorBrush ToMediaBrush(this SharpDX.Color color)
        {
            return new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
        }

        public static SolidColorBrush ToMediaBrush(this Vector4 colorVec)
        {
            return new SolidColorBrush(Color.FromArgb(
                (byte)(colorVec.W * 255),
                (byte)(colorVec.X * 255),
                (byte)(colorVec.Y * 255),
                (byte)(colorVec.Z * 255)));
        }
    }

}
