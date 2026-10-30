using Cad_Point_Manager.Common;
using Cad_Point_Manager.Common.Collections;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.PointRendering;
using Cad_Point_Manager.Models.Printing;
using Cad_Point_Manager.Services;
using Cad_Point_Manager.ViewModels;
using Cad_Point_Manager.Views.Assorted;
using Cad_Point_Manager.Views.ValidationRules;
using SharpDX;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using TextBox = System.Windows.Controls.TextBox;

namespace Cad_Point_Manager.Views.UserControls
{
    /// <summary>
    /// Interaction logic for LeftHandPopout.xaml
    /// </summary>
    public partial class LeftHandPopout : UserControl, INotifyPropertyChanged
    {
        #region Fields
        private const double _panelHideTime = 200;
        private const double _zoomToPointPaddingFactor = 2;

        private readonly DispatcherTimer _hideTimer = new();
        private bool _isMouseOverPanel = false;
        private bool _mainPanelIsVisible = false;
        private ScaleTransform _mainPanelTransform = new();

        // Points related fields
        private List<CogoPoint> _pointsEditTargets = [];
        private TextBox? _activePointsEditBox;
        private readonly List<CogoPoint> _selectedPoints = [];
        private bool _pointsTabVisible = true;
        private double _pointsTabOpacity = 0;
        private bool _propertiesTabVisible = true;
        private double _propertiesTabOpacity = 0;
        private bool _viewsTabVisible = true;
        private double _viewsTabOpacity = 0;
        private CogoPointSelectionViewModel _cogoPointSelectionViewModel;
        private int _lastCreatedPointNumber = 1;

        private string? _lastPointsListContextField;
        private ListViewItem? _lastPointsListItem;

        private bool _isCreatingNewPoint = false;
        private CogoPoint? _newPoint = null;
        private int _newPointFieldIndex = -1;

        private static readonly string[] _newPointFieldOrder =
        {
            "PointNumber",
            "Northing",
            "Easting",
            "Elevation",
            "Description"
        };

        // Scenes related fields
        private List<Scene> _selectedScenes = [];
        private Scene _newScene = null;
        private ListViewItem? _lastScenesListItem;
        private Scene? _lastRightClickedScene;
        #endregion

        #region Properties
        public bool PointsTabVisible
        {
            get { return _pointsTabVisible; }
            set
            {
                _pointsTabVisible = value;
                OnPropertyChanged(nameof(PointsTabVisible));
            }
        }
        public double PointsTabOpacity
        {
            get { return _pointsTabOpacity; }
            set
            {
                _pointsTabOpacity = value;
                OnPropertyChanged(nameof(PointsTabOpacity));
            }
        }
        public bool PropertiesTabVisible
        {
            get { return _propertiesTabVisible; }
            set
            {
                _propertiesTabVisible = value;
                OnPropertyChanged(nameof(PropertiesTabVisible));
            }
        }
        public double PropertiesTabOpacity
        {
            get { return _propertiesTabOpacity; }
            set
            {
                _propertiesTabOpacity = value;
                OnPropertyChanged(nameof(PropertiesTabOpacity));
            }
        }
        public bool ViewsTabVisible
        {
            get { return _viewsTabVisible; }
            set
            {
                _viewsTabVisible = value;
                OnPropertyChanged(nameof(ViewsTabVisible));
            }
        }
        public double ViewsTabOpacity
        {
            get { return _viewsTabOpacity; }
            set
            {
                _viewsTabOpacity = value;
                OnPropertyChanged(nameof(ViewsTabOpacity));
            }
        }
        public CogoPointSelectionViewModel CogoPointSelectionViewModel
        {
            get { return _cogoPointSelectionViewModel; }
            set
            {
                _cogoPointSelectionViewModel = value;
                OnPropertyChanged(nameof(CogoPointSelectionViewModel));
            }
        }
        #endregion

        #region Dependency Properties
        public double TabWidth
        {
            get { return (double)GetValue(TabWidthProperty); }
            set { SetValue(TabWidthProperty, value); }
        }

        public static readonly DependencyProperty TabWidthProperty =
        DependencyProperty.Register(
            nameof(TabWidth),
            typeof(double),
            typeof(LeftHandPopout),
            new PropertyMetadata(20.0));

        public CadManager CadManager
        {
            get { return (CadManager)GetValue(CadManagerProperty); }
            set { SetValue(CadManagerProperty, value); }
        }
        public static readonly DependencyProperty CadManagerProperty =
        DependencyProperty.Register(
            nameof(CadManager),
            typeof(CadManager),
            typeof(LeftHandPopout),
            new PropertyMetadata(null, OnCadManagerChanged));

        public Camera Camera
        {
            get { return (Camera)GetValue(CameraProperty); }
            set { SetValue(CameraProperty, value); }
        }
        public static readonly DependencyProperty CameraProperty =
        DependencyProperty.Register(
            nameof(Camera),
            typeof(Camera),
            typeof(LeftHandPopout),
            new PropertyMetadata(null, OnCadManagerChanged));

        public static readonly DependencyProperty SelectedCogoPointsProperty =
            DependencyProperty.Register(
            nameof(SelectedCogoPoints),
            typeof(BatchableObservableCollection<CogoPoint>),
            typeof(LeftHandPopout),
            new FrameworkPropertyMetadata(
                new BatchableObservableCollection<CogoPoint>(),
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedCogoPointsChanged));
        public BatchableObservableCollection<CogoPoint> SelectedCogoPoints
        {
            get => (BatchableObservableCollection<CogoPoint>)GetValue(SelectedCogoPointsProperty);
            set
            {
                SetValue(SelectedCogoPointsProperty, value);
                CogoPointSelectionViewModel?.Refresh();
            }
        }

