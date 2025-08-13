using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cad_Point_Manager.Extensions
{
    public static class RectExtensions
    {
        public static Rect Zero => new(0, 0, 0, 0);

        public static Point Center(this Rect rect)
        {
            return new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        }
    }
}
