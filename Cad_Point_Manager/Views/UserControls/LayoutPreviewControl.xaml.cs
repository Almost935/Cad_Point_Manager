using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Models.Printing;
using SharpDX.Direct3D9;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Cad_Point_Manager.Views.UserControls
{
    /// <summary>
    /// Interaction logic for LayoutPreviewControl.xaml
    /// </summary>
    public partial class LayoutPreviewControl : UserControl, INotifyPropertyChanged
    {
        private const double DipsPerInch = 96.0;

        // Preview DPI (not print DPI). Bump if you want sharper previews.
        public int PreviewDpi { get; set; } = 150;

        // Cache: (sceneId, pixelW, pixelH) -> bitmap
        private readonly ConcurrentDictionary<(Guid sceneId, int w, int h), BitmapSource> _cache = new();

        public LayoutPreviewControl()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        public static readonly DependencyProperty RendererProperty =
            DependencyProperty.Register(
                nameof(Renderer),
                typeof(D3dDxfControl),
                typeof(LayoutPreviewControl),
                new PropertyMetadata(null));

        public D3dDxfControl? Renderer
        {
            get => (D3dDxfControl?)GetValue(RendererProperty);
            set => SetValue(RendererProperty, value);
        }

        public static readonly DependencyProperty LayoutProperty =
            DependencyProperty.Register(
                nameof(Layout),
                typeof(Layout),
                typeof(LayoutPreviewControl),
                new PropertyMetadata(null, OnLayoutChanged));

        public Layout? Layout
        {
            get => (Layout?)GetValue(LayoutProperty);
            set => SetValue(LayoutProperty, value);
        }

        // Provide the scenes collection (or dictionary) so we can resolve SceneId -> Scene
        public static readonly DependencyProperty ScenesProperty =
            DependencyProperty.Register(
                nameof(Scenes),
                typeof(IEnumerable<Scene>),
                typeof(LayoutPreviewControl),
                new PropertyMetadata(null, OnLayoutChanged));
        public IEnumerable<Scene>? Scenes
        {
            get => (IEnumerable<Scene>?)GetValue(ScenesProperty);
            set => SetValue(ScenesProperty, value);
        }
        #endregion
        private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LayoutPreviewControl c)
                _ = c.RebuildAsync();
        }

        public async Task RebuildAsync()
        {
            RootCanvas.Children.Clear();

            if (Layout == null || Scenes == null) { return; }

            if (Renderer == null)
            {
                DrawPageOnly();
                return;
            }

            // Build lookup
            var sceneById = Scenes.ToDictionary(s => s.SceneId);

            // 1) Draw page background in DIPs
            DrawPageOnly();

            // 2) Add each viewport frame (border + image)
            //foreach (var vp in Layout.Viewports)
            //{
            var vp = Layout.Viewport;
            Scene? scene = null;
            if (Scenes.Count() > vp.SceneIndex)
            {
                scene = Scenes.ToList()[vp.SceneIndex];
                //if (scene is null) { continue; }
            }
            if (scene is null)
            {
                DrawPageOnly();
                return;
            }

            var frame = CreateViewportFrame(vp);
            RootCanvas.Children.Add(frame);

            // Render preview bitmap sized to the frame at PreviewDpi
            int pxW = InchesToPixels(vp.LocalRectIn.Width, PreviewDpi);
            int pxH = InchesToPixels(vp.LocalRectIn.Height, PreviewDpi);

            if (pxW < 1 || pxH < 1) { return; }

            var bmp = await GetOrRenderAsync(scene, pxW, pxH);

            // Set image
            if (frame.Child is Image img) { img.Source = bmp; }
            //}
        }
        public static bool BitmapLooksBlank(BitmapSource bmp)
        {
            if (bmp.Format != PixelFormats.Bgra32)
                bmp = new FormatConvertedBitmap(bmp, PixelFormats.Bgra32, null, 0);

            int w = bmp.PixelWidth;
            int h = bmp.PixelHeight;
            int stride = w * 4;

            byte[] pixels = new byte[h * stride];
            bmp.CopyPixels(pixels, stride, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte b = pixels[i + 0];
                byte g = pixels[i + 1];
                byte r = pixels[i + 2];
                byte a = pixels[i + 3];

                // detect anything not near-white
                if (a > 10 && (r < 245 || g < 245 || b < 245))
                    return false;
            }

            return true;
        }

        public static void SaveBitmapToPng(BitmapSource bitmap, string filePath)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            encoder.Save(fs);
        }

        private void DrawPageOnly()
        {
            if (Layout == null) { return; }

            double pageW = Layout.PageSize.Width * DipsPerInch;
            double pageH = Layout.PageSize.Height * DipsPerInch;

            RootCanvas.Children.Clear();

            // Make the canvas exactly the page size (no padding; Viewbox will scale it)
            RootCanvas.Width = pageW;
            RootCanvas.Height = pageH;

            var pageBorder = new Border
            {
                Width = pageW,
                Height = pageH,
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(160, 160, 160)),
                BorderThickness = new Thickness(1)
            };

            Canvas.SetLeft(pageBorder, 0);
            Canvas.SetTop(pageBorder, 0);
            RootCanvas.Children.Add(pageBorder);
        }

        private Border CreateViewportFrame(LayoutViewport vp)
        {
            if (Layout == null) { throw new InvalidOperationException(); }

            // Map local->page
            Rect pageRectIn = new Rect(
                vp.LocalRectIn.X,
                vp.LocalRectIn.Y,
                vp.LocalRectIn.Width,
                vp.LocalRectIn.Height);

            double x = pageRectIn.X * DipsPerInch;
            double y = pageRectIn.Y * DipsPerInch;
            double w = pageRectIn.Width * DipsPerInch;
            double h = pageRectIn.Height * DipsPerInch;

            var img = new Image { Stretch = Stretch.Uniform };

            var border = new Border
            {
                Width = w,
                Height = h,
                Child = img,
                BorderThickness = new Thickness(vp.ShowBorder ? 4 : 0),
                BorderBrush = Brushes.Black,
                Background = Brushes.White,
                ClipToBounds = false   // important so drawings don’t leak outside
            };

            Canvas.SetLeft(border, x);
            Canvas.SetTop(border, y);

            return border;
        }

        private async Task<BitmapSource> GetOrRenderAsync(Scene scene, int pixelW, int pixelH)
        {
            var key = (scene.SceneId, pixelW, pixelH);
            //if (_cache.TryGetValue(key, out var cached)) { return cached; }

            if (Renderer == null) { throw new InvalidOperationException("Renderer is null."); }

            // Must render on the renderer's dispatcher (UI thread)
            var bmp = await Renderer.Dispatcher.InvokeAsync(() =>
                Renderer.RenderSceneToBitmapSource(scene, pixelW, pixelH));

            _cache[key] = bmp;
            return bmp;
        }

        private static int InchesToPixels(double inches, int dpi)
            => (int)Math.Round(inches * dpi);

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
