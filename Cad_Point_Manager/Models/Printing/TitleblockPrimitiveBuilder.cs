using PdfSharpCore.Drawing;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.Printing
{
    public static class TitleblockPrimitiveBuilder
    {
        // constants (inches) copied from your XAML
        public const double PageW = 36;
        public const double PageH = 24;

        public static List<TbPrimitive> Build(Layout layout, byte[] logoBytes)
        {
            var a = layout.Attributes; // adjust if your model differs

            List<TbPrimitive> p = [];

            double borderStrokeThickness = 0.01;
            TbColor templateHatchFill = new(255, 91, 91, 91);

            // Logo cell border + image
            //p.Add(new TbRect(29.5, 0.5, 6.0, 6.185, StrokeIn: 0.01));
            p.Add(new TbImage(29.5, 0.5, 6.0, 6.185, logoBytes));

            // NOTES header bar
            //p.Add(new TbRect(29.5, 6.685, 6.0, 0.573, StrokeIn: 0.01));
            p.Add(new TbText(
                29.5, 6.685, 6.0, 0.573,
                "NOTES",
                "Arial", 0.35, TbColor.Black, 
                Bold: false, Align: TbAlign.MiddleCenter));

            // Notes body box (just draw the text inside; you control wrapping)
            //p.Add(new TbRect(29.5, 7.2585, 6.0, 5.5085, StrokeIn: 0.01));
            p.Add(new TbText(
                29.55, 7.3085, 5.9, 5.4085,
                a?.Notes?.Text ?? "",
                "Arial", 0.25, TbColor.Black, 
                Align: TbAlign.TopLeft));

            // --- “Drawings based on” table ---
            double gx = 29.5;
            double gy = 12.767;

            // Column widths: 4.75 + 1.25
            double c0 = 4.75, c1 = 1.25;

            // Row heights (from your XAML rows 0..9)
            double[] rh = { 0.25, 0.75, 0.5, 0.75, 0.75, 0.75, 0.75, 0.75, 0.75, 0.25 };

            // Helper to sum row offsets
            double RowTop(int r) { double t = 0; for (int i = 0; i < r; i++) t += rh[i]; return t; }

            // Top hatch strip (draw as filled rect; you can add diagonal hatch later)
            //p.Add(new TbRect(gx, gy + RowTop(0), c0 + c1, rh[0], StrokeIn: 0.01, FillGray: 0.9));

            // Header row text
            //p.Add(new TbRect(gx, gy + RowTop(1), c0 + c1, rh[1], StrokeIn: 0.01));
            p.Add(new TbText(gx, gy + RowTop(1), c0 + c1, rh[1],
                "DRAWINGS BASED ON:", "Arial", 0.35, TbColor.Black, Align: TbAlign.MiddleCenter));

            // Column header row (row 2)
            //p.Add(new TbRect(gx, gy + RowTop(2), c0, rh[2], StrokeIn: 0.01));
            //p.Add(new TbRect(gx + c0, gy + RowTop(2), c1, rh[2], StrokeIn: 0.01));
            p.Add(new TbText(gx, gy + RowTop(2), c0, rh[2], "DRAWING DESCRIPTION", "Arial", 0.25, TbColor.Black, Align: TbAlign.MiddleCenter));
            p.Add(new TbText(gx + c0, gy + RowTop(2), c1, rh[2], "DATE", "Arial", 0.25, TbColor.Black, Align: TbAlign.MiddleCenter));

            // Rows 3..8: 6 lines of desc/date
            (string desc, string date)[] rows =
            [
            (a?.DrawingDesc1?.Text ?? "", a?.DrawingDate1?.Text ?? ""),
            (a?.DrawingDesc2?.Text ?? "", a?.DrawingDate2?.Text ?? ""),
            (a?.DrawingDesc3?.Text ?? "", a?.DrawingDate3?.Text ?? ""),
            (a?.DrawingDesc4?.Text ?? "", a?.DrawingDate4?.Text ?? ""),
            (a?.DrawingDesc5?.Text ?? "", a?.DrawingDate5?.Text ?? ""),
            (a?.DrawingDesc6?.Text ?? "", a?.DrawingDate6?.Text ?? "")
            ];

            for (int i = 0; i < 6; i++)
            {
                int r = 3 + i;

                // slight inset padding
                p.Add(new TbText(gx + 0.05, gy + RowTop(r) + 0.05, c0 - 0.1, rh[r] - 0.1, rows[i].desc, "Arial", 0.25, TbColor.Black, Align: TbAlign.MiddleLeft));
                p.Add(new TbText(gx + c0, gy + RowTop(r) + 0.05, c1, rh[r] - 0.1, rows[i].date, "Arial", 0.25, TbColor.Black, Align: TbAlign.MiddleCenter));
            }

            // Main outer border
            p.Add(new TbRect(0.438, 0.438, 35.125, 23.125, borderStrokeThickness, TbColor.Black, null));

            // Viewport border
            p.Add(new TbRect(0.5, 0.5, 28.93, 23, borderStrokeThickness, TbColor.Black, null));

            // Titleblock information border
            p.Add(new TbRect(29.5, 0.5, 6, 23, borderStrokeThickness, TbColor.Black, null));

            // Image divider
            p.Add(new TbLine(29.5, 6.685, 35.5, 6.685, borderStrokeThickness, TbColor.Black));

            // Notes dividers
            p.Add(new TbLine(29.5, 7.258, 35.5, 7.258, borderStrokeThickness, TbColor.Black));
            p.Add(new TbLine(29.5, 12.767, 35.5, 12.767, borderStrokeThickness, TbColor.Black));

            // Hatch strip dividers
            p.Add(new TbLine(29.5, 12.892, 35.5, 12.892, borderStrokeThickness, TbColor.Black));
            p.Add(new TbLine(29.5, 13.017, 35.5, 13.017, borderStrokeThickness, TbColor.Black));

            p.Add(new TbRect(12.892, 0.5, 0.336, 0.125, borderStrokeThickness / 2, TbColor.Red, templateHatchFill));

            return p;
        }
    }

    public enum TbAlign
    {
        TopLeft, TopCenter, TopRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        BottomLeft, BottomCenter, BottomRight,
    }

    public abstract record TbPrimitive;

    public record TbRect(
        double X, double Y, double W, double H,
        double StrokeIn,
        TbColor? StrokeColor = null,
        TbColor? FillColor = null
    ) : TbPrimitive;

    public record TbLine(
        double X1, double Y1, double X2, double Y2,
        double StrokeIn,
        TbColor StrokeColor
    ) : TbPrimitive;

    public record TbText(
        double X, double Y, double W, double H,
        string Text,
        string FontFamily,
        double FontSizeIn,
        TbColor FontColor,
        bool Bold = false,
        TbAlign Align = TbAlign.BottomLeft
    ) : TbPrimitive;

    public record TbImage(
        double X, double Y, double W, double H,
        byte[] ImageBytes // already-resolved bytes (jpg/png)
    ) : TbPrimitive;

    public readonly record struct TbColor(byte A, byte R, byte G, byte B)
    {
        public static TbColor Black => new(255, 0, 0, 0);
        public static TbColor White => new(255, 255, 255, 255);
        public static TbColor Red => new(255, 255, 0, 0);
        public static TbColor Blue => new(255, 0, 0, 255);

        public bool IsTransparent => A == 0;

        public XColor XColor => XColor.FromArgb(A, R, G, B);
        public XBrush XBrush => new XSolidBrush(XColor);
    }
}
