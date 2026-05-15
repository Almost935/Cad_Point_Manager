using PdfiumViewer;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Cad_Point_Manager.Services.Exporting
{
    public class LayoutPreviewService
    {
        public BitmapSource RenderPreview(
            MemoryStream pdfStream,
            int width,
            int height,
            float dpi = 72)
        {
            using var document = PdfDocument.Load(pdfStream);

            using var image = (Bitmap)document.Render(
                page: 0,
                width: width,
                height: height,
                dpiX: dpi,
                dpiY: dpi,
                flags: PdfRenderFlags.ForPrinting);

            IntPtr hBitmap = image.GetHbitmap();

            try
            {
                var source = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                source.Freeze();

                return source;
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
    }
}
