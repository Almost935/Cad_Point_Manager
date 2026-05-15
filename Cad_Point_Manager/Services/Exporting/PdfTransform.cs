using PdfSharpCore.Drawing;
using System.Numerics;
using System.Windows.Media;

namespace Cad_Point_Manager.Services.Exporting
{
    public static class PdfTransform
    {
        public static XPoint WorldToPdf(Vector3 world,  Matrix worldToPdf)
        {
            var p = worldToPdf.Transform(new System.Windows.Point(world.X, world.Y));
            return new XPoint(p.X, p.Y);
        }

        public static XColor ToXColor(Vector4 c) =>
            XColor.FromArgb(255,
                (byte)(c.X * 255),
                (byte)(c.Y * 255),
                (byte)(c.Z * 255));
    }
}
