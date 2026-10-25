using Cad_Point_Manager.Common;
using PdfSharpCore.Drawing;
using System.Windows;
using System.Windows.Media;

namespace Cad_Point_Manager.Helpers
{
    public static class PdfDrawingHelpers
    {
        private static readonly Dictionary<string, string> PdfFontMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // SHX
                ["simplex"] = "Arial",
                ["simplex.shx"] = "Arial",
                ["txt"] = "Courier New",
                ["txt.shx"] = "Courier New",
                ["romans"] = "Times New Roman",
                ["romans.shx"] = "Times New Roman",

                // Arial variants
                ["Arial CE"] = "Arial",
                ["Arial CYR"] = "Arial",
                ["Arial Greek"] = "Arial",

                // AutoCAD aliases
                ["Standard"] = "Arial",
                ["Monotxt"] = "Courier New",

                // Vertical fonts
                ["@MS Gothic"] = "MS Gothic",
            };

        public static string GetFontFamily(string? fontName)
        {
            if (string.IsNullOrWhiteSpace(fontName))
                return "Arial";

            if (PdfFontMap.TryGetValue(fontName, out var mapped))
                return mapped;

            else
                return fontName;
        }
        public static double NormalizeDeg(double deg)
        {
            deg %= 360.0;
            if (deg < 0) { deg += 360.0; }
            return deg;
        }
        public static XPoint WorldToPdf(Vector2d w, Matrix worldToPdf)
        {
            var p = worldToPdf.Transform(new Point(w.X, w.Y));
            return new XPoint(p.X, p.Y);
        }
        public static double WorldToPdfScale(Matrix worldToPdf)
        {
            return Math.Sqrt(worldToPdf.M21 * worldToPdf.M21 + worldToPdf.M22 * worldToPdf.M22);
        }
    }
}
