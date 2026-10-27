using Cad_Point_Manager.Common;
using System.Runtime.CompilerServices;
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

        public static Rect Normalized(this Rect r)
        {
            if (r.IsEmpty) return Rect.Empty;
            var x1 = r.X; var y1 = r.Y;
            var x2 = r.X + r.Width; var y2 = r.Y + r.Height;
            var left = Math.Min(x1, x2);
            var top = Math.Min(y1, y2);
            var right = Math.Max(x1, x2);
            var bottom = Math.Max(y1, y2);

            return new Rect(new Point(left, top), new Point(right, bottom));
        }
        public static bool Touches(this Rect a, Rect b)
        {
            if (a.IsEmpty || b.IsEmpty) return false;
            return a.IntersectsWith(b) || a.Contains(b) || b.Contains(a);
        }
        public static bool FullyContains(this Rect a, Rect b)
        {
            if (a.IsEmpty || b.IsEmpty) return false;
            return a.Contains(b);
        }

        public static SharpDX.RectangleF ToRectangeF(this Rect rect)
        {
            return new SharpDX.RectangleF((float)rect.Left, (float)rect.Top, (float)rect.Width, (float)rect.Height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Union(this ref Rect rect, Vector2d point)
        {
            if (rect.IsEmpty)
            {
                rect = new Rect(point.X, point.Y, 0, 0);
                return;
            }

            double left = Math.Min(rect.Left, point.X);
            double top = Math.Min(rect.Top, point.Y);
            double right = Math.Max(rect.Right, point.X);
            double bottom = Math.Max(rect.Bottom, point.Y);

            rect = new Rect(left, top, right - left, bottom - top);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains(this Rect rect, Vector2d point)
            => !rect.IsEmpty &&
            point.X >= rect.Left &&
            point.X <= rect.Right &&
            point.Y >= rect.Top &&
            point.Y <= rect.Bottom;
    }
}
