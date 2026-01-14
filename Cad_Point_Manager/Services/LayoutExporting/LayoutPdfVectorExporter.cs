using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.DrawingObjects3D;
using Cad_Point_Manager.Models.Printing;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Cad_Point_Manager.Services.LayoutExporting
{
    public class LayoutPdfVectorExporter : ILayoutPdfVectorExporter
    {
        public Task ExportAsync(Layout layout, CadManager cadManager3D, Scene scene, string outputPath, CancellationToken ct = default)
        {
            return Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var viewSize = new Size(cadManager3D.ViewportSize.Width, cadManager3D.ViewportSize.Height);
                double pageWpts = layout.PageSize.Width * 72.0;
                double pageHpts = layout.PageSize.Height * 72.0;

                Rect pdfViewportPts = new Rect(
                    layout.Viewport.LocalRectIn.X * 72.0,
                    layout.Viewport.LocalRectIn.Y * 72.0,
                    layout.Viewport.LocalRectIn.Width * 72.0,
                    layout.Viewport.LocalRectIn.Height * 72.0);
                Matrix worldToPdf = LayoutPdfMatrixBuilder.BuildWorldToPdfContain_YDown(scene.Bounds.ToRect(), pdfViewportPts);

                LayoutPdfVectorExporter.Export(
                    layout,
                    cadManager3D.Layers.Select(kvp => kvp.Value),
                    worldToPdf,
                    outputPath);

            }).Task;
        }

        public static void Export(
            Layout layout,
            IEnumerable<ObjectLayer> layers,
            Matrix worldToPdf,
            string outputPdfPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPdfPath)!);

            double pageWpts = layout.PageSize.Width * 72.0;
            double pageHpts = layout.PageSize.Height * 72.0;

            using var doc = new PdfDocument();
            var page = doc.AddPage();
            page.Width = pageWpts;
            page.Height = pageHpts;

            using var gfx = XGraphics.FromPdfPage(page);
            gfx.DrawRectangle(XBrushes.White, 0, 0, pageWpts, pageHpts);

            foreach (var layer in layers)
            {
                if (!layer.IsVisible) continue;

                foreach (var obj in layer.DrawingObjects)
                {
                    var pen = new XPen(PdfTransform.ToXColor(obj.Color.ToVector4()), 0.25);

                    switch (obj)
                    {
                        case DrawingLine line:
                            DrawLine(gfx, line, worldToPdf, pen);
                            break;

                        case DrawingPolyline pl:
                            DrawPolyline(gfx, pl.Vertices, worldToPdf, pen);
                            break;

                        case DrawingArc arc:
                            DrawPolyline(gfx, arc.Vertices, worldToPdf, pen);
                            break;

                        case DrawingCircle circle:
                            DrawPolyline(gfx, circle.Vertices, worldToPdf, pen);
                            break;

                        case DrawingMtext mtext:
                            DrawMtext(gfx, mtext, worldToPdf);
                            break;
                    }
                }
            }
            doc.Save(outputPdfPath);
        }

        static void DrawLine(XGraphics gfx, DrawingLine line, Matrix worldToPdf, XPen pen)
        {
            var p0 = PdfTransform.WorldToPdf(line.Start.ToVector3(), worldToPdf);
            var p1 = PdfTransform.WorldToPdf(line.End.ToVector3(), worldToPdf);
            gfx.DrawLine(pen, p0, p1);
        }

        static void DrawPolyline(XGraphics gfx, LineVertex[] verts, Matrix worldToPdf, XPen pen)
        {
            if (verts == null || verts.Length < 2) { return; }

            for (int i = 0; i + 1 < verts.Length; i += 2)
            {
                var a = PdfTransform.WorldToPdf(verts[i].Position.ToVector3(), worldToPdf);
                var b = PdfTransform.WorldToPdf(verts[i + 1].Position.ToVector3(), worldToPdf);
                gfx.DrawLine(pen, a, b);
            }
        }

        static void DrawMtext(XGraphics gfx, DrawingMtext mtext, Matrix worldToPdf)
        {
            var verts = mtext.TextVertices;
            if (verts == null || verts.Count < 3) return;

            var brush = new XSolidBrush(PdfTransform.ToXColor(mtext.Color.ToVector4()));
            var path = new XGraphicsPath();

            for (int i = 0; i + 2 < verts.Count; i += 3)
            {
                var p0 = PdfTransform.WorldToPdf(verts[i].Position.ToVector3(), worldToPdf);
                var p1 = PdfTransform.WorldToPdf(verts[i + 1].Position.ToVector3(), worldToPdf);
                var p2 = PdfTransform.WorldToPdf(verts[i + 2].Position.ToVector3(), worldToPdf);
                path.AddPolygon(new[] { p0, p1, p2 });
            }

            gfx.DrawPath(brush, path);
        }
    }
}