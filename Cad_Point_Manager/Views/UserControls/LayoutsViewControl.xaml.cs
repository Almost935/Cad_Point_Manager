using Cad_Point_Manager.Controls;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.PointRendering;
using Cad_Point_Manager.Models.Printing;
using Cad_Point_Manager.Services;
using Cad_Point_Manager.Views.Assorted;
using MaterialDesignThemes.Wpf;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

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

        private Matrix _initialMatrix;

        private bool _isCreatingNewLayout;
        private Layout _newLayout;
        private int _newLayoutFieldIndex;
        private ListViewItem? _lastLayoutsListItem;
        private string? _lastLayoutsListContextField;
        private FrameworkElement? _lastRightClickedCellElement;
        private static readonly string[] _newLayoutFieldOrder =
        {
            "Name",
            "PageWidth",
            "PageHeight",
            "Scene"
        };
        #endregion

        #region Properties
        private bool IsInDesignMode =>
            DesignerProperties.GetIsInDesignMode(new DependencyObject());
        #endregion

        #region Dependency Properties
        public static readonly DependencyProperty CadManagerProperty =
            DependencyProperty.Register(
                nameof(CadManager),
                typeof(CadManager),
                typeof(LayoutsViewControl),
                new PropertyMetadata(null, OnCadManagerChanged));
        public CadManager? CadManager
        {
            get => (CadManager?)GetValue(CadManagerProperty);
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

        public static readonly DependencyProperty SelectedLayoutsProperty =
            DependencyProperty.Register(
                nameof(SelectedLayouts),
                typeof(ObservableCollection<Layout>),
                typeof(LayoutsViewControl),
                new PropertyMetadata(new ObservableCollection<Layout>()));
        public ObservableCollection<Layout> SelectedLayouts
        {
            get => (ObservableCollection<Layout>)GetValue(SelectedLayoutsProperty);
            set => SetValue(SelectedLayoutsProperty, value);
        }

        public static readonly DependencyProperty ViewMatrixProperty =
        DependencyProperty.Register(
            nameof(ViewMatrix),
            typeof(Matrix),
            typeof(LayoutsViewControl),
            new PropertyMetadata(Matrix.Identity));
        public Matrix ViewMatrix
        {
            get => (Matrix)GetValue(ViewMatrixProperty);
            private set => SetValue(ViewMatrixProperty, value);
        }
        #endregion

        #region Constructors
        public LayoutsViewControl()
        {
            InitializeComponent();
        }
        #endregion

        #region Events
        public event EventHandler? ViewMatrixChanged;
        #endregion

        #region Methods
        private void LayoutsListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject src) { return; }
            if (e.OriginalSource is not FrameworkElement fe) { return; }

            ListViewItem? lvi = src as ListViewItem
                ?? VisualTreeHelpers.FindAncestor<ListViewItem>(src);

            if (lvi?.DataContext is not Layout layout)
            {
                _lastLayoutsListItem = null;
                _lastLayoutsListContextField = null;
                _lastRightClickedCellElement = null;

                return;
            }

            string field = LayoutsInferFieldNameFromDisplayElement(fe);

            if (string.IsNullOrEmpty(field))
            {
                e.Handled = true;

                _lastLayoutsListItem = null;
                _lastLayoutsListContextField = null;
                _lastRightClickedCellElement = null;

                return;
            }

            _lastLayoutsListContextField = field;
            _lastLayoutsListItem = lvi;
            _lastRightClickedCellElement = fe;
        }
        private void NewLayout_Click(object sender, RoutedEventArgs e)
        {
            Rect viewportBounds = new(0.5, 0.5, 28.938, 23);
            LayoutViewport viewport = new(viewportBounds, CadManager.Camera.OverviewScene);

            if (!CadManager.TryAddLayout(CadManager.GetNextAvailableLayoutName(), viewport, out var newLayout))
            {
                throw new Exception("Failed to add new layout.");
            }

            layoutsListView.SelectedItem = newLayout;
            layoutsListView.UpdateLayout();

            layoutsListView.ScrollIntoView(newLayout);

            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                GroupItem targetGroupItem = null;

                foreach (var groupItem in VisualTreeHelpers.FindVisualChildren<GroupItem>(layoutsListView))
                {
                    if (groupItem.DataContext is CollectionViewGroup cvg &&
                        ReferenceEquals(cvg.Name, newLayout))
                    {
                        targetGroupItem = groupItem;
                        break;
                    }
                }

                if (targetGroupItem != null)
                {
                    var expander = VisualTreeHelpers.FindVisualChildren<Expander>(targetGroupItem).FirstOrDefault();
                    expander?.IsExpanded = true;
                }

                layoutsListView.ScrollIntoView(newLayout);

                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    if (layoutsListView.ItemContainerGenerator.ContainerFromItem(newLayout) is ListViewItem container)
                    {
                        _isCreatingNewLayout = true;
                        _newLayout = newLayout;
                        _newLayoutFieldIndex = 0;

                        BeginLayoutsListCellEdit(container, "Name");
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        {
                            if (layoutsListView.ItemContainerGenerator.ContainerFromItem(newLayout) is ListViewItem li)
                            {
                                _isCreatingNewLayout = true;
                                _newLayout = newLayout;
                                _newLayoutFieldIndex = 0;

                                BeginLayoutsListCellEdit(li, "Name");
                            }
                        }));
                    }
                }));
            }));
        }
        private void DeleteLayouts_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show($"Are you sure you want to delete {SelectedLayouts.Count} layout?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                foreach (var layout in SelectedLayouts.Cast<Layout>().ToList())
                {
                    CadManager?.TryDeleteLayout(layout);
                }
            }
        }
        private void EditLayout_Click(object sender, RoutedEventArgs e)
        {
            if (_lastLayoutsListItem is null ||
                string.IsNullOrEmpty(_lastLayoutsListContextField)) { return; }

            if (!IsValidStoredLayoutCell()) { return; }

            BeginLayoutsListCellEdit(_lastLayoutsListItem, _lastLayoutsListContextField);

            pointsListViewContextMenu.IsOpen = false;
        }

        private void BeginLayoutsListCellEdit(ListViewItem lvi, string field)
        {
            if (lvi == null) { return; }

            InlineEdit.SetEditingField(lvi, field);
            string tboxName = GetLayoutsPropertyFromTBox(field);
            if (tboxName is null) { return; }

            lvi.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (VisualTreeHelpers.FindByName(lvi, tboxName) is TextBox editBox)
                {
                    editBox.Focus();
                    editBox.SelectAll();
                }
            }), DispatcherPriority.Input);
        }
        private string GetLayoutsPropertyFromTBox(string tboxName)
        {
            return tboxName switch
            {
                "Name" => "layoutNameEdit",
                "PageWidth" => "layoutWidthEdit",
                "PageHeight" => "layoutHeightEdit",
                _ => null
            };
        }
        private static string LayoutsInferFieldNameFromDisplayElement(FrameworkElement fe)
        {
            var grid = VisualTreeHelpers.FindAncestor<Grid>(fe);
            if (grid?.Name is string s && !string.IsNullOrWhiteSpace(s))
            {
                return s switch
                {
                    "layoutName" => "Name",
                    "layoutWidth" => "PageWidth",
                    "layoutHeight" => "PageHeight",
                    _ => null
                };
            }
            return null;
        }

        private bool IsValidStoredLayoutCell()
        {
            if (_lastRightClickedCellElement == null) { return false; }

            if (!VisualTreeHelpers.IsElementInVisualTree(_lastRightClickedCellElement)) { return false; }

            var currentLvi = VisualTreeHelpers.FindAncestor<ListViewItem>(_lastRightClickedCellElement);
            if (currentLvi == null || currentLvi != _lastLayoutsListItem) { return false; }

            string field = LayoutsInferFieldNameFromDisplayElement(_lastRightClickedCellElement);

            if (string.IsNullOrEmpty(field)) { return false; }

            // Ensure field still matches what we stored
            return field == _lastLayoutsListContextField;
        }

        private void SetViewMatrix(Matrix m)
        {
            ViewMatrix = m;
            ViewMatrixChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ReloadPreview()
        {
            LayoutPreviewControl.RebuildAsync();
        }

        private void LayoutsListInlineEditBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox tb) { return; }

            var lvi = VisualTreeHelpers.FindAncestor<ListViewItem>(tb);
            if (lvi == null) { return; }
            if (lvi.DataContext is not Layout layout) { return; }

            string field = InlineEdit.GetEditingField(lvi);
            var binding = tb.GetBindingExpression(TextBox.TextProperty);

            bool isNewLayoutRow = _isCreatingNewLayout && ReferenceEquals(layout, _newLayout);

            if (e.Key == Key.Enter)
            {
                string text = tb.Text;
                string? errorMessage = null;

                switch (field)
                {
                    case "Name":
                        {
                            if (!ValidationService.ValidateLayoutNameChange(text, layout, CadManager, out string svcError))
                            {
                                errorMessage = svcError;
                            }
                            break;
                        }
                    case "PageWidth":
                    case "PageHeight":
                        {
                            if (!double.TryParse(text, out _))
                            {
                                errorMessage = $"{field} must be a valid page size.";
                            }
                            break;
                        }
                    case "Scene":
                        {
                            if (!ValidationService.ValidateString(text, out string svcError))
                            {
                                errorMessage = svcError;
                            }
                            break;
                        }
                    default:
                        break;
                }

                if (errorMessage != null)
                {
                    if (binding != null)
                    {
                        Validation.MarkInvalid(
                            binding,
                            new ValidationError(
                                new DataErrorValidationRule(),
                                binding,
                                errorMessage,
                                null));
                    }

                    e.Handled = true;
                    return;
                }
                else
                {
                    // Valid: commit the value
                    if (binding != null)
                    {
                        Validation.ClearInvalid(binding);
                        binding.UpdateSource();
                    }

                    e.Handled = true;

                    if (isNewLayoutRow)
                    {
                        // ------- NEW POINT MODE: go to next field or finish -------
                        int idx = Array.IndexOf(_newLayoutFieldOrder, field);
                        if (idx >= 0 && idx < _newLayoutFieldOrder.Length - 1)
                        {
                            // Next field in the wizard
                            _newLayoutFieldIndex = idx + 1;
                            string nextField = _newLayoutFieldOrder[_newLayoutFieldIndex];

                            InlineEdit.SetEditingField(lvi, nextField);

                            lvi.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                string tboxName = GetLayoutsPropertyFromTBox(nextField);
                                if (tboxName != null &&
                                    VisualTreeHelpers.FindByName(lvi, tboxName) is TextBox nextTb)
                                {
                                    nextTb.Focus();
                                    nextTb.SelectAll();
                                }
                            }), DispatcherPriority.Input);
                        }
                        else
                        {
                            // Last field ("Description") just finished — end wizard mode
                            InlineEdit.SetEditingField(lvi, null);
                            _isCreatingNewLayout = false;
                            _newLayout = null;
                            _newLayoutFieldIndex = -1;
                        }

                        return;
                    }
                    else
                    {
                        // ------- NORMAL EDIT MODE (existing points) -------
                        InlineEdit.SetEditingField(lvi, null);

                        // Your old behavior: move focus to next control
                        (tb as UIElement)?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                        return;
                    }
                }
            }

            if (e.Key == Key.Escape)
            {
                if (isNewLayoutRow)
                {
                    // Cancel creation of this new point completely
                    if (CadManager is not null && _newLayout != null)
                    {
                        CadManager.TryDeleteLayout(_newLayout);
                    }

                    _isCreatingNewLayout = false;
                    _newLayout = null;
                    _newLayoutFieldIndex = -1;

                    InlineEdit.SetEditingField(lvi, null);
                    layoutsListView.SelectedItem = null;

                    e.Handled = true;
                    return;
                }
                else
                {
                    // Existing behavior: revert / exit edit for existing points
                    binding?.UpdateTarget();
                    InlineEdit.SetEditingField(lvi, null);
                    e.Handled = true;
                    return;
                }
            }
        }
        private void InlineEditBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox tb) { return; }

            var lvi = VisualTreeHelpers.FindAncestor<ListViewItem>(tb);
            if (lvi == null) { return; }

            var binding = tb.GetBindingExpression(TextBox.TextProperty);
            if (!Validation.GetHasError(tb))
            {
                binding?.UpdateSource();
            }

            string? currentField = LayoutsInferFieldNameFromDisplayElement(tb);
            string? editingField = InlineEdit.GetEditingField(lvi);

            if (!string.IsNullOrEmpty(editingField) &&
                currentField != null &&
                !string.Equals(editingField, currentField, StringComparison.Ordinal))
            {
                return;
            }

            InlineEdit.SetEditingField(lvi, null);
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
            var oldValue = e.OldValue as CadManager;
            if (oldValue is not null)
            {
                oldValue.Layouts.CollectionChanged -= ctrl.Layouts_CollectionChanged;
            }
            var newValue = e.NewValue as CadManager;
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
            if (e.ChangedButton == MouseButton.Middle)
            {
                if (layoutsListView.IsMouseOver) { return; }

                _panning = true;
                _panStartMouse = e.GetPosition(BackgroundCanvas);
                _panStartMatrix = ViewMatrix;
                Mouse.Capture(BackgroundCanvas);
                e.Handled = true;
            }

            CommitInlineEditIfActive(e.OriginalSource as DependencyObject);
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

            Point cur = e.GetPosition(BackgroundCanvas);

            Vector delta = cur - _panStartMouse;

            Matrix m = _panStartMatrix;
            m.Translate(delta.X, delta.Y);

            SetViewMatrix(m);
        }
        private void Root_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (layoutsListView.IsMouseOver) { return; }

            // Mouse position in the content we are transforming (Surface coords)
            Point p = e.GetPosition(BackgroundCanvas);

            Matrix m = ViewMatrix;

            // Current uniform scale (assuming you only do uniform scaling)
            double currentScale = m.M11;
            double factor = e.Delta > 0 ? ZoomStep : 1.0 / ZoomStep;

            double newScale = Clamp(currentScale * factor, MinScale, MaxScale);
            factor = newScale / currentScale; // adjust factor if clamped

            // Zoom around the mouse point: translate(-p), scale, translate(+p)
            m.Translate(-p.X, -p.Y);
            m.Scale(factor, factor);
            m.Translate(p.X, p.Y);

            SetViewMatrix(m);
            e.Handled = true;
        }

        private void BackgroundCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (IsInDesignMode) { return; }

            FitPageToView();
        }

        private void CommitInlineEditIfActive(DependencyObject clickedElement)
        {
            // Find active editing ListViewItem
            var editingItem = VisualTreeHelpers
                .FindVisualChildren<ListViewItem>(layoutsListView)
                .FirstOrDefault(lvi => InlineEdit.GetEditingField(lvi) != null);

            if (editingItem == null)
                return;

            // If click is INSIDE the same item → ignore
            if (clickedElement != null)
            {
                var clickedLvi = VisualTreeHelpers.FindAncestor<ListViewItem>(clickedElement);
                if (clickedLvi == editingItem)
                    return;
            }

            // 🔥 Force commit (simulate LostFocus)
            InlineEdit.SetEditingField(editingItem, null);
        }

        private static double Clamp(double v, double min, double max)
        => v < min ? min : (v > max ? max : v);

        private void FitPageToView()
        {
            if (IsInDesignMode) { return; }

            double viewW = BackgroundCanvas.ActualWidth - 20;
            double viewH = BackgroundCanvas.ActualHeight - 20;

            if (viewW <= 0 || viewH <= 0) { return; }

            double pageW = ActiveLayout.PageWidth;
            double pageH = ActiveLayout.PageHeight;

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
            SetViewMatrix(m);
        }

        public void ResetView()
        {
            SetViewMatrix(_initialMatrix);
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
