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

        public static readonly DependencyProperty ViewportWidthProperty =
            DependencyProperty.Register(
                nameof(ViewportWidth),
                typeof(double),
                typeof(LayoutPreviewControl),
                new PropertyMetadata(0.00, OnViewportSizeChanged));
        public double ViewportWidth
        {
            get => (double)GetValue(ViewportWidthProperty);
            set => SetValue(ViewportWidthProperty, value);
        }

        public static readonly DependencyProperty ViewportHeightProperty =
            DependencyProperty.Register(
                nameof(ViewportHeight),
                typeof(double),
                typeof(LayoutPreviewControl),
                new PropertyMetadata(0.00, OnViewportSizeChanged));
        public double ViewportHeight
        {
            get => (double)GetValue(ViewportHeightProperty);
            set => SetValue(ViewportHeightProperty, value);
        }
        #endregion

        #region Methods
        private static void OnViewportSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LayoutPreviewControl c) 
            {
                
            }
        }
        private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LayoutPreviewControl c) { _ = c.RebuildAsync(); }
        }

        public async Task RebuildAsync()
        {
            if (Layout == null) { return; }
            if (Renderer == null) { return; }

            Rect bounds = Layout.Viewport.Bounds;

            int pxW = InchesToPixels(ViewportWidth, PreviewDpi);
            int pxH = InchesToPixels(ViewportHeight, PreviewDpi);

            if (pxW < 1 || pxH < 1) { return; }

            var wb = GetOrCreateWriteable(pxW, pxH);
            await Renderer.Dispatcher.InvokeAsync(() =>
            {
                Renderer.RenderSceneIntoWriteableBitmap(bounds, wb);
            });

            LayoutPreviewImage.Source = wb;
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
