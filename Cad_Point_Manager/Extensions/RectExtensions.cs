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

        // "Touch" selection: any overlap or containment in either direction
        public static bool Touches(this Rect a, Rect b)
        {
            if (a.IsEmpty || b.IsEmpty) return false;
            return a.IntersectsWith(b) || a.Contains(b) || b.Contains(a);
        }

        // "Contain" selection: a must fully contain b
        public static bool FullyContains(this Rect a, Rect b)
        {
            if (a.IsEmpty || b.IsEmpty) return false;
            return a.Contains(b);
        }

        public static SharpDX.RectangleF ToRectangeF(this Rect rect)
        {
            return new SharpDX.RectangleF(rect.Left.ToFloat(), rect.Top.ToFloat(), rect.Width.ToFloat(), rect.Height.ToFloat());
        }
    }
}