        public PointGroup ActivePointGroup
        {
            get { return (PointGroup)GetValue(ActivePointGroupProperty); }
            set { SetValue(ActivePointGroupProperty, value); }
        }
        public static readonly DependencyProperty ActivePointGroupProperty =
        DependencyProperty.Register(
            nameof(ActivePointGroup),
            typeof(PointGroup),
            typeof(LeftHandPopout),
            new PropertyMetadata(null));
        #endregion

        #region Constructors
        public LeftHandPopout()
        {
            InitializeComponent();

            mainPanel.RenderTransform = _mainPanelTransform;

            HideControl();

            _hideTimer.Interval = TimeSpan.FromSeconds(1);
            _hideTimer.Tick += HideTimer_Tick;
        }
        #endregion
        // ------------------------------------------------------------------------
        // ------------------------------------------------------------------------
        // ------------------------ GENERAL METHODS -------------------------------
        // ------------------------------------------------------------------------
        // ------------------------------------------------------------------------
        #region General Methods
        private static void OnCadManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LeftHandPopout control && e.NewValue is not null && e.NewValue is CadManager cadManager)
            {

                control.CogoPointSelectionViewModel?.UpdateCadManager(cadManager);
                control.CogoPointSelectionViewModel?.Refresh();
            }
        }

        private void SelectedCogoPoints_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            CogoPointSelectionViewModel?.Refresh();
        }
        private static void OnSelectedCogoPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not LeftHandPopout control)
                return;

            if (e.OldValue is ObservableCollection<CogoPoint> oldCollection)
            {
                oldCollection.CollectionChanged -= control.SelectedCogoPoints_CollectionChanged;
            }

            if (e.NewValue is ObservableCollection<CogoPoint> newCollection)
            {
                newCollection.CollectionChanged += control.SelectedCogoPoints_CollectionChanged;

                if (control.CogoPointSelectionViewModel == null)
                {
                    control.CogoPointSelectionViewModel =
                        new CogoPointSelectionViewModel(control.CadManager, newCollection);
                }
                else
                {
                    control.CogoPointSelectionViewModel.SelectedPoints = newCollection;
                }
            }
        }

        private void HideTimer_Tick(object sender, EventArgs e)
        {
            _hideTimer.Stop();
            if (!_isMouseOverPanel)
            {
                HideControl();
            }
        }

        private void OverallGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            if (pointsListViewContextMenu.IsOpen || scenesListViewContextMenu.IsOpen) { return; }
            _isMouseOverPanel = false;
            _hideTimer.Start();
        }
        private void OverallGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            _isMouseOverPanel = true;
            _hideTimer.Stop();
            ShowControl();
        }

        private void ShowControl()
        {
            _mainPanelIsVisible = true;
            DoubleAnimation slideIn = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(_panelHideTime),
                FillBehavior = FillBehavior.HoldEnd
            };
            _mainPanelTransform.BeginAnimation(ScaleTransform.ScaleXProperty, slideIn);
        }
        private void HideControl()
        {
            _mainPanelIsVisible = false;
            DoubleAnimation slideOut = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(_panelHideTime),
                FillBehavior = FillBehavior.HoldEnd
            };
            _mainPanelTransform.BeginAnimation(ScaleTransform.ScaleXProperty, slideOut);
        }

        private void InlineEditBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox tb)
                return;

            var lvi = VisualTreeHelpers.FindAncestor<ListViewItem>(tb);
            if (lvi == null)
                return;

            var binding = tb.GetBindingExpression(TextBox.TextProperty);

            // POINTS LISTVIEW
            if (lvi.DataContext is CogoPoint cp)
            {
                if (!ReferenceEquals(tb, _activePointsEditBox))
                    return;

                if (_isCreatingNewPoint)
                {
                    _activePointsEditBox = null;
                    _pointsEditTargets.Clear();

                    InlineEdit.SetEditingField(lvi, null);

                    _isCreatingNewPoint = false;
                    _newPoint = null;
                    _newPointFieldIndex = -1;
                }

                string? field = InlineEdit.GetEditingField(lvi);

                if (string.IsNullOrEmpty(field))
                    return;

                if (!CommitPointsListCellEdit(tb, cp, field))
                {
                    binding?.UpdateTarget();

                    if (binding != null)
                        Validation.ClearInvalid(binding);
                }

                _activePointsEditBox = null;
                _pointsEditTargets.Clear();

                InlineEdit.SetEditingField(lvi, null);
                return;
            }

            // SCENES LISTVIEW
            if (lvi.DataContext is Scene)
            {
                if (!Validation.GetHasError(tb))
                    binding?.UpdateSource();

                InlineEdit.SetEditingField(lvi, null);
            }
        }
        #endregion

        // ------------------------------------------------------------------------
        // ------------------------------------------------------------------------
        // -------------- POINTS LISTVIEW BEHAVIOR AND CONTEXT MENU ---------------
        // ------------------------------------------------------------------------
        // ------------------------------------------------------------------------
        #region Points ListView Methods
        private void PointsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedPoints.Clear();
            var selectedItems = (sender as ListView).SelectedItems;

            foreach (var selectedItem in selectedItems)
            {
                if (selectedItem is CogoPoint selectedPoint && selectedPoint is not null)
                {
                    _selectedPoints.Add(selectedPoint);
                }
            }
        }
        private async void PointsBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            if ((PropertiesTabVisible || ViewsTabVisible) && _mainPanelIsVisible)
            {
                await Task.Delay(GlobalHelperProperties.PopOutCloseDelay);
            }
            if (pointsBorder.IsMouseOver)
            {
                PointsTabVisible = true;
                PointsTabOpacity = 1;
                PropertiesTabVisible = false;
                PropertiesTabOpacity = 0;
                ViewsTabVisible = false;
                ViewsTabOpacity = 0;
            }
        }
        private void PointsListViewNewPoint_Click(object sender, RoutedEventArgs e)
        {
            if (CadManager is null) { return; }

            if (ActivePointGroup is null)
            {
                if (DataContext is MainViewModel vm)
                    vm.ShowActivePointGroupError();

                return;
            }

            bool created = TryCreateNewPoint(
                CadManager.GetNextAvailablePointNumber(_lastCreatedPointNumber),
                Vector2d.Zero, ActivePointGroup, 0, "");
        }
        private void PointsListViewEditPoint_Click(object sender, RoutedEventArgs e)
        {
            if (_lastPointsListItem == null || string.IsNullOrEmpty(_lastPointsListContextField))
            { return; }

            BeginPointsListCellEdit(_lastPointsListItem, _lastPointsListContextField);

            pointsListViewContextMenu.IsOpen = false;
        }
        private void PointsListViewDeletePoint_Click(object sender, RoutedEventArgs e)
        {
            if (CadManager is null) { return; }

            var pointsToDelete = new List<CogoPoint>(_selectedPoints);
            foreach (var point in pointsToDelete)
            {
                CadManager.TryDeletePoint(point);
            }
            CadManager.CogoPointCircleVerticesDirty = true;
            CadManager.CogoPointTextVerticesDirty = true;
        }
        private void PointsListViewZoomPoint_Click(object sender, RoutedEventArgs e)
        {
            if (Camera is null ||
                _lastPointsListItem is null ||
                _lastPointsListItem.DataContext is not CogoPoint p)
                return;

            CadManager.ZoomToPoint(p, _zoomToPointPaddingFactor);
        }
        private void PointsListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // element that was actually right-clicked
            if (e.OriginalSource is not DependencyObject src) { return; }
            if (e.OriginalSource is not FrameworkElement fe) { return; }

            // Walk up from the OriginalSource until we find something we can map to a field
            ListViewItem? lvi = src as ListViewItem
                                   ?? VisualTreeHelpers.FindAncestor<ListViewItem>(src);
            if (lvi == null) { return; }

            string field = GetFieldFromMousePosition(lvi);
            if (string.IsNullOrEmpty(field)) { return; }

            _lastPointsListContextField = field;
            _lastPointsListItem = lvi;
        }
        private void PointsListView_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (!mainPanel.IsMouseOver)
            {
                _isMouseOverPanel = false;
                _hideTimer.Start();
            }
        }
        private void PointsCellDisplay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount < 2) { return; }
            if (sender is not FrameworkElement fe || VisualTreeHelpers.FindAncestor<ListViewItem>(fe) is not ListViewItem lvi) { return; }

            string field = PointsInferFieldNameFromDisplayElement(fe);
            if (string.IsNullOrEmpty(field)) { return; }

            BeginPointsListCellEdit(lvi, field);
        }
        private static string? PointsInferFieldNameFromDisplayElement(DependencyObject obj)
        {
            while (obj != null)
            {
                if (obj is Grid g && !string.IsNullOrWhiteSpace(g.Name))
                {
                    return g.Name switch
                    {
                        "pointNumber" => "PointNumber",
                        "northing" => "Northing",
                        "easting" => "Easting",
                        "elevation" => "Elevation",
                        "description" => "Description",
                        _ => null
                    };
                }

                obj = VisualTreeHelper.GetParent(obj);
            }

            return null;
        }
        private string? GetFieldFromMousePosition(ListViewItem item)
        {
            if (pointsListView.View is not GridView gv) { return null; }

            var p = Mouse.GetPosition(item);

            double runningWidth = 0;

            for (int i = 0; i < gv.Columns.Count; i++)
            {
                runningWidth += gv.Columns[i].ActualWidth;

                if (p.X <= runningWidth)
                {
                    return i switch
                    {
                        0 => "PointNumber",
                        1 => "Northing",
                        2 => "Easting",
                        3 => "Elevation",
                        4 => "Description",
                        _ => null
                    };
                }
            }

            return null;
        }
        private void PointsListInlineEditBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox tb) { return; }

            var lvi = VisualTreeHelpers.FindAncestor<ListViewItem>(tb);
            if (lvi == null) { return; }
            if (lvi.DataContext is not CogoPoint cp) { return; }

            string? field = InlineEdit.GetEditingField(lvi);
            if (string.IsNullOrEmpty(field)) { return; }

            var binding = tb.GetBindingExpression(TextBox.TextProperty);

            // Determine whether we're editing a newly created point.
            bool isNewPointRow = _isCreatingNewPoint &&
                                 ReferenceEquals(cp, _newPoint);

            if (e.Key == Key.Enter)
            {
                e.Handled = true;

                if (!CommitPointsListCellEdit(tb, cp, field))
                {
                    return;
                }

                if (isNewPointRow)
                {
                    // --------------------------------------------------
                    // NEW POINT MODE
                    // --------------------------------------------------

                    int idx = Array.IndexOf(_newPointFieldOrder, field);

                    if (idx >= 0 && idx < _newPointFieldOrder.Length - 1)
                    {
                        // Advance to the next property.
                        _newPointFieldIndex = idx + 1;

                        string nextField = _newPointFieldOrder[_newPointFieldIndex];

                        // The previous TextBox is no longer active.
                        _activePointsEditBox = null;
                        _pointsEditTargets.Clear();

                        // Switch the editable field.
                        InlineEdit.SetEditingField(lvi, nextField);

                        lvi.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            string? tboxName = GetPointsPropertyFromTBox(nextField);

                            if (tboxName != null &&
                                VisualTreeHelpers.FindByName(lvi, tboxName) is TextBox nextTb)
                            {
                                _activePointsEditBox = nextTb;
                                _pointsEditTargets = [cp];

                                nextTb.Focus();
                                nextTb.SelectAll();
                            }

                        }), DispatcherPriority.Input);
                    }
                    else
                    {
                        _activePointsEditBox = null;
                        _pointsEditTargets.Clear();

                        InlineEdit.SetEditingField(lvi, null);

                        _isCreatingNewPoint = false;
                        _newPoint = null;
                        _newPointFieldIndex = -1;

                        CadManager.CogoPointCircleVerticesDirty = true;
                        CadManager.CogoPointTextVerticesDirty = true;
                    }

                    return;
                }

                // --------------------------------------------------
                // NORMAL EDIT MODE (INCLUDING BULK EDITING)
                // --------------------------------------------------

                _activePointsEditBox = null;
                _pointsEditTargets.Clear();

                InlineEdit.SetEditingField(lvi, null);

                // Move keyboard focus away from the completed edit.
                tb.MoveFocus(
                    new TraversalRequest(FocusNavigationDirection.Next));

                return;
            }

            if (e.Key == Key.Escape)
            {
                e.Handled = true;

                // Restore the original value.
                binding?.UpdateTarget();

                if (binding != null)
                {
                    Validation.ClearInvalid(binding);
                }

                // Clear the active edit session.
                _activePointsEditBox = null;
                _pointsEditTargets.Clear();

                if (isNewPointRow)
                {
                    // --------------------------------------------------
                    // CANCEL NEW POINT CREATION
                    // --------------------------------------------------

                    if (CadManager != null && _newPoint != null)
                    {
                        CadManager.TryDeletePoint(_newPoint);

                        CadManager.CogoPointCircleVerticesDirty = true;
                        CadManager.CogoPointTextVerticesDirty = true;
                    }

                    _isCreatingNewPoint = false;
                    _newPoint = null;
                    _newPointFieldIndex = -1;

                    InlineEdit.SetEditingField(lvi, null);

                    pointsListView.SelectedItem = null;

                    return;
                }

                // --------------------------------------------------
                // CANCEL NORMAL / BULK EDIT
                // --------------------------------------------------

                InlineEdit.SetEditingField(lvi, null);

                return;
            }
        }

        private void BeginPointsListCellEdit(ListViewItem lvi, string field)
        {
            if (lvi.DataContext is not CogoPoint cp)
                return;

            string? tboxName = GetPointsPropertyFromTBox(field);
            if (tboxName is null)
                return;

            // Point numbers must always be edited individually.
            if (field == nameof(CogoPoint.PointNumber))
            {
                _pointsEditTargets = [cp];
            }
            else
            {
                // Only bulk-edit if the clicked point belongs
                // to the current multi-selection.
                _pointsEditTargets =
                    lvi.IsSelected ? pointsListView.SelectedItems.OfType<CogoPoint>().ToList() : [cp];
            }

            InlineEdit.SetEditingField(lvi, field);

            lvi.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (VisualTreeHelpers.FindByName(lvi, tboxName) is TextBox editBox)
                {
                    _activePointsEditBox = editBox;

                    editBox.Focus();
                    editBox.SelectAll();
                }
            }), DispatcherPriority.Input);
        }
        private bool CommitPointsListCellEdit(TextBox tb, CogoPoint cp, string field)
        {
            var binding = tb.GetBindingExpression(TextBox.TextProperty);

            string text = tb.Text;

            string? errorMessage = null;
            double number = 0;

            switch (field)
            {
                case nameof(CogoPoint.PointNumber):
                    if (!ValidationService.ValidatePointNumberChange(
                            text, cp, CadManager, out string pointError))
                    {
                        errorMessage = pointError;
                    }
                    break;

                case nameof(CogoPoint.Northing):
                case nameof(CogoPoint.Easting):
                case nameof(CogoPoint.Elevation):
                    if (!double.TryParse(
                            text,
                            NumberStyles.Float | NumberStyles.AllowThousands,
                            CultureInfo.CurrentCulture,
                            out number) ||
                        !double.IsFinite(number))
                    {
                        errorMessage = $"{field} must be a valid number.";
                    }
                    break;

                case nameof(CogoPoint.Description):
                    if (!ValidationService.ValidateString(
                            text, out string descriptionError))
                    {
                        errorMessage = descriptionError;
                    }
                    break;

                default:
                    return false;
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

                return false;
            }

            if (binding != null)
                Validation.ClearInvalid(binding);

            binding?.UpdateSource();

            if (Validation.GetHasError(tb))
                return false;

            if (field != nameof(CogoPoint.PointNumber))
            {
                foreach (CogoPoint point in _pointsEditTargets)
                {
                    if (ReferenceEquals(point, cp))
                        continue;

                    switch (field)
                    {
                        case nameof(CogoPoint.Northing):
                            point.Northing = number;
                            break;

                        case nameof(CogoPoint.Easting):
                            point.Easting = number;
                            break;

                        case nameof(CogoPoint.Elevation):
                            point.Elevation = number;
                            break;

                        case nameof(CogoPoint.Description):
                            point.Description = text;
                            break;
                    }
                }
            }

            if (field is nameof(CogoPoint.Northing) or nameof(CogoPoint.Easting))
            {
                CadManager.UpdateCogoPointTree();
            }

            return true;
        }
        private string GetPointsPropertyFromTBox(string tboxName)
        {
            return tboxName switch
            {
                "PointNumber" => "pointNumberEdit",
                "Northing" => "pointNorthingEdit",
                "Easting" => "pointEastingEdit",
                "Elevation" => "pointElevationEdit",
                "Description" => "pointDescriptionEdit",
                _ => null
            };
        }
        private bool TryCreateNewPoint(
            int num, Vector2d pos, PointGroup pg, float elevation, string description)
        {
            if (!CadManager.TryCreatePoint(num, pos, pg, out CogoPoint p, elevation, description))
                return false;

            pointsListView.SelectedItem = p;
            pointsListView.UpdateLayout();

            // First scroll attempt: realize the group container
            pointsListView.ScrollIntoView(p);

            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                // 1. Find the GroupItem that corresponds to this PointGroup
                GroupItem targetGroupItem = null;

                foreach (var groupItem in VisualTreeHelpers.FindVisualChildren<GroupItem>(pointsListView))
                {
                    if (groupItem.DataContext is CollectionViewGroup cvg &&
                        ReferenceEquals(cvg.Name, pg))
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

                // 3. Now the group is expanded; scroll the point again so its row is in view
                pointsListView.ScrollIntoView(p);

                // 4. On another dispatcher tick, its ListViewItem should exist and we can start editing
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    if (pointsListView.ItemContainerGenerator.ContainerFromItem(p) is ListViewItem container)
                    {
                        // NEW POINT EDIT MODE:
                        _isCreatingNewPoint = true;
                        _newPoint = p;
                        _newPointFieldIndex = 0; // "PointNumber"

                        BeginPointsListCellEdit(container, "PointNumber");
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        {
                            if (pointsListView.ItemContainerGenerator.ContainerFromItem(p) is ListViewItem li)
                            {
                                _isCreatingNewPoint = true;
                                _newPoint = p;
                                _newPointFieldIndex = 0;

                                BeginPointsListCellEdit(li, "PointNumber");
                            }
                        }));
                    }
                }));
            }));

            return true;
        }

        private void PointsListItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ListViewItem lvi ||
                lvi.DataContext is not CogoPoint ||
                !lvi.IsSelected ||
                pointsListView.SelectedItems.Count <= 1 ||
                Keyboard.Modifiers != ModifierKeys.None)
            {
                return;
            }

            // Keep normal editing controls interactive.
            if (e.OriginalSource is DependencyObject source &&
                (source is TextBox ||
                 VisualTreeHelpers.FindAncestor<TextBox>(source) != null))
            {
                return;
            }

            if (e.ClickCount == 2)
            {
                string? field = GetFieldFromMousePosition(lvi);

                if (!string.IsNullOrEmpty(field))
                    BeginPointsListCellEdit(lvi, field);
            }

            e.Handled = true;
        }

        private void PointsListView_Loaded(object sender, RoutedEventArgs e)
            => UpdatePointsListColumnWidths();
        private void PointsListView_SizeChanged(object sender, SizeChangedEventArgs e)
            => UpdatePointsListColumnWidths();
        private void UpdatePointsListColumnWidths()
        {
            if (pointsListView.View is not GridView gridView)
                return;

            double availableWidth = pointsListView.ActualWidth;

            if (availableWidth <= 0)
                return;

            double columnWidth = availableWidth / 5.0;

            gridView.Columns[0].Width = columnWidth * 0.75;
            gridView.Columns[1].Width = columnWidth;
            gridView.Columns[2].Width = columnWidth;
            gridView.Columns[3].Width = columnWidth;
            gridView.Columns[4].Width = columnWidth * 1.25;
        }
        #endregion

        // ------------------------------------------------------------------------
        // ------------------------------------------------------------------------
        // ----------  PROPERTIES PANEL TEXTBOXES VALIDATION & BEHAVIOR -----------
        // ------------------------------------------------------------------------
        // ------------------------------------------------------------------------
        #region Properties Panel Methods
        private async void PropertiesBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            if ((ViewsTabVisible || PointsTabVisible) && _mainPanelIsVisible)
            {
                await Task.Delay(GlobalHelperProperties.PopOutCloseDelay);
            }
            if (propertiesBorder.IsMouseOver)
            {
                PointsTabVisible = false;
                PointsTabOpacity = 0;
                PropertiesTabVisible = true;
                PropertiesTabOpacity = 1;
                ViewsTabOpacity = 0;
                ViewsTabVisible = false;
            }
        }
        private void PropertiesTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var textBox = sender as TextBox;

            if (textBox != null && !textBox.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                textBox.Focus(); // Triggers GotKeyboardFocus
            }
        }
        private void PropertiesTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            textBox?.SelectAll();
        }

        // Point Number Properties
        private void PropertiesPointNumberTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (CogoPointSelectionViewModel.SelectedPoints.Count > 0 && sender is TextBox textbox)
            {
                var text = textbox.Text;
                var binding = textbox.GetBindingExpression(TextBox.TextProperty);
                bool isValid = int.TryParse(text, out int pointNum) && pointNum > 0;
                var pointNumExists = CadManager.PointExists(pointNum);
                if (isValid && !pointNumExists)
                {
                    binding?.UpdateSource();
                    Validation.ClearInvalid(binding);
                }
                else
                {
                    binding?.UpdateTarget();
                    Validation.ClearInvalid(binding);
                }
            }
        }
        private void PropertiesPointNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox tb) { return; }

            var text = tb.Text;
            bool isValid = int.TryParse(text, out int number) && number > 0;

            if (!isValid)
            {
                Validation.MarkInvalid(
                    BindingOperations.GetBindingExpression(tb, TextBox.TextProperty),
                    new ValidationError(new PointNumberValidationRule(), tb.GetBindingExpression(TextBox.TextProperty), "Point number must be a positive integer", null));
            }
            else
            {
                Validation.ClearInvalid(tb.GetBindingExpression(TextBox.TextProperty));
            }
        }
        private void PropertiesPointNumberTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox textbox) { return; }

            if (e.Key == Key.Enter)
            {
                if (textbox.Text == CogoPointSelectionViewModel.PointNumber)
                {
                    var binding = textbox.GetBindingExpression(TextBox.TextProperty);
                    binding?.UpdateSource();
                    Validation.ClearInvalid(binding);

                    var request = new TraversalRequest(FocusNavigationDirection.Next);
                    (sender as UIElement)?.MoveFocus(request);
                    e.Handled = true;

                    return;
                }

                var isValid = ValidationService.ValidateNewPointNumber(textbox.Text, CadManager, out string errorMessage);
                if (!isValid)
                {
                    Validation.MarkInvalid(BindingOperations.GetBindingExpression(textbox, TextBox.TextProperty),
                            new ValidationError(new DataErrorValidationRule(), textbox.GetBindingExpression(TextBox.TextProperty), errorMessage, null));
                }
                else
                {
                    var binding = textbox.GetBindingExpression(TextBox.TextProperty);
                    binding?.UpdateSource();
                    Validation.ClearInvalid(binding);

                    var request = new TraversalRequest(FocusNavigationDirection.Next);
                    (sender as UIElement)?.MoveFocus(request);
                    e.Handled = true;
                }
            }
            if (e.Key == Key.Escape)
            {
                var binding = textbox.GetBindingExpression(TextBox.TextProperty);
                binding?.UpdateTarget();
                Validation.ClearInvalid(binding);
            }
        }

        // Northing Properties
        private void PropertiesNorthingTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (CogoPointSelectionViewModel.SelectedPoints.Count > 0 && sender is TextBox textbox)
            {
                var text = textbox.Text;
                var binding = textbox.GetBindingExpression(TextBox.TextProperty);
                bool isValid = double.TryParse(text, out _);
                if (isValid)
                {
                    binding?.UpdateSource();
                    Validation.ClearInvalid(binding);
                    CadManager.UpdateCogoPointTree();
                }
                else
                {
                    binding?.UpdateTarget();
                    Validation.ClearInvalid(binding);
                }
            }
        }
        private void PropertiesNorthingTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox tb) { return; }

            var text = tb.Text;
            bool isValid = double.TryParse(text, out double number);
            if (!isValid)
            {
                Validation.MarkInvalid(
                    BindingOperations.GetBindingExpression(tb, TextBox.TextProperty),
                    new ValidationError(new DoubleValidationRule(), tb.GetBindingExpression(TextBox.TextProperty), "Northing must be a valid number", null));
            }
            else
            {
                Validation.ClearInvalid(tb.GetBindingExpression(TextBox.TextProperty));
            }
        }
        private void PropertiesNorthingTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox textbox) { return; }

            if (e.Key == Key.Enter)
            {
                var text = textbox.Text;
                bool isValid = double.TryParse(text, out _);
                var binding = textbox.GetBindingExpression(TextBox.TextProperty);
                if (isValid)
                {
                    binding?.UpdateSource();
                    Validation.ClearInvalid(binding);

                    var request = new TraversalRequest(FocusNavigationDirection.Next);
                    (sender as UIElement)?.MoveFocus(request);
                    CadManager.UpdateCogoPointTree();
                    e.Handled = true;
                }
            }
            if (e.Key == Key.Escape)
            {
                var binding = textbox.GetBindingExpression(TextBox.TextProperty);
                binding?.UpdateTarget();
                Validation.ClearInvalid(binding);
            }
        }

        // Easting Properties
        private void PropertiesEastingTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (CogoPointSelectionViewModel.SelectedPoints.Count > 0 && sender is TextBox textbox)
            {
                var text = textbox.Text;
                var binding = textbox.GetBindingExpression(TextBox.TextProperty);
                bool isValid = double.TryParse(text, out _);
                if (isValid)
                {
                    binding?.UpdateSource();
                    Validation.ClearInvalid(binding);
                    CadManager.UpdateCogoPointTree();
                }
                else
                {
                    binding?.UpdateTarget();
                    Validation.ClearInvalid(binding);
                }
            }
        }
        private void PropertiesEastingTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox tb) { return; }

            var text = tb.Text;
            bool isValid = double.TryParse(text, out _);
            if (!isValid)
            {
                Validation.MarkInvalid(
                    BindingOperations.GetBindingExpression(tb, TextBox.TextProperty),
                    new ValidationError(new DoubleValidationRule(), tb.GetBindingExpression(TextBox.TextProperty), "Easting must be a valid number", null));
            }
            else
            {
                Validation.ClearInvalid(tb.GetBindingExpression(TextBox.TextProperty));
            }
        }
        private void PropertiesEastingTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox textbox) { return; }

            if (e.Key == Key.Enter)
            {
                var text = textbox.Text;
                bool isValid = double.TryParse(text, out _);
                var binding = textbox.GetBindingExpression(TextBox.TextProperty);
                if (isValid)
                {
                    binding?.UpdateSource();
                    Validation.ClearInvalid(binding);

                    var request = new TraversalRequest(FocusNavigationDirection.Next);
                    (sender as UIElement)?.MoveFocus(request);
                    CadManager.UpdateCogoPointTree();
                    e.Handled = true;
                }
            }
            if (e.Key == Key.Escape)
            {
                var binding = textbox.GetBindingExpression(TextBox.TextProperty);
                binding?.UpdateTarget();
                Validation.ClearInvalid(binding);
            }
        }

        // Elevation Properties
        private void PropertiesElevationTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (CogoPointSelectionViewModel.SelectedPoints.Count > 0 && sender is TextBox textbox)
            {
                var text = textbox.Text;
                var binding = textbox.GetBindingExpression(TextBox.TextProperty);
                bool isValid = double.TryParse(text, out _);
                if (isValid)
                {
                    binding?.UpdateSource();
                    Validation.ClearInvalid(binding);
                }
                else
                {
                    binding?.UpdateTarget();
                    Validation.ClearInvalid(binding);
                }
            }
        }
        private void PropertiesElevationTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox tb) { return; }

            var text = tb.Text;
            bool isValid = double.TryParse(text, out _);
            if (!isValid)
            {
                Validation.MarkInvalid(
                    BindingOperations.GetBindingExpression(tb, TextBox.TextProperty),
                    new ValidationError(new DoubleValidationRule(), tb.GetBindingExpression(TextBox.TextProperty), "Elevation must be a valid number", null));
            }
            else
            {
                Validation.ClearInvalid(tb.GetBindingExpression(TextBox.TextProperty));
            }
        }
        private void PropertiesElevationTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox textbox) { return; }

            if (e.Key == Key.Enter)
            {
                var text = textbox.Text;
                bool isValid = double.TryParse(text, out _);
                var binding = textbox.GetBindingExpression(TextBox.TextProperty);
                if (isValid)
                {
                    binding?.UpdateSource();
                    Validation.ClearInvalid(binding);

                    var request = new TraversalRequest(FocusNavigationDirection.Next);
                    (sender as UIElement)?.MoveFocus(request);
                    e.Handled = true;
                }
            }
            if (e.Key == Key.Escape)
            {
                var binding = textbox.GetBindingExpression(TextBox.TextProperty);
                binding?.UpdateTarget();
                Validation.ClearInvalid(binding);
            }
        }

        // Description Properties
        private void PropertiesDescriptionTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (CogoPointSelectionViewModel.SelectedPoints.Count > 0 && sender is TextBox textbox)
            {
                var text = textbox.Text;
                var binding = textbox.GetBindingExpression(TextBox.TextProperty);
                bool isValid = double.TryParse(text, out _);
                if (isValid)
                {
                    binding?.UpdateSource();
                    Validation.ClearInvalid(binding);
                }
                else
                {
                    binding?.UpdateTarget();
                    Validation.ClearInvalid(binding);
                }
            }
        }
        private void PropertiesDescriptionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox tb) { return; }

            var rule = new NoIllegalCharactersRule();
            var result = rule.Validate(tb.Text, CultureInfo.CurrentCulture);

            if (!result.IsValid)
            {
                Validation.MarkInvalid(
                    BindingOperations.GetBindingExpression(tb, TextBox.TextProperty),
                    new ValidationError(rule, tb.GetBindingExpression(TextBox.TextProperty), "", null));
            }
            else
            {
                Validation.ClearInvalid(tb.GetBindingExpression(TextBox.TextProperty));
            }
        }
        private void PropertiesDescriptionTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox textbox) { return; }

            if (e.Key == Key.Enter)
            {
                var rule = new NoIllegalCharactersRule();
                var result = rule.Validate(textbox.Text, CultureInfo.CurrentCulture);
                var binding = textbox.GetBindingExpression(TextBox.TextProperty);

                if (result.IsValid)
                {
                    binding?.UpdateSource();
                    Validation.ClearInvalid(binding);

                    var request = new TraversalRequest(FocusNavigationDirection.Next);
                    (sender as UIElement)?.MoveFocus(request);
                    e.Handled = true;
                }
            }
            if (e.Key == Key.Escape)
            {
                var binding = textbox.GetBindingExpression(TextBox.TextProperty);
                binding?.UpdateTarget();
                Validation.ClearInvalid(binding);
            }
        }

        // Point Group Properties
        private void PropertiesPointGroupComboBox_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox comboBox)
                return;

            if (comboBox.SelectedItem is not PointGroup pointGroup)
                return;

            foreach (var point in CogoPointSelectionViewModel.SelectedPoints)
            {
                if (!ReferenceEquals(point.PointGroup, pointGroup))
                {
                    point.UpdatePointGroup(pointGroup);
                }
            }

            CogoPointSelectionViewModel.Refresh();
        }
        #endregion

        // ------------------------------------------------------------------------
        // ------------------------------------------------------------------------
        // -------------- SCENES LISTVIEW BEHAVIOR AND CONTEXT MENU ---------------
        // ------------------------------------------------------------------------
        // ------------------------------------------------------------------------
        #region Scenes ListView Methods
        private void ScenesListView_Loaded(object sender, RoutedEventArgs e)
        {
            ListView listview = sender as ListView;

            // Set column widths on each gridview
            GridView scenesGridView = listview.View as GridView;
            double scenesGridViewTotalWidth = mainPanel.ActualWidth;
            double scenesGridViewColumnWidth = scenesGridViewTotalWidth / scenesGridView.Columns.Count;
            if (scenesGridViewColumnWidth > 0)
            {
                double nameWidthFactor = 1.4;
                double dimsWidthFactor = (5 - nameWidthFactor) / (scenesGridView.Columns.Count - 1);
                scenesGridView.Columns[0].Width = scenesGridViewColumnWidth * nameWidthFactor;
                scenesGridView.Columns[1].Width = scenesGridViewColumnWidth * dimsWidthFactor;
                scenesGridView.Columns[2].Width = scenesGridViewColumnWidth * dimsWidthFactor;
                scenesGridView.Columns[3].Width = scenesGridViewColumnWidth * dimsWidthFactor;
                scenesGridView.Columns[4].Width = scenesGridViewColumnWidth * dimsWidthFactor;
            }
        }
        private void ScenesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedScenes.Clear();
            var selectedItems = (sender as ListView).SelectedItems;

            foreach (Scene scene in selectedItems)
            {
                if (scene is not null)
                {
                    _selectedScenes.Add(scene);
                }
            }
        }
        private async void ScenesBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            if ((PropertiesTabVisible || PointsTabVisible) && _mainPanelIsVisible)
            {
                await Task.Delay(GlobalHelperProperties.PopOutCloseDelay);
            }
            if (viewsBorder.IsMouseOver)
            {
                PointsTabVisible = false;
                PointsTabOpacity = 0;
                PropertiesTabVisible = false;
                PropertiesTabOpacity = 0;
                ViewsTabVisible = true;
                ViewsTabOpacity = 1;
            }
        }
        private void NewSceneButton_Click(object sender, RoutedEventArgs e)
        {
            if (Camera is null) { return; }

            string tempViewName = Camera.GetTempSceneName();
            if (!Camera.TrySaveScene(tempViewName, out var newScene) || newScene == null) { return; }

            _newScene = newScene;

            scenesListView.SelectedItem = _newScene;
            scenesListView.UpdateLayout();
            scenesListView.ScrollIntoView(_newScene);

            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                var container = scenesListView.ItemContainerGenerator.ContainerFromItem(_newScene) as ListViewItem;
                if (container == null)
                {
                    // try again once more if virtualization delayed it
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        if (scenesListView.ItemContainerGenerator.ContainerFromItem(_newScene) is ListViewItem li)
                        {
                            BeginScenesListCellEdit(li, "Name");
                        }
                    }));
                }
                else
                {
                    BeginScenesListCellEdit(container, "Name");
                }
            }));
        }
        private void ScenesListNewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (Camera is null) { return; }

            string tempViewName = Camera.GetTempSceneName();
            if (!Camera.TrySaveScene(tempViewName, out var newScene) || newScene == null) { return; }

            _newScene = newScene;

            scenesListView.SelectedItem = _newScene;
            scenesListView.UpdateLayout();
            scenesListView.ScrollIntoView(_newScene);

            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                var container = scenesListView.ItemContainerGenerator.ContainerFromItem(_newScene) as ListViewItem;
                if (container == null)
                {
                    // try again once more if virtualization delayed it
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        if (scenesListView.ItemContainerGenerator.ContainerFromItem(_newScene) is ListViewItem li)
                        {
                            BeginScenesListCellEdit(li, "Name");
                        }
                    }));
                }
                else
                {
                    BeginScenesListCellEdit(container, "Name");
                }
            }));
        }
        private void ScenesListRenameMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_lastScenesListItem is null) { return; }

            BeginScenesListCellEdit(_lastScenesListItem, "Name");
        }
        private void ScenesListDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (Camera is null) { return; }

            var scenesToDelete = new List<Scene>(_selectedScenes);
            foreach (var scene in scenesToDelete)
            {
                Camera.TryDeleteScene(scene);
            }
        }
        private void ScenesListZoomMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (Camera is null || _lastRightClickedScene is null) { return; }

            Camera.LoadScene(_lastRightClickedScene);
        }
        private void ScenesListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // element that was actually right-clicked
            if (e.OriginalSource is not DependencyObject src) { return; }
            if (e.OriginalSource is not FrameworkElement fe) { return; }

            // Walk up from the OriginalSource until we find something we can map to a field
            ListViewItem? lvi = src as ListViewItem
                                   ?? VisualTreeHelpers.FindAncestor<ListViewItem>(src);
            if (lvi == null) { return; }
            if (lvi.DataContext is not Scene scene) { return; }
            _lastRightClickedScene = scene;

            string field = ScenesInferFieldNameFromDisplayElement(fe);
            if (string.IsNullOrEmpty(field)) { return; }

            _lastScenesListItem = lvi;
        }
        private void ScenesListView_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (!mainPanel.IsMouseOver)
            {
                _isMouseOverPanel = false;
                _hideTimer.Start();
            }
        }
        private void ScenesCellDisplay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount < 2) { return; }
            if (sender is not FrameworkElement fe || VisualTreeHelpers.FindAncestor<ListViewItem>(fe) is not ListViewItem lvi) { return; }

            string field = ScenesInferFieldNameFromDisplayElement(fe);
            if (string.IsNullOrEmpty(field)) { return; }

            BeginScenesListCellEdit(lvi, field);
        }
        private static string ScenesInferFieldNameFromDisplayElement(FrameworkElement fe)
        {
            var grid = VisualTreeHelpers.FindAncestor<Grid>(fe);
            if (grid?.Name is string s && !string.IsNullOrWhiteSpace(s))
            {
                // Map headers to property names as written in XAML
                return s switch
                {
                    "sceneNameGrid" => "Name",
                    _ => null
                };
            }
            return null;
        }
        private void BeginScenesListCellEdit(ListViewItem lvi, string field)
        {
            if (lvi == null) { return; }

            InlineEdit.SetEditingField(lvi, field);
            string tboxName = GetScenesPropertyFromTBox(field);
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
        private void ScenesListInlineEditBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox tb) { return; }

            var lvi = VisualTreeHelpers.FindAncestor<ListViewItem>(tb);
            if (lvi == null) { return; }
            if (lvi.DataContext is not Scene scene) { return; }

            // Which field are we currently editing? (set earlier in CellDisplay_MouseDown)
            string field = InlineEdit.GetEditingField(lvi);
            var binding = tb.GetBindingExpression(TextBox.TextProperty);

            if (e.Key == Key.Enter)
            {
                string text = tb.Text;
                string? errorMessage = null;

                switch (field)
                {
                    // ---- POINT NUMBER: non-negative int, must NOT already exist ----
                    case "Name":
                        {
                            if (!ValidationService.ValidateSceneNameChange(text, scene, Camera, out string svcError))
                            {
                                errorMessage = svcError;
                            }
                            break;
                        }

                    default: break;
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
                    if (binding != null)
                    {
                        Validation.ClearInvalid(binding);
                        binding.UpdateSource();
                    }

                    InlineEdit.SetEditingField(lvi, null);
                    e.Handled = true;

                    // move focus to next cell
                    (tb as UIElement)?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    return;
                }
            }
            if (e.Key == Key.Escape)
            {
                // revert UI to source
                binding?.UpdateTarget();
                InlineEdit.SetEditingField(lvi, null);
                e.Handled = true;
                return;
            }
        }
        private string GetScenesPropertyFromTBox(string tboxName)
        {
            return tboxName switch
            {
                "Name" => "sceneNameEdit",
                _ => null
            };
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
