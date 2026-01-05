using Cad_Point_Manager.Controls;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.Printing;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Cad_Point_Manager.Views.UserControls
{
    /// <summary>
    /// Interaction logic for LayoutsViewControl.xaml
    /// </summary>
    public partial class LayoutsViewControl : UserControl, INotifyPropertyChanged
    {
        #region Fields
        private const double ZoomStep = 1.25;
        private const double MinScale = 0.005;
        private const double MaxScale = 500.0;

        private Point _panStartMouse;     // mouse position when pan started (in container coords)
        private Matrix _panStartMatrix;   // matrix at pan start
        private bool _panning;
        private bool _didInitialFit;

        private Matrix _initialMatrix;
        #endregion

        #region Properties

        #endregion

        #region Dependency Properties
        public static readonly DependencyProperty CadManagerProperty =
            DependencyProperty.Register(
                nameof(CadManager),
                typeof(CadManager3D),
                typeof(LayoutsViewControl),
                new PropertyMetadata(null, OnCadManagerChanged));
        public CadManager3D? CadManager
        {
            get => (CadManager3D?)GetValue(CadManagerProperty);
            set => SetValue(CadManagerProperty, value);
        }

        public static readonly DependencyProperty ActiveLayoutProperty =
           DependencyProperty.Register(
               nameof(ActiveLayout),
               typeof(Layout),
               typeof(LayoutsViewControl),
               new PropertyMetadata(null, OnActiveLayoutChanged));
        public Layout? ActiveLayout
        {
            get => (Layout?)GetValue(ActiveLayoutProperty);
            set => SetValue(ActiveLayoutProperty, value);
        }

        public static readonly DependencyProperty RendererProperty =
            DependencyProperty.Register(
                nameof(Renderer),
                typeof(D3dDxfControl),
                typeof(LayoutsViewControl),
                new PropertyMetadata(null, OnRendererChanged));
        public D3dDxfControl? Renderer
        {
            get => (D3dDxfControl?)GetValue(RendererProperty);
            set => SetValue(RendererProperty, value);
        }

        public static readonly DependencyProperty ScenesProperty =
            DependencyProperty.Register(
                nameof(Scenes),
                typeof(IEnumerable<Scene>),
                typeof(LayoutsViewControl),
                new PropertyMetadata(null));
        public IEnumerable<Scene>? Scenes
        {
            get => (IEnumerable<Scene>?)GetValue(ScenesProperty);
            set => SetValue(ScenesProperty, value);
        }
        #endregion

        #region Constructors
        public LayoutsViewControl()
        {
            InitializeComponent();
        }
        #endregion

        #region Methods
        public void ReloadPreview()
        {
            LayoutPreviewControl.RebuildAsync();
        }

        private void LayoutsListView_Loaded(object sender, RoutedEventArgs e)
        {
            DoubleClickSetListView listview = sender as DoubleClickSetListView;

            GridView layoutssGridView = listview.View as GridView;
            double layoutsListTotalWidth = layoutsListView.ActualWidth;
            double layoutsListColumnWidth = layoutsListTotalWidth / layoutssGridView.Columns.Count;
            if (layoutsListColumnWidth > 0)
            {
                layoutssGridView.Columns[0].Width = layoutsListColumnWidth * 1.15;
                layoutssGridView.Columns[1].Width = layoutsListColumnWidth * 0.75;
                layoutssGridView.Columns[2].Width = layoutsListColumnWidth * 0.75;
                layoutssGridView.Columns[3].Width = layoutsListColumnWidth * 1.35;
            }
        }

        private void LayoutsListInlineEditBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {

        }

        private void InlineEditBox_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void LayoutsCellDisplay_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void Layouts_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (CadManager is not null && CadManager.Layouts.Count > 0)
            {
                if (ActiveLayout is null || !CadManager.Layouts.Contains(ActiveLayout))
                {
                    ActiveLayout = CadManager.Layouts.First();
                }
            }
        }

        private static void OnCadManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (LayoutsViewControl)d;
            var oldValue = e.OldValue as CadManager3D;
            if (oldValue is not null)
            {
                oldValue.Layouts.CollectionChanged -= ctrl.Layouts_CollectionChanged;
            }
            var newValue = e.NewValue as CadManager3D;
            if (newValue is not null)
            {
                if (oldValue is not null)
                {
                    oldValue.Layouts.CollectionChanged -= ctrl.Layouts_CollectionChanged;
                }
                newValue.Layouts.CollectionChanged += ctrl.Layouts_CollectionChanged;
            }
        }
        private static void OnActiveLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (LayoutsViewControl)d;
            if (ctrl is not null) { ctrl.LayoutPreviewControl.RebuildAsync(); }

            if (e.OldValue is Layout oldLayout) { oldLayout.Viewport.PropertyChanged -= ctrl.ActiveLayoutViewport_PropertyChanged; }

            if (e.NewValue is Layout newLayout) { newLayout.Viewport.PropertyChanged += ctrl.ActiveLayoutViewport_PropertyChanged; }
        }
        private async void ActiveLayoutViewport_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LayoutViewport.Scene) ||
                e.PropertyName == nameof(LayoutViewport.LocalRectIn) ||
                e.PropertyName == nameof(LayoutViewport.ShowBorder))
            {
                // If PropertyChanged can come from a background thread, marshal to UI thread
                if (!Dispatcher.CheckAccess())
                {
                    await Dispatcher.InvokeAsync(() => LayoutPreviewControl.RebuildAsync());
                    return;
                }

                LayoutPreviewControl.RebuildAsync();
            }
        }

        private static void OnRendererChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (LayoutsViewControl)d;
            if (ctrl is not null)
            {
                ctrl.LayoutPreviewControl.RebuildAsync();
            }
        }
        #endregion

        #region Pan and Zoom Methods
        private void Root_MouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Middle button drag to pan (change to Left if you prefer)
            if (e.ChangedButton == MouseButton.Middle)
            {
                if (tabControl.IsMouseOver) { return; }

                _panning = true;
                _panStartMouse = e.GetPosition((IInputElement)sender);
                _panStartMatrix = transform.Matrix;
                Mouse.Capture((IInputElement)sender);
                e.Handled = true;
            }
        }
        private void Root_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle && _panning)
            {
                _panning = false;
                Mouse.Capture(null);
                e.Handled = true;
            }
        }
        private void Root_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_panning) { return; }

            Point cur = e.GetPosition((IInputElement)sender);

            Vector delta = cur - _panStartMouse;

            Matrix m = _panStartMatrix;
            m.Translate(delta.X, delta.Y);

            transform.Matrix = m;
        }
        private void Root_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (tabControl.IsMouseOver) { return; }

            // Mouse position in the content we are transforming (Surface coords)
            Point p = e.GetPosition(BackgroundCanvas);

            Matrix m = transform.Matrix;

            // Current uniform scale (assuming you only do uniform scaling)
            double currentScale = m.M11;
            double factor = e.Delta > 0 ? ZoomStep : 1.0 / ZoomStep;

            double newScale = Clamp(currentScale * factor, MinScale, MaxScale);
            factor = newScale / currentScale; // adjust factor if clamped

            // Zoom around the mouse point: translate(-p), scale, translate(+p)
            m.Translate(-p.X, -p.Y);
            m.Scale(factor, factor);
            m.Translate(p.X, p.Y);

            transform.Matrix = m;
            e.Handled = true;
        }
       
        private void BackgroundCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FitPageToView();
        }

        private static double Clamp(double v, double min, double max)
        => v < min ? min : (v > max ? max : v);

        private void FitPageToView()
        {
            if (ActiveLayout?.PageSize == null) { return; }

            double viewW = BackgroundCanvas.ActualWidth - 20;
            double viewH = BackgroundCanvas.ActualHeight - 20;

            if (viewW <= 0 || viewH <= 0) { return; }

            double pageW = ActiveLayout.PageSize.Width;
            double pageH = ActiveLayout.PageSize.Height;

            if (pageW <= 0 || pageH <= 0) { return; }

            // Optional padding so it’s not flush against edges
            const double pad = 20;

            double scaleX = (viewW - pad * 2) / pageW;
            double scaleY = (viewH - pad * 2) / pageH;
            double s = Clamp(Math.Min(scaleX, scaleY), MinScale, MaxScale);

            // Center the page in the view
            double tx = (viewW - pageW * s) * 0.5;
            double ty = (viewH - pageH * s) * 0.5;

            // Build matrix: scale then translate
            var m = Matrix.Identity;
            m.Scale(s, s);
            m.Translate(tx, ty);

            _initialMatrix = m;
            transform.Matrix = m;

            _didInitialFit = true;
        }

        public void ResetView()
        {
            transform.Matrix = _initialMatrix;
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
