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

namespace Cad_Point_Manager.Services
{
    public static class LayoutPdfExporter
    {
        public static async Task ExportLayoutToPdfAsync(
            Layout layout,
            ILayoutRenderHost renderHost,
            string outputPdfPath,
            int dpi = 300)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (renderHost == null) throw new ArgumentNullException(nameof(renderHost));
            if (string.IsNullOrWhiteSpace(outputPdfPath)) throw new ArgumentNullException(nameof(outputPdfPath));

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                ExportOnUiThread(layout, renderHost, outputPdfPath, dpi);
            });
        }

        private static void ExportOnUiThread(Layout layout, ILayoutRenderHost renderHost, string outputPdfPath, int dpi)
        {
            RenderTargetBitmap pageBitmap = RenderLayoutToBitmap(layout, renderHost, dpi);

            using var pngStream = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(pageBitmap));
            encoder.Save(pngStream);
            pngStream.Position = 0;

            using var doc = new PdfDocument();
            var page = doc.AddPage();

            double pageWidthInches = layout.PageSize.Width;
            double pageHeightInches = layout.PageSize.Height;

            page.Width = pageWidthInches * 72.0;
            page.Height = pageHeightInches * 72.0;

            using (var gfx = XGraphics.FromPdfPage(page))
            using (var img = XImage.FromStream(() => pngStream))
            {
                gfx.DrawImage(img, 0, 0, page.Width, page.Height);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPdfPath)!);
            using var fs = File.Create(outputPdfPath);
            doc.Save(fs);
        }

        private static RenderTargetBitmap RenderLayoutToBitmap(Layout layout, ILayoutRenderHost renderHost, int dpi)
        {
            int pagePxW = (int)Math.Round((double)(layout.PageSize.Width * dpi));
            int pagePxH = (int)Math.Round((double)(layout.PageSize.Height * dpi));

            var titleblock = new BaseTitleblock
            {
                Width = layout.PageSize.Width,
                Height = layout.PageSize.Height
            };

            titleblock.Measure(new Size(layout.PageSize.Width, layout.PageSize.Height));
            titleblock.Arrange(new Rect(0, 0, layout.PageSize.Width, layout.PageSize.Height));
            titleblock.UpdateLayout();

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

            var viewportWb = new WriteableBitmap(
                vpPxW, vpPxH,
                dpi, dpi,
                PixelFormats.Bgra32,
                null);

            renderHost.RenderSceneIntoWriteableBitmap(layout.Viewport.Scene, viewportWb);
            viewportWb.Freeze();

            var exportRoot = new Canvas
            {
                Width = layout.PageSize.Width,
                Height = layout.PageSize.Height,
                Background = Brushes.White
            };

            exportRoot.Children.Add(titleblock);

            var viewportBorder = new Border
            {
                Width = titleblock.ViewportWidth,
                Height = titleblock.ViewportHeight,
                Child = new Image
                {
                    Source = viewportWb,
                    Stretch = Stretch.Fill,
                    SnapsToDevicePixels = true
                }
            };
            Canvas.SetLeft(viewportBorder, titleblock.ViewportLeft);
            Canvas.SetTop(viewportBorder, titleblock.ViewportTop);
            exportRoot.Children.Add(viewportBorder);

            exportRoot.Measure(new Size(exportRoot.Width, exportRoot.Height));
            exportRoot.Arrange(new Rect(0, 0, exportRoot.Width, exportRoot.Height));
            exportRoot.UpdateLayout();

            var rtb = new RenderTargetBitmap(pagePxW, pagePxH, dpi, dpi, PixelFormats.Pbgra32);
            rtb.Render(exportRoot);
            rtb.Freeze();
            return rtb;
        }
    }
}
