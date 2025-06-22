using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Interop;
using Drawing = System.Drawing;
using Microsoft.Win32.SafeHandles;
using System.Reflection.Metadata;
using System.Numerics;

namespace Cad_Point_Manager.Helpers
{
    public class CustomCursorFactory
    {
        public static Cursor CreateCrosshairCursor(int size = 64, int thickness = 1, (byte a, byte r, byte g, byte b)? color = null)
        {
            color ??= (0, 0, 0, 255);
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                var pen = new Pen(new SolidColorBrush(Color.FromArgb(color.Value.a, color.Value.r, color.Value.g, color.Value.b)), thickness);
                dc.DrawLine(pen, new Point(0, size / 2), new Point(size, size / 2));
                dc.DrawLine(pen, new Point(size / 2, 0), new Point(size / 2, size));
            }

            var bmp = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(dv);

            return CursorFromBitmapSource(bmp, size / 2, size / 2);
        }

        public static Cursor CreateCrosshairWithSquareCenterCursor(int size = 64, double thickness = 1, double squareWidth = 8, double crossHairsOffset = 5, double innerCrosshairSize = 4, (byte a, byte r, byte g, byte b)? color = null)
        {
            color ??= (0, 0, 0, 255);
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                var pen = new Pen(new SolidColorBrush(Color.FromArgb(color.Value.a, color.Value.r, color.Value.g, color.Value.b)), thickness);

                // Outer lines
                double fullOffset = squareWidth / 2 + crossHairsOffset;
                dc.DrawLine(pen, new Point(size / 2 + fullOffset, size / 2), new Point(size, size / 2));
                dc.DrawLine(pen, new Point(size / 2 - fullOffset, size / 2), new Point(0, size / 2));
                dc.DrawLine(pen, new Point(size / 2, size / 2 + fullOffset), new Point(size / 2, size));
                dc.DrawLine(pen, new Point(size / 2, size / 2 - fullOffset), new Point(size / 2, 0));

                // Inner crosshairs
                dc.DrawLine(pen, new Point(size / 2 - innerCrosshairSize, size / 2), new Point(size / 2 + innerCrosshairSize, size / 2));
                dc.DrawLine(pen, new Point(size / 2, size / 2 - innerCrosshairSize), new Point(size / 2, size / 2 + innerCrosshairSize));

                Rect rect = new(
                    (size - squareWidth) / 2,
                    (size - squareWidth) / 2,
                    squareWidth,
                    squareWidth);
                dc.DrawRectangle(null, pen, rect);
            }

            var bmp = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(dv);

            return CursorFromBitmapSource(bmp, size / 2, size / 2);
        }

        private static Cursor CursorFromBitmapSource(BitmapSource source, int hotspotX, int hotspotY)
        {
            using var ms = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));
            encoder.Save(ms);

            using var bmp = new Drawing.Bitmap(ms);
            IntPtr hIcon = bmp.GetHicon();
            var iconInfo = new ICONINFO();
            GetIconInfo(hIcon, ref iconInfo);
            iconInfo.xHotspot = hotspotX;
            iconInfo.yHotspot = hotspotY;
            iconInfo.fIcon = false;

            IntPtr hCursor = CreateIconIndirect(ref iconInfo);
            return CursorInteropHelper.Create(new SafeIconHandle(hCursor));
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [DllImport("user32.dll")]
        private static extern bool GetIconInfo(IntPtr hIcon, ref ICONINFO pIconInfo);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateIconIndirect(ref ICONINFO icon);
    }

    public class SafeIconHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeIconHandle(IntPtr preexistingHandle, bool ownsHandle = true)
            : base(ownsHandle)
        {
            SetHandle(preexistingHandle);
        }

        protected override bool ReleaseHandle()
        {
            return DestroyIcon(handle);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);
    }
}
