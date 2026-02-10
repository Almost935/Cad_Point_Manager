using PdfSharpCore.Drawing;
using System.Windows;
using System.Windows.Media;

namespace Cad_Point_Manager.Helpers
{
    public static class PdfDrawingHelpers
    {
        public static double NormalizeDeg(double deg)
        {
            deg %= 360.0;
            if (deg < 0) { deg += 360.0; }
            return deg;
        }
        public static XPoint WorldToPdf(SharpDX.Vector2 w, Matrix worldToPdf)
        {
            var p = worldToPdf.Transform(new Point(w.X, w.Y));
            return new XPoint(p.X, p.Y);
        }
        public static double WorldToPdfScale(Matrix worldToPdf)
        {
            return Math.Sqrt(worldToPdf.M21 * worldToPdf.M21 + worldToPdf.M22 * worldToPdf.M22);
        }
        public static double GetWorldToPdfScale(Matrix worldToPdf)
        {
            // pdf points per 1 world unit (uses transformed unit vector)
            var p0 = worldToPdf.Transform(new Point(0, 0));
            var p1 = worldToPdf.Transform(new Point(1, 0));
            double dx = p1.X - p0.X;
            double dy = p1.Y - p0.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Converts a "target text height in world units" into a PDF XFont size in points,
        /// by calibrating against PDFSharp's MeasureString height.
        /// </summary>
        public static double ComputeCalibratedFontSizePts(
            XGraphics gfx,
            string fontFamily,
            XFontStyle style,
            double targetHeightPts)
        {
            // trial size in points (big enough to reduce quantization)
            const double trialPts = 100.0;

            var trialFont = new XFont(fontFamily, trialPts, style);

            // PDFSharp's measurement model is not DirectWrite, so we calibrate.
            double measuredTrialHeightPts = gfx.MeasureString("Ag", trialFont).Height;
            if (measuredTrialHeightPts <= 0.001) { measuredTrialHeightPts = trialPts; }

            double sizePts = trialPts * (targetHeightPts / measuredTrialHeightPts);
            return sizePts;
        }
    }
}
