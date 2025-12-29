using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Models.Printing;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Cad_Point_Manager.Views.UserControls
{
    /// <summary>
    /// Interaction logic for LayoutPreviewControl.xaml
    /// </summary>
    public partial class LayoutPreviewControl : UserControl, INotifyPropertyChanged
    {
        #region Fields
        private const double DipsPerInch = 96.0;

        private WriteableBitmap? _wb;
        private int _wbW, _wbH;
        // Cache: (sceneId, pixelW, pixelH) -> bitmap
        private readonly ConcurrentDictionary<(Guid sceneId, int w, int h), BitmapSource> _cache = new();
        #endregion

        #region Properities
        // Preview DPI (not print DPI). Bump if you want sharper previews.
        public int PreviewDpi { get; set; } = 150;
        #endregion

        #region Constructors
        public LayoutPreviewControl()
        {
            InitializeComponent();
        }
        #endregion

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

        #region Methods
        private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LayoutPreviewControl c)
                _ = c.RebuildAsync();
        }

        public async Task RebuildAsync()
        {
            RootCanvas.Children.Clear();

            if (Layout == null) { return; }

            if (Renderer == null)
            {
                DrawPageOnly();
                return;
            }

            DrawPageOnly();

            var vp = Layout.Viewport;
            Scene scene = vp.Scene;

            var frame = CreateViewportFrame(vp);
            RootCanvas.Children.Add(frame);

            // Render preview bitmap sized to the frame at PreviewDpi
            int pxW = InchesToPixels(vp.LocalRectIn.Width, PreviewDpi);
            int pxH = InchesToPixels(vp.LocalRectIn.Height, PreviewDpi);

            if (pxW < 1 || pxH < 1) { return; }

            var wb = GetOrCreateWriteable(pxW, pxH);
            await Renderer.Dispatcher.InvokeAsync(() =>
            {
                Renderer.RenderSceneIntoWriteableBitmap(scene, wb);
            });
            if (frame.Child is Image img)
            {
                img.Source = wb;
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                img.UseLayoutRounding = true;
                img.SnapsToDevicePixels = true;

            }
        }
        private Image CreateViewportImage()
        {
            var vp = Layout.Viewport;
            int pxW = InchesToPixels(vp.LocalRectIn.Width, PreviewDpi);
            int pxH = InchesToPixels(vp.LocalRectIn.Height, PreviewDpi);

            if (pxW < 1 || pxH < 1) { return; }

            var wb = GetOrCreateWriteable(pxW, pxH);
            await Renderer.Dispatcher.InvokeAsync(() =>
            {
                Renderer.RenderSceneIntoWriteableBitmap(scene, wb);
            });

            var img = new Image { Stretch = Stretch.Uniform };
            img.Source = wb;
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
            img.UseLayoutRounding = true;
            img.SnapsToDevicePixels = true;
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
            };
            //RootCanvas.Children.Add(pageBorder);
        }

        private Border CreateViewportFrame(LayoutViewport vp)
        {
            if (Layout == null) { throw new InvalidOperationException(); }

            // Map local->page
            Rect pageRectIn = new(
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
                //BorderThickness = new Thickness(vp.ShowBorder ? 4 : 0),
                //BorderBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                ClipToBounds = true
            };
            //Canvas.SetLeft(border, x);
            //Canvas.SetTop(border, y);
            return border;
        }

        private static int InchesToPixels(double inches, int dpi) => (int)Math.Round(inches * dpi);

        private WriteableBitmap GetOrCreateWriteable(int w, int h)
        {
            if (_wb == null || _wbW != w || _wbH != h)
            {
                _wbW = w; _wbH = h;
                _wb = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);
            }
            return _wb;
        }
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
