using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Models.Printing;
using Cad_Point_Manager.Views.UserControls;

namespace Cad_Point_Manager.Services.LayoutExporting
{
    public static class LayoutPdfExporter
    {
        const double DipPerInch = 96.0;
        static double InToDip(double inches) => inches * DipPerInch;

        public static async Task ExportLayoutToPdfAsync(
            Layout layout,
            ILayoutRenderHost renderHost,
            string outputPdfPath,
            int dpi = 300)
        {
            if (layout == null) { throw new ArgumentNullException(nameof(layout)); }
            if (renderHost == null) { throw new ArgumentNullException(nameof(renderHost)); }
            if (string.IsNullOrWhiteSpace(outputPdfPath)) { throw new ArgumentNullException(nameof(outputPdfPath)); }

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                ExportOnUiThread(layout, renderHost, outputPdfPath, dpi);
            });
        }

        private static void ExportOnUiThread(Layout layout, ILayoutRenderHost renderHost, string outputPdfPath, int dpi)
        {
            var pageBitmap = RenderLayoutToBitmap(layout, renderHost, dpi);

            // TEST 1: inspect this PNG first
            SaveBitmapAsPng(pageBitmap, Path.ChangeExtension(outputPdfPath, ".debug_page.png"));

            // Encode bitmap -> PNG bytes
            byte[] pngBytes;
            using (var ms = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(pageBitmap));
                encoder.Save(ms);
                pngBytes = ms.ToArray();
            }

            using var doc = new PdfDocument();
            var page = doc.AddPage();

            page.Width = layout.PageSize.Width * 72.0;
            page.Height = layout.PageSize.Height * 72.0;

            using (var gfx = XGraphics.FromPdfPage(page))
            using (var img = XImage.FromStream(() => new MemoryStream(pngBytes)))
            {
                gfx.DrawImage(img, 0, 0, page.Width, page.Height);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPdfPath)!);
            doc.Save(outputPdfPath);

            // TEST 2: validate header
            var header = System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(outputPdfPath), 0, 4);
            System.Diagnostics.Debug.WriteLine($"PDF header: '{header}'"); // should be %PDF
        }

        private static RenderTargetBitmap RenderLayoutToBitmap(Layout layout, ILayoutRenderHost renderHost, int dpi)
        {
            const double DipPerInch = 96.0;
            static double InToDip(double inches) => inches * DipPerInch;

            // Output bitmap in pixels
            int pagePxW = (int)Math.Round((double)(layout.PageSize.Width * dpi));
            int pagePxH = (int)Math.Round((double)(layout.PageSize.Height * dpi));

            // WPF visuals measured/arranged in DIPs
            double pageDipW = InToDip(layout.PageSize.Width);
            double pageDipH = InToDip(layout.PageSize.Height);

            var titleblock = new BaseTitleblock
            {
                Width = pageDipW,
                Height = pageDipH
            };

            titleblock.Measure(new Size(pageDipW, pageDipH));
            titleblock.Arrange(new Rect(0, 0, pageDipW, pageDipH));
            titleblock.UpdateLayout();

            // These are in inches (as your code assumes)
            double vpWInches = titleblock.ViewportWidth;
            double vpHInches = titleblock.ViewportHeight;

            int vpPxW = (int)Math.Round(vpWInches * dpi);
            int vpPxH = (int)Math.Round(vpHInches * dpi);

            int maxTex = renderHost.MaxTextureSize;
            if (vpPxW > maxTex || vpPxH > maxTex)
            {
                double scale = Math.Min((double)maxTex / vpPxW, (double)maxTex / vpPxH);
                vpPxW = Math.Max(1, (int)Math.Floor(vpPxW * scale));
                vpPxH = Math.Max(1, (int)Math.Floor(vpPxH * scale));
            }

            if (layout.Viewport?.Scene == null)
                throw new InvalidOperationException("Layout.Viewport.Scene is null. Pick a Scene before exporting.");

            var viewportWb = new WriteableBitmap(vpPxW, vpPxH, dpi, dpi, PixelFormats.Bgra32, null);
            renderHost.RenderSceneIntoWriteableBitmap(layout.Viewport.Scene, viewportWb);
            viewportWb.Freeze();

            var exportRoot = new Canvas
            {
                Width = pageDipW,
                Height = pageDipH,
                Background = Brushes.White
            };

            exportRoot.Children.Add(titleblock);

            var viewportBorder = new Border
            {
                Width = InToDip(titleblock.ViewportWidth),
                Height = InToDip(titleblock.ViewportHeight),
                Child = new Image
                {
                    Source = viewportWb,
                    Stretch = Stretch.Fill,
                    SnapsToDevicePixels = true
                }
            };

            // Convert viewport placement inches -> DIPs
            Canvas.SetLeft(viewportBorder, InToDip(titleblock.ViewportLeft));
            Canvas.SetTop(viewportBorder, InToDip(titleblock.ViewportTop));
            exportRoot.Children.Add(viewportBorder);

            exportRoot.Measure(new Size(pageDipW, pageDipH));
            exportRoot.Arrange(new Rect(0, 0, pageDipW, pageDipH));
            exportRoot.UpdateLayout();

            var rtb = new RenderTargetBitmap(pagePxW, pagePxH, dpi, dpi, PixelFormats.Pbgra32);
            rtb.Render(exportRoot);
            rtb.Freeze();
            return rtb;
        }



        // Testing
        private static void SaveBitmapAsPng(BitmapSource bitmap, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            using var fs = File.Create(path);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(fs);
        }
    }
}
