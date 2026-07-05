using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.Printing;
using Cad_Point_Manager.Services.Exporting;
using System.Windows;
using System.Windows.Controls;

namespace Cad_Point_Manager.Views.UserControls
{
    /// <summary>
    /// Interaction logic for PdfLayoutPreviewControl.xaml
    /// </summary>
    public partial class PdfLayoutPreviewControl : UserControl
    {
        #region Fields
        private readonly LayoutPreviewService _previewService = new();
        #endregion

        #region Dependency Properties
        public static readonly DependencyProperty RendererProperty =
            DependencyProperty.Register(
                nameof(Renderer),
                typeof(D3dDxfControl),
                typeof(PdfLayoutPreviewControl),
                new PropertyMetadata(null));
        public D3dDxfControl? Renderer
        {
            get => (D3dDxfControl?)GetValue(RendererProperty);
            set => SetValue(RendererProperty, value);
        }

        public static readonly DependencyProperty CadManagerProperty =
            DependencyProperty.Register(
                nameof(CadManager),
                typeof(CadManager),
                typeof(PdfLayoutPreviewControl),
                new PropertyMetadata(null));
        public CadManager CadManager
        {
            get => (CadManager)GetValue(CadManagerProperty);
            set => SetValue(CadManagerProperty, value);
        }

        public static readonly DependencyProperty ActiveLayoutProperty =
            DependencyProperty.Register(
                nameof(ActiveLayout),
                typeof(Layout),
                typeof(PdfLayoutPreviewControl),
                new PropertyMetadata(null));
        public Layout? ActiveLayout
        {
            get => (Layout?)GetValue(ActiveLayoutProperty);
            set => SetValue(ActiveLayoutProperty, value);
        }

        public static readonly DependencyProperty ScenesProperty =
            DependencyProperty.Register(
                nameof(Scenes),
                typeof(IEnumerable<Scene>),
                typeof(PdfLayoutPreviewControl),
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
                typeof(PdfLayoutPreviewControl),
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
                typeof(PdfLayoutPreviewControl),
                new PropertyMetadata(0.00, OnViewportSizeChanged));
        public double ViewportHeight
        {
            get => (double)GetValue(ViewportHeightProperty);
            set => SetValue(ViewportHeightProperty, value);
        }
        #endregion

        #region Constructors
        public PdfLayoutPreviewControl()
        {
            InitializeComponent();
        }
        #endregion

        #region Methods
        public void Rebuild()
        {
            if (ActiveLayout == null ||
               CadManager == null ||
               Renderer == null ||
               Renderer.StateController == null ||
               Renderer.SceneIdMap == null ||
               Renderer.ResCache == null)
            {
                return;
            }

            int renderWidth =
                (int)Math.Round(ActiveLayout.Viewport.LocalRectIn.Width * GlobalHelperProperties.PdfPreviewDpi);
            int renderHeight =
                (int)Math.Round(ActiveLayout.Viewport.LocalRectIn.Height * GlobalHelperProperties.PdfPreviewDpi);

            var worldToPdf =
                   LayoutPdfVectorExporter.BuildWorldToPdfFromCamera(
                       ActiveLayout,
                       CadManager,
                       ActiveLayout.Viewport.Scene.Bounds.ToRect());

            using var pdf =
                LayoutPdfVectorExporter.ExportViewportPreviewToStream(
                    ActiveLayout,
                    CadManager,
                    Renderer.StateController,
                    Renderer.SceneIdMap,
                    Renderer.ResCache,
                    worldToPdf);
            pdf.Position = 0;

            var bmp =
                _previewService.RenderPreview(
                    pdf,
                    renderWidth,
                    renderHeight,
                    GlobalHelperProperties.PdfPreviewDpi);

            LayoutPreviewImage.Source = bmp;
        }

        public async Task RebuildAsync()
        {
            var activeLayout = ActiveLayout;
            var cadManager = CadManager;
            var renderer = Renderer;
            var stateController = renderer?.StateController;
            var sceneIdMap = renderer?.SceneIdMap;
            var resCache = renderer?.ResCache;

            if (activeLayout == null ||
                cadManager == null ||
                renderer == null ||
                stateController == null ||
                sceneIdMap == null ||
                resCache == null)
            {
                return;
            }

            int renderWidth =
                (int)Math.Round(activeLayout.PageWidth * GlobalHelperProperties.PdfPreviewDpi);
            int renderHeight =
                (int)Math.Round(activeLayout.PageHeight * GlobalHelperProperties.PdfPreviewDpi);

            var worldToPdf =
                   LayoutPdfVectorExporter.BuildWorldToPdfFromCamera(
                       activeLayout,
                       cadManager,
                       activeLayout.Viewport.Scene.Bounds.ToRect());

            await Task.Run(() =>
            {
                using var pdf =
                    LayoutPdfVectorExporter.ExportToStream(
                        activeLayout,
                        cadManager,
                        stateController,
                        sceneIdMap,
                        resCache,
                        worldToPdf);

                pdf.Position = 0;

                var bmp =
                    _previewService.RenderPreview(
                        pdf,
                        renderWidth,
                        renderHeight,
                        GlobalHelperProperties.PdfPreviewDpi);

                if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
                {
                    return;
                }

                Dispatcher.Invoke(() =>
                {
                    LayoutPreviewImage.Source = bmp;
                });
            });
        }

        private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PdfLayoutPreviewControl c) { _ = c.RebuildAsync(); }
        }
        private static void OnViewportSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PdfLayoutPreviewControl c)
            {

            }
        }
        #endregion
    }
}
