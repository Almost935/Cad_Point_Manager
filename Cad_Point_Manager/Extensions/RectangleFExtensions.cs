using SharpDX;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Extensions
{
    public static class RectangleFExtensions
    {
        public static RectangleF Transform(this RectangleF rect, Matrix3x2 m)
        {
            // Original corners
            var tl = new Vector2(rect.Left, rect.Top);
            var tr = new Vector2(rect.Right, rect.Top);
            var bl = new Vector2(rect.Left, rect.Bottom);
            var br = new Vector2(rect.Right, rect.Bottom);

            // Transform corners manually (Matrix3x2 layout: M11 M12 / M21 M22 / M31 M32)
            tl = TransformPoint(tl, m);
            tr = TransformPoint(tr, m);
            bl = TransformPoint(bl, m);
            br = TransformPoint(br, m);

            // Build axis-aligned bounding box of transformed points
            float minX = Math.Min(Math.Min(tl.X, tr.X), Math.Min(bl.X, br.X));
            float maxX = Math.Max(Math.Max(tl.X, tr.X), Math.Max(bl.X, br.X));
            float minY = Math.Min(Math.Min(tl.Y, tr.Y), Math.Min(bl.Y, br.Y));
            float maxY = Math.Max(Math.Max(tl.Y, tr.Y), Math.Max(bl.Y, br.Y));

            return new RectangleF(
                minX,
                minY,
                maxX - minX,
                maxY - minY);
        }

        private static Vector2 TransformPoint(Vector2 p, Matrix3x2 m)
        {
            float x = p.X * m.M11 + p.Y * m.M21 + m.M31;
            float y = p.X * m.M12 + p.Y * m.M22 + m.M32;
            return new Vector2(x, y);
        }
    }
}
