using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Helpers;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.PointRendering;
using Cad_Point_Manager.Models.Printing;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Cad_Point_Manager.Services.Exporting
{
    public class LayoutPdfVectorExporter
    {
        private const double PdfPointsPerInch = 72;

        static double Pt(double inches) => inches * PdfPointsPerInch;
        static readonly Dictionary<short, XGraphicsPath> _duGlyphPathCache = [];

        public Task ExportAsync(
            Layout layout,
            CadManager cadManager3D,
            Scene scene,
            List<TbPrimitive> templatePrims, string outputPath,
            bool openAfterExport = false)
        {
            return Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var viewSize = new Size(cadManager3D.ViewportSize.Width, cadManager3D.ViewportSize.Height);
                double pageWpts = layout.PageWidth * PdfPointsPerInch;
                double pageHpts = layout.PageHeight * PdfPointsPerInch;

                Matrix worldToPdf = BuildWorldToPdfFromCamera(layout, cadManager3D, scene.Bounds);

                LayoutPdfVectorExporter.Export(
                    layout,
                    cadManager3D,
                    templatePrims,
                    worldToPdf,
                    outputPath,
                    openAfterExport);
            }).Task;
        }

        public static void Export(
            Layout layout,
            CadManager cadManager,
            List<TbPrimitive> templatePrims,
            Matrix worldToPdf,
            string outputPdfPath,
            bool openAfterExport = false)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPdfPath)!);

            double pageWpts = layout.PageWidth * PdfPointsPerInch;
            double pageHpts = layout.PageHeight * PdfPointsPerInch;

            using var doc = new PdfDocument();
            var page = doc.AddPage();
            page.Width = pageWpts;
            page.Height = pageHpts;

            using var gfx = XGraphics.FromPdfPage(page);
            gfx.DrawRectangle(XBrushes.White, 0, 0, pageWpts, pageHpts);

            // --- CLIP TO VIEWPORT FOR CAD ONLY ---
            var viewportClip = new XRect(
                layout.Viewport.LocalRectIn.X * PdfPointsPerInch,
                layout.Viewport.LocalRectIn.Y * PdfPointsPerInch,
                layout.Viewport.LocalRectIn.Width * PdfPointsPerInch,
                layout.Viewport.LocalRectIn.Height * PdfPointsPerInch);

            // Capture graphics state *object* so we can restore the exact state.
            XGraphicsState cadClipState = gfx.Save();
            gfx.IntersectClip(viewportClip);

            foreach (var kv in cadManager.Layers)
            {
                var layer = kv.Value;
                if (!layer.IsVisible) { continue; }

                foreach (var obj in layer.DrawingObjects)
                {
                    var pen = new XPen(PdfTransform.ToXColor(obj.GetColor().ToVector4()), 0.0);
                    obj.DrawToPdf(gfx, worldToPdf, pen);
                }
            }

            DrawCogoPointStrings(gfx, cadManager, worldToPdf);

            gfx.Restore(cadClipState);
            DrawTitleblockPdf(gfx, templatePrims);
            doc.Save(outputPdfPath);

            _duGlyphPathCache.Clear();

            if (openAfterExport)
            {
                FileHelpers.TryOpenFile(outputPdfPath);
            }
        }

        public static MemoryStream ExportToStream(Layout layout, CadManager cadManager, Matrix worldToPdf)
        {
            Stopwatch sw = Stopwatch.StartNew();

            var stream = new MemoryStream();

            double pageWpts = layout.PageWidth * PdfPointsPerInch;
            double pageHpts = layout.PageHeight * PdfPointsPerInch;

            using var doc = new PdfDocument();
            var page = doc.AddPage();
            page.Width = pageWpts;
            page.Height = pageHpts;

            using var gfx = XGraphics.FromPdfPage(page);
            gfx.DrawRectangle(XBrushes.White, 0, 0, pageWpts, pageHpts);

            var viewportClip = new XRect(
                layout.Viewport.LocalRectIn.X * PdfPointsPerInch,
                layout.Viewport.LocalRectIn.Y * PdfPointsPerInch,
                layout.Viewport.LocalRectIn.Width * PdfPointsPerInch,
                layout.Viewport.LocalRectIn.Height * PdfPointsPerInch);

            gfx.IntersectClip(viewportClip);

            foreach (var kv in cadManager.Layers)
            {
                var layer = kv.Value;
                if (!layer.IsVisible) { continue; }

                foreach (var obj in layer.DrawingObjects)
                {
                    var pen = new XPen(PdfTransform.ToXColor(obj.GetColor().ToVector4()), 0.0);
                    obj.DrawToPdf(gfx, worldToPdf, pen);
                }
            }

            DrawCogoPointStrings(gfx, cadManager, worldToPdf);

            doc.Save(stream, false);
            stream.Position = 0;

            _duGlyphPathCache.Clear();

            sw.Stop();
            Debug.WriteLine($"PDF export completed in {sw.Elapsed.TotalSeconds:F2} seconds.");

            return stream;
        }

        public static MemoryStream ExportViewportPreviewToStream(
            Layout layout, CadManager cadManager, Matrix worldToPdf)
        {
            var stream = new MemoryStream();

            using var doc = new PdfDocument();
            var page = doc.AddPage();
            double vpWpts = layout.Viewport.LocalRectIn.Width * PdfPointsPerInch;
            double vpHpts = layout.Viewport.LocalRectIn.Height * PdfPointsPerInch;

            page.Width = vpWpts;
            page.Height = vpHpts;

            using var gfx = XGraphics.FromPdfPage(page);

            gfx.DrawRectangle(XBrushes.White, 0, 0, vpWpts, vpHpts);

            foreach (var kv in cadManager.Layers)
            {
                var layer = kv.Value;
                if (!layer.IsVisible) { continue; }

                foreach (var obj in layer.DrawingObjects)
                {
                    var pen = new XPen(PdfTransform.ToXColor(obj.GetColor().ToVector4()), 0.0);
                    obj.DrawToPdf(gfx, worldToPdf, pen);
                }
            }

            DrawCogoPointStrings(gfx, cadManager, worldToPdf);

            doc.Save(stream, false);

            stream.Position = 0;

            return stream;
        }

        private static void DrawCogoPointStrings(XGraphics gfx, CadManager cadManager, Matrix worldToPdf)
        {
            const string fontFamily = "Arial";

            double scaleX = PointsPerWorldUnitX(worldToPdf);
            double scaleY = PointsPerWorldUnitY(worldToPdf);
            double fontScale = PointsPerWorldUnit(worldToPdf);

            foreach (var pg in cadManager.PointGroups)
            {
                if (pg is null || !pg.IsVisible)
                    continue;

                foreach (var cp in cadManager.GetPoints(pg))
                {
                    if (cp is null)
                        continue;

                    var color = GetCogoPointColor(cp);
                    var brush = new XSolidBrush(color);
                    var pen = new XPen(color, 0.25);

                    double pointScale = pg.PointScale;

                    DrawLabel(cp.PointNumber.ToString(), 0);

                    if (cp.IsElevationVisible)
                        DrawLabel(cp.Elevation.ToString("F3"), 1);

                    if (cp.IsDescriptionVisible)
                        DrawLabel(cp.Description, 2);

                    Vector2d center = cp.Position.Transform(worldToPdf);
                    double radiusWorld = GlobalHelperProperties.CogoPointCirclePixelRadius * pointScale;

                    double rx = radiusWorld * scaleX;
                    double ry = radiusWorld * scaleY;

                    gfx.DrawEllipse(brush, center.X - rx, center.Y - ry, rx * 2, ry * 2);

                    if (cp.HasLeaderLine)
                    {
                        Vector2d start = cp.Position.Transform(worldToPdf);
                        Vector2d end = (cp.Position + cp.TextInfoOffset).Transform(worldToPdf);

                        gfx.DrawLine(pen, new XPoint(start.X, start.Y), new XPoint(end.X, end.Y));
                    }

                    void DrawLabel(string text, int line)
                    {
                        if (string.IsNullOrEmpty(text))
                            return;

                        Vector2d labelWorld = cp.GetLabelOffset(line);
                        Vector2d labelPdf = labelWorld.Transform(worldToPdf);

                        double fontSizePts = Math.Max(0.1, pg.FontBaseSize * pointScale * fontScale);
                        var font = new XFont(fontFamily, fontSizePts, XFontStyle.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode));

                        gfx.DrawString(text, font, brush, new XPoint(labelPdf.X, labelPdf.Y), XStringFormats.BaseLineLeft);
                    }
                }
            }
        }

        private static void DrawTitleblockPdf(XGraphics gfx, IEnumerable<TbPrimitive> prims)
        {
            foreach (var prim in prims)
            {
                switch (prim)
                {
                    case TbRect r:
                        {
                            if (r.FillBrush is not null)
                            {
                                gfx.DrawRectangle(r.StrokePen, r.FillBrush,
                                    Pt(r.X), Pt(r.Y), Pt(r.W), Pt(r.H));
                            }
                            else
                            {
                                gfx.DrawRectangle(r.StrokePen, Pt(r.X), Pt(r.Y), Pt(r.W), Pt(r.H));
                            }
                            break;
                        }
                    case TbLine l:
                        {
                            gfx.DrawLine(l.StrokePen, Pt(l.X1), Pt(l.Y1), Pt(l.X2), Pt(l.Y2));
                            break;
                        }
                    case TbText t:
                        {
                            var style = t.Bold ? XFontStyle.Bold : XFontStyle.Regular;
                            var font = new XFont(t.FontFamily, Pt(t.FontSizeIn), style);

                            // IMPORTANT: rect is derived from anchor semantics of Align
                            var rect = AnchorRect(t.X, t.Y, t.W, t.H, t.Align);

                            DrawWrappedText(gfx, t.Text ?? "", font, t.FontBrush, rect, t.Align, clipToRect: true);
                            break;
                        }
                    case TbImage img:
                        {
                            using var ms = new MemoryStream(img.ImageBytes);
                            using var xi = XImage.FromStream(() => ms);
                            gfx.DrawImage(xi, Pt(img.X), Pt(img.Y), Pt(img.W), Pt(img.H));
                            break;
                        }
                }
            }
        }
        private static void DrawWrappedText(
            XGraphics gfx,
             string text,
             XFont font,
             XBrush brush,
             XRect rect,
             TbAlign align,
             bool clipToRect = true)
        {
            if (clipToRect)
            {
                gfx.Save();
                gfx.IntersectClip(rect);
            }

            var lines = WrapLinesNoJustify(gfx, font, text ?? "", rect.Width);

            double lineH = gfx.MeasureString("Ag", font).Height;
            double ascent = lineH * 0.83; // baseline approximation
            double textH = lines.Count * lineH;

            // vertical alignment of the BLOCK within rect
            double yStart = align switch
            {
                TbAlign.TopLeft or TbAlign.TopCenter or TbAlign.TopRight => rect.Y,
                TbAlign.MiddleLeft or TbAlign.MiddleCenter or TbAlign.MiddleRight => rect.Y + Math.Max(0, (rect.Height - textH) / 2),
                TbAlign.BottomLeft or TbAlign.BottomCenter or TbAlign.BottomRight => rect.Y + Math.Max(0, rect.Height - textH),
                _ => rect.Y
            };

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                double yTop = yStart + i * lineH;

                if (yTop + lineH > rect.Bottom) { break; }

                double lineW = gfx.MeasureString(line, font).Width;

                double x = align switch
                {
                    TbAlign.TopCenter or TbAlign.MiddleCenter or TbAlign.BottomCenter => rect.X + (rect.Width - lineW) / 2,
                    TbAlign.TopRight or TbAlign.MiddleRight or TbAlign.BottomRight => rect.X + (rect.Width - lineW),
                    _ => rect.X
                };

                gfx.DrawString(line, font, brush, new XPoint(x, yTop + ascent));
            }

            if (clipToRect) gfx.Restore();
        }

        private static XMatrix BuildDuToPdf(
            double duToWorldBase,
            double penDU,
            double ySign,
            CogoPoint cp,
            int line,
            Matrix worldToPdf)
        {
            var offset = cp.GetLabelOffset(line);

            double duToWorld = duToWorldBase * cp.PointGroup.PointScale;

            // --- DU -> WORLD (WPF matrix math) ---
            var duToWorldM = new Matrix(
                duToWorld, 0, 0, duToWorld * ySign, offset.X + penDU * duToWorld, offset.Y);

            // Compose with WORLD -> PDF (still WPF)
            duToWorldM.Append(worldToPdf);

            // --- CONVERT WPF Matrix -> XMatrix ---
            return new XMatrix(
                duToWorldM.M11,
                duToWorldM.M12,
                duToWorldM.M21,
                duToWorldM.M22,
                duToWorldM.OffsetX,
                duToWorldM.OffsetY);
        }

        private static XColor GetCogoPointColor(CogoPoint cp)
        {
            if (cp.IsSelected)
            {
                return PdfTransform.ToXColor(
                    GlobalHelperProperties.SelectedObjectColor.ToVector4());
            }

            return PdfTransform.ToXColor(
                cp.PointGroup.Color.ToVector4());
        }
        private static List<string> WrapLinesNoJustify(XGraphics gfx, XFont font, string text, double maxWidthPts)
        {
            var lines = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
                return lines;

            // normalize newlines + tabs
            text = text.Replace("\r\n", "\n").Replace("\t", "    ");

            foreach (var paragraph in text.Split('\n'))
            {
                // preserve blank lines
                if (paragraph.Length == 0)
                {
                    lines.Add(string.Empty);
                    continue;
                }

                var words = paragraph.Split(' ', StringSplitOptions.None); // keep empty entries for multiple spaces
                var current = new StringBuilder();

                for (int i = 0; i < words.Length; i++)
                {
                    // Rebuild the original spacing: each token after the first gets a single space.
                    // If you truly want to preserve multiple spaces, we can—most titleblocks don’t need it.
                    var token = words[i];
                    var candidate = current.Length == 0 ? token : current + " " + token;

                    double w = gfx.MeasureString(candidate, font).Width;

                    if (w <= maxWidthPts || current.Length == 0)
                    {
                        current.Clear();
                        current.Append(candidate);
                    }
                    else
                    {
                        lines.Add(current.ToString());
                        current.Clear();
                        current.Append(token);
                    }
                }

                if (current.Length > 0) { lines.Add(current.ToString()); }
            }

            return lines;
        }
        private static XRect AnchorRect(double xIn, double yIn, double wIn, double hIn, TbAlign align)
        {
            double x = Pt(xIn);
            double y = Pt(yIn);
            double w = Pt(wIn);
            double h = Pt(hIn);

            // Convert anchor (x,y) to top-left rect (rx, ry)
            double rx = x;
            double ry = y;

            switch (align)
            {
                case TbAlign.TopLeft:
                    // already top-left
                    break;

                case TbAlign.TopCenter:
                    rx = x - w / 2;
                    break;

                case TbAlign.TopRight:
                    rx = x - w;
                    break;

                case TbAlign.MiddleLeft:
                    ry = y - h / 2;
                    break;

                case TbAlign.MiddleCenter:
                    rx = x - w / 2;
                    ry = y - h / 2;
                    break;

                case TbAlign.MiddleRight:
                    rx = x - w;
                    ry = y - h / 2;
                    break;

                case TbAlign.BottomLeft:
                    ry = y - h;
                    break;

                case TbAlign.BottomCenter:
                    rx = x - w / 2;
                    ry = y - h;
                    break;

                case TbAlign.BottomRight:
                    rx = x - w;
                    ry = y - h;
                    break;
            }

            return new XRect(rx, ry, w, h);
        }

        private static double PointsPerWorldUnit(Matrix worldToPdf)
        {
            double dx = worldToPdf.M21;
            double dy = worldToPdf.M22;
            double len = Math.Sqrt(dx * dx + dy * dy);
            return len <= 0 ? 1.0 : len;
        }

        static double PointsPerWorldUnitX(Matrix worldToPdf)
        {
            double dx = worldToPdf.M11;
            double dy = worldToPdf.M12;
            double len = Math.Sqrt(dx * dx + dy * dy);
            return len <= 0 ? 1.0 : len;
        }

        static double PointsPerWorldUnitY(Matrix worldToPdf)
        {
            double dx = worldToPdf.M21;
            double dy = worldToPdf.M22;
            double len = Math.Sqrt(dx * dx + dy * dy);
            return len <= 0 ? 1.0 : len;
        }

        public static Matrix BuildWorldToPdfFromCamera(Layout layout, CadManager cadManager, Rect bounds)
        {
            if (layout is null)
                throw new ArgumentNullException(nameof(layout));

            if (cadManager is null)
                throw new ArgumentNullException(nameof(cadManager));

            if (bounds.IsEmpty ||
                bounds.Width <= 0 ||
                bounds.Height <= 0)
            {
                throw new ArgumentException(
                    "Invalid scene bounds.", nameof(bounds));
            }

            var vp = layout.Viewport.LocalRectIn;

            // PDF viewport coordinates in points.
            double vpX = vp.X * PdfPointsPerInch;
            double vpY = vp.Y * PdfPointsPerInch;
            double vpW = vp.Width * PdfPointsPerInch;
            double vpH = vp.Height * PdfPointsPerInch;

            if (vpW <= 0 || vpH <= 0)
                throw new ArgumentException("Invalid PDF viewport.");

            const double paddingFraction = 0.02;

            double worldW = bounds.Width * (1.0 + 2.0 * paddingFraction);
            double worldH = bounds.Height * (1.0 + 2.0 * paddingFraction);

            double worldCenterX = bounds.X + bounds.Width * 0.5;
            double worldCenterY = bounds.Y + bounds.Height * 0.5;

            // Uniform scale, preserving the drawing aspect ratio.
            double scale = Math.Min(
                vpW / worldW,
                vpH / worldH);

            // Position the drawing center in the PDF viewport center.
            double pdfCenterX = vpX + vpW * 0.5;
            double pdfCenterY = vpY + vpH * 0.5;

            // World: Y increases upward.
            // PDF:   Y increases downward.
            return new Matrix(
                scale, 0,
                0, -scale,
                pdfCenterX - worldCenterX * scale,
                pdfCenterY + worldCenterY * scale);
        }

    }
}