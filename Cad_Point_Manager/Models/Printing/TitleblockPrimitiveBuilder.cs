using PdfSharpCore.Drawing;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Cad_Point_Manager.Models.Printing
{
    public static class TitleblockPrimitiveBuilder
    {
        // constants (inches) copied from your XAML
        public const double PageW = 36;
        public const double PageH = 24;

        private const double _marginLeft = 0.05;

        public static List<TbPrimitive> Build(Layout layout, byte[] logoBytes)
        {
            var a = layout.Attributes; // adjust if your model differs
            var layoutFontFamily = layout.FontFamily.Source;
            List<TbPrimitive> p = [];

            double borderStrokeThickness = 0.01;
            XPen borderPen = new(XColors.Black, borderStrokeThickness);
            XPen halfBorderPen = new(XColors.Black, borderStrokeThickness / 2);
            XSolidBrush templateHatchBrush = new(XColor.FromArgb(255, 91, 91, 91));
            XSolidBrush attributesBrush = new(XColor.FromArgb(255, 0, 0, 0));

            // Logo cell border + image
            p.Add(new TbImage(29.5, 0.5, 6.0, 6.185, logoBytes));

            // NOTES header bar
            p.Add(new TbText(32.5, 6.9715, 6.0, 0.573,
                "NOTES", layoutFontFamily, 0.35, attributesBrush,
                Bold: false, Align: TbAlign.MiddleCenter));

            // Notes body box (just draw the text inside; you control wrapping)
            p.Add(new TbText(
                29.55, 7.3085, 5.9, 5.4085,
                a?.Notes?.Text ?? "",
                layoutFontFamily, 0.25, attributesBrush,
                Align: TbAlign.TopLeft));

            // Header row text
            p.Add(new TbText(32.5, 13.392, 6, 0.75,
                "DRAWINGS BASED ON:", layoutFontFamily, 0.35,
                attributesBrush, Align: TbAlign.MiddleCenter));

            // Column header row (row 2)
            p.Add(new TbText(31.875, 14.017, 4.75, 0.75, "DRAWING DESCRIPTION", layoutFontFamily, 0.25, attributesBrush, Align: TbAlign.MiddleCenter));
            p.Add(new TbText(34.875, 14.017, 1.25, 0.75, "DATE", layoutFontFamily, 0.25, attributesBrush, Align: TbAlign.MiddleCenter));

            // Rows 3..8: 6 lines of desc/date
            (string desc, string date)[] rows = [
            (a?.DrawingDesc1?.Text ?? "", a?.DrawingDate1?.Text ?? ""),
            (a?.DrawingDesc2?.Text ?? "", a?.DrawingDate2?.Text ?? ""),
            (a?.DrawingDesc3?.Text ?? "", a?.DrawingDate3?.Text ?? ""),
            (a?.DrawingDesc4?.Text ?? "", a?.DrawingDate4?.Text ?? ""),
            (a?.DrawingDesc5?.Text ?? "", a?.DrawingDate5?.Text ?? ""),
            (a?.DrawingDesc6?.Text ?? "", a?.DrawingDate6?.Text ?? "")];

            double gx = 29.5; double gy = 14.267;
            double h = 0.75;
            for (int i = 0; i < 6; i++)
            {
                p.Add(new TbText(gx + _marginLeft, gy + (h / 2) + (h * i), 4.75, h, rows[i].desc, layoutFontFamily, 0.25, attributesBrush, Align: TbAlign.MiddleLeft));
                p.Add(new TbText(gx + 5.375, gy + (h / 2) + (h * i), 1.25, h, rows[i].date, layoutFontFamily, 0.25, attributesBrush, Align: TbAlign.MiddleCenter));
            }

            // Main outer border
            p.Add(new TbRect(0.438, 0.438, 35.125, 23.125, borderPen, null));

            // Viewport border
            p.Add(new TbRect(0.5, 0.5, 28.93, 23, borderPen, null));

            // Titleblock information border
            p.Add(new TbRect(29.5, 0.5, 6, 23, borderPen, null));

            // Image divider
            p.Add(new TbLine(29.5, 6.685, 35.5, 6.685, borderPen));

            // Notes dividers
            p.Add(new TbLine(29.5, 7.258, 35.5, 7.258, borderPen));
            p.Add(new TbLine(29.5, 12.767, 35.5, 12.767, borderPen));

            // Hatch strip #1 dividers
            p.Add(new TbLine(29.5, 13.017, 35.5, 13.017, borderPen));

            double rowYStart = 12.767; double rowXStart = 29.5;
            p.Add(new TbRect(rowXStart, rowYStart, 0.336, 0.125, halfBorderPen, templateHatchBrush));
            p.Add(new TbRect(rowXStart + 0.336, rowYStart + 0.125, 0.336, 0.125, halfBorderPen, templateHatchBrush));
            p.Add(new TbRect(rowXStart + 0.672, rowYStart, 0.336, 0.125, halfBorderPen, templateHatchBrush));
            p.Add(new TbRect(rowXStart + 1.008, rowYStart + 0.125, 0.623, 0.125, halfBorderPen, templateHatchBrush));
            p.Add(new TbRect(rowXStart + 1.631, rowYStart, 0.623, 0.125, halfBorderPen, templateHatchBrush));
            p.Add(new TbRect(rowXStart + 2.254, rowYStart + 0.125, 0.623, 0.125, halfBorderPen, templateHatchBrush));
            p.Add(new TbRect(rowXStart + 2.877, rowYStart, 0.623, 0.125, halfBorderPen, templateHatchBrush));
            p.Add(new TbRect(rowXStart + 3.500, rowYStart + 0.125, 1.25, 0.125, halfBorderPen, templateHatchBrush));
            p.Add(new TbRect(rowXStart + 4.750, rowYStart, 1.25, 0.125, halfBorderPen, templateHatchBrush));

            // Drawings table dividers
            p.Add(new TbLine(29.5, 13.767, 35.5, 13.767, borderPen));
            p.Add(new TbLine(29.5, 14.267, 35.5, 14.267, borderPen));
            p.Add(new TbLine(29.5, 15.017, 35.5, 15.017, borderPen));
            p.Add(new TbLine(29.5, 15.767, 35.5, 15.767, borderPen));
            p.Add(new TbLine(29.5, 16.517, 35.5, 16.517, borderPen));
            p.Add(new TbLine(29.5, 17.267, 35.5, 17.267, borderPen));
            p.Add(new TbLine(29.5, 18.017, 35.5, 18.017, borderPen));
            p.Add(new TbLine(29.5, 18.767, 35.5, 18.767, borderPen));

            p.Add(new TbLine(34.25, 13.767, 34.25, 18.767, borderPen));

            // Hatch strip #2 dividers
            p.Add(new TbLine(29.5, 19.017, 35.5, 19.017, borderPen));
            rowYStart = 18.767;
            p.Add(new TbRect(rowXStart, rowYStart, 0.336, 0.125, halfBorderPen, templateHatchBrush));
            p.Add(new TbRect(rowXStart + 0.336, rowYStart + 0.125, 0.336, 0.125, halfBorderPen, templateHatchBrush));
            p.Add(new TbRect(rowXStart + 0.672, rowYStart, 0.336, 0.125, halfBorderPen, templateHatchBrush));
            p.Add(new TbRect(rowXStart + 1.008, rowYStart + 0.125, 0.623, 0.125, halfBorderPen, templateHatchBrush));
            p.Add(new TbRect(rowXStart + 1.631, rowYStart, 0.623, 0.125, halfBorderPen, templateHatchBrush));
            p.Add(new TbRect(rowXStart + 2.254, rowYStart + 0.125, 0.623, 0.125, halfBorderPen, templateHatchBrush));
            p.Add(new TbRect(rowXStart + 2.877, rowYStart, 0.623, 0.125, halfBorderPen, templateHatchBrush));
            p.Add(new TbRect(rowXStart + 3.500, rowYStart + 0.125, 1.25, 0.125, halfBorderPen, templateHatchBrush));
            p.Add(new TbRect(rowXStart + 4.750, rowYStart, 1.25, 0.125, halfBorderPen, templateHatchBrush));

            // Bottom section text
            p.Add(new TbText(30.344, 19.267, 1.688, 0.5,
                "DRAWN BY:", layoutFontFamily, 0.25,
                attributesBrush, Align: TbAlign.MiddleCenter));
            p.Add(new TbText(31.188 + _marginLeft, 19.267, 4.312, 0.5,
                a?.DrawnBy?.Text ?? "", layoutFontFamily, 0.25,
                attributesBrush, Align: TbAlign.MiddleLeft));

            p.Add(new TbText(30.344, 19.767, 1.688, 0.5,
                "DATE:", layoutFontFamily, 0.25,
                attributesBrush, Align: TbAlign.MiddleCenter));
            p.Add(new TbText(31.188 + _marginLeft, 19.767, 4.312, 0.5,
                a?.DateDrawn?.Text ?? "", layoutFontFamily, 0.25,
                attributesBrush, Align: TbAlign.MiddleLeft));

            p.Add(new TbText(30.344, 20.517, 1.688, 1,
                "PROJECT:", layoutFontFamily, 0.25,
                attributesBrush, Align: TbAlign.MiddleCenter));
            p.Add(new TbText(31.188 + _marginLeft, 20.517, 4.312, 1,
                a?.ProjectName?.Text ?? "", layoutFontFamily, 0.25,
                attributesBrush, Align: TbAlign.MiddleLeft));

            p.Add(new TbText(32.5, 21.517, 6, 1,
                a?.PageTitle?.Text ?? "", layoutFontFamily, 0.25,
                attributesBrush, Align: TbAlign.MiddleCenter));

            p.Add(new TbText(32.5, 22.517, 6, 1,
               a?.PageNumber?.Text ?? "", layoutFontFamily, 0.5,
               attributesBrush, Align: TbAlign.MiddleCenter));

            Debug.WriteLine($"a.PageNumber.Text: {a.PageNumber.Text}");

            p.Add(new TbText(32.5, 23.2585, 6, 1,
               a?.Scale?.Text ?? "", layoutFontFamily, 0.25,
               attributesBrush, Align: TbAlign.MiddleCenter));

            // Bottom section dividers
            p.Add(new TbLine(29.5, 19.517, 35.5, 19.517, borderPen));       // Drawn by divider
            p.Add(new TbLine(29.5, 20.017, 35.5, 20.017, borderPen));       // Date drawn divider
            p.Add(new TbLine(29.5, 21.017, 35.5, 21.017, borderPen));       // Project name divider
            p.Add(new TbLine(29.5, 22.017, 35.5, 22.017, borderPen));       // Page title divider
            p.Add(new TbLine(29.5, 23.017, 35.5, 23.017, borderPen));       // Page number divider
            p.Add(new TbLine(29.5, 23.017, 35.5, 23.017, borderPen));       // Vertical divider 1
            p.Add(new TbLine(31.188, 19.017, 31.188, 21.017, borderPen));   // Vertical divider 2

            return p;
        }
    }

    public enum TbAlign
    {
        TopLeft, 
        TopCenter, 
        TopRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight,
    }

    public abstract record TbPrimitive;

    public record TbRect(
        double X, double Y, double W, double H,
        XPen? StrokePen = null,
        XBrush? FillBrush = null
    ) : TbPrimitive;

    public record TbLine(
        double X1, double Y1, double X2, double Y2,
        XPen StrokePen
    ) : TbPrimitive;

    public record TbText(
        double X, double Y, double W, double H,
        string Text,
        string FontFamily,
        double FontSizeIn,
        XBrush FontBrush,
        bool Bold = false,
        TbAlign Align = TbAlign.BottomLeft
    ) : TbPrimitive;

    public record TbImage(
        double X, double Y, double W, double H,
        byte[] ImageBytes // already-resolved bytes (jpg/png)
    ) : TbPrimitive;
}
