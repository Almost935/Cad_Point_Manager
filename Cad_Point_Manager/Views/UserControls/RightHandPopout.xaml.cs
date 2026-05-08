using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.DrawingObjects;
using Cad_Point_Manager.Models.PointRendering;
using Cad_Point_Manager.Views.Assorted;
using ColorPicker;
using SharpDX;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Color = System.Windows.Media.Color;
using TextBox = System.Windows.Controls.TextBox;

namespace Cad_Point_Manager.Views.UserControls
{
    /// <summary>
    /// Interaction logic for RightHandPopout.xaml
    /// </summary>
    public partial class RightHandPopout : UserControl, INotifyPropertyChanged
    {
        #region Fields
        private const double _panelHideTime = 200;

        private readonly List<ObjectLayer> _selectedLayers = [];
        private readonly List<PointGroup> _selectedPointGroups = [];

        private string? _lastContextField;
        private ListViewItem? _lastPointGroupListViewItem;

        private int _pointGroupAnchorIndex = -1; // where SHIFT ranges start

        private bool _mainPanelIsVisible = false;
        private bool _layerListVisible = true;
        private double _layerListOpacity = 0;
        private bool _layerListColorPickerOpen = false;
        private bool _pointGroupListVisible = true;
        private double _pointGroupListOpacity = 0;
        private bool _pointGroupListColorPickerOpen = false;
        private PointGroup _openColorPickerPG;
        private Color _prevPointGroupColor;
        private bool _pointGroupsMessageBoxOpen = false;

        //private string _newPointGroupName = "";
        //private Color _newPointGroupColor = Colors.Black;
        private double _newPointGroupScale = 1;
        private bool _newPointColorPickerToggleOpen = false;
        private ICollectionView _availableMergePointGroups;
        private PointGroup _mergePointGroup = null;
        private bool _ignorePGListViewSelectionChanged = false;

        private readonly DispatcherTimer _hideTimer = new();
        private bool _isMouseOverPanel = false;
        private ScaleTransform _mainPanelTransform = new();
        #endregion

        #region Properties
        public bool LayerListVisible
        {
            get { return _layerListVisible; }
            set
            {
                _layerListVisible = value;
                OnPropertyChanged(nameof(LayerListVisible));
            }
        }
        public bool PointGroupListVisible
        {
            get { return _pointGroupListVisible; }
            set
            {
                _pointGroupListVisible = value;
                OnPropertyChanged(nameof(PointGroupListVisible));
            }
        }
        public double LayerListOpacity
        {
            get { return _layerListOpacity; }
            set
            {
                _layerListOpacity = value;
                OnPropertyChanged(nameof(LayerListOpacity));
            }
        }
        public bool LayerListColorPickerOpen
        {
            get { return _layerListColorPickerOpen; }
            set
            {
                _layerListColorPickerOpen = value;
                OnPropertyChanged(nameof(LayerListColorPickerOpen));
            }
        }
        public double PointGroupListOpacity
        {
            get { return _pointGroupListOpacity; }
            set
            {
                _pointGroupListOpacity = value;
                OnPropertyChanged(nameof(PointGroupListOpacity));
            }
        }
        public bool PointGroupListColorPickerOpen
        {
            get { return _pointGroupListColorPickerOpen; }
            set
            {
                _pointGroupListColorPickerOpen = value;
                OnPropertyChanged(nameof(PointGroupListColorPickerOpen));
            }
        }
        public double NewPointGroupScale
        {
            get { return _newPointGroupScale; }
            set
            {
                _newPointGroupScale = value;
                OnPropertyChanged(nameof(NewPointGroupScale));
            }
        }
        public bool NewPointColorPickerToggleOpen
        {
            get { return _newPointColorPickerToggleOpen; }
            set
            {
                _newPointColorPickerToggleOpen = value;
                OnPropertyChanged(nameof(NewPointColorPickerToggleOpen));
            }
        }
        public ICollectionView AvailableMergePointGroups
        {
            get => _availableMergePointGroups;
            set
            {
                _availableMergePointGroups = value;
                OnPropertyChanged(nameof(AvailableMergePointGroups));
            }
        }
        public PointGroup MergePointGroup
        {
            get => _mergePointGroup;
            set
            {
                _mergePointGroup = value;
                OnPropertyChanged(nameof(MergePointGroup));
            }
        }

        public List<CogoPoint> SelectedCogoPoints => _selectedPointGroups.SelectMany(pg => pg.Points).ToList();
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
            typeof(RightHandPopout),
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
            typeof(RightHandPopout),
            new PropertyMetadata(null, (d, e) => ((RightHandPopout)d).InitializeMergeCollectionView()));

        public ICollectionView LayerCollectionView
        {
            get { return (ICollectionView)GetValue(LayerCollectionViewProperty); }
            set { SetValue(LayerCollectionViewProperty, value); }
        }
        public static readonly DependencyProperty LayerCollectionViewProperty =
        DependencyProperty.Register(
            nameof(LayerCollectionView),
            typeof(ICollectionView),
            typeof(RightHandPopout),
            new PropertyMetadata(null));

        public ICollectionView PointGroupCollectionView
        {
            get { return (ICollectionView)GetValue(PointGroupCollectionViewProperty); }
            set { SetValue(PointGroupCollectionViewProperty, value); }
        }
        public static readonly DependencyProperty PointGroupCollectionViewProperty =
        DependencyProperty.Register(
            nameof(PointGroupCollectionView),
            typeof(ICollectionView),
            typeof(RightHandPopout),
            new PropertyMetadata(null));
        #endregion

        #region Constructors
        public RightHandPopout()
        {
            InitializeComponent();

            //pointGroupsListView.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent,
            //    new MouseButtonEventHandler(PointGroupsListView_PreviewMouseLeftButtonDown), handledEventsToo: true);
            layersListView.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent,
                new MouseButtonEventHandler(LayersListView_PreviewMouseLeftButtonDown),
                handledEventsToo: true);

            mainPanel.RenderTransform = _mainPanelTransform;

            HideControl();

            _hideTimer.Interval = TimeSpan.FromSeconds(1);
            _hideTimer.Tick += HideTimer_Tick;
        }
        #endregion

        #region Methods
        private void HideTimer_Tick(object sender, EventArgs e)
        {
            _hideTimer.Stop();
            if (!_isMouseOverPanel && !PointGroupListColorPickerOpen && !NewPointColorPickerToggleOpen &&
                !LayerListColorPickerOpen && !_pointGroupsMessageBoxOpen && !pgListViewContextMenu.IsOpen)
            {
                HideControl();
            }
        }

        private void OverallGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            _isMouseOverPanel = true;
            _hideTimer.Stop();
            ShowControl();
        }
        private void OverallGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            //if (pgListViewContextMenu.IsOpen || _pointGroupsMessageBoxOpen) { return; }

            _isMouseOverPanel = false;
            _hideTimer.Start();
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

        // Layer related methods
        private void LayersListView_Loaded(object sender, RoutedEventArgs e)
        {
            ListView listview = sender as ListView;

            // Set column widths on each gridview
            GridView layerListGridView = listview.View as GridView;
            double layerListTotalWidth = listview.ActualWidth;
            double layerListColumnWidth = layerListTotalWidth / layerListGridView.Columns.Count;
            if (layerListColumnWidth > 0)
            {
                // Set column with name and visibility checkbox to double that of the color picker col
                layerListGridView.Columns[0].Width = layerListColumnWidth * 1.8;
                layerListGridView.Columns[1].Width = layerListColumnWidth * 0.6;
                layerListGridView.Columns[2].Width = layerListColumnWidth * 0.6;
            }
        }
        private void LayersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedLayers.Clear();
            var selectedItems = (sender as ListView).SelectedItems;

            foreach (var selectedItem in selectedItems)
            {
                if (selectedItem is KeyValuePair<string, ObjectLayer> selectedLayer)
                {
                    if (selectedLayer.Value is not null)
                    {
                        _selectedLayers.Add(selectedLayer.Value);
                    }
                }
            }
        }
        private void LayerCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var layer in _selectedLayers)
            {
                layer.IsVisible = true;
            }
        }
        private void LayerCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var layer in _selectedLayers)
            {
                layer.IsVisible = false;
            }
        }
        private async void LayersBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            if (PointGroupListVisible && _mainPanelIsVisible)
            {
                await Task.Delay(GlobalHelperProperties.PopOutCloseDelay);
            }
            if (layersBorder.IsMouseOver)
            {
                PointGroupListVisible = false;
                LayerListVisible = true;
                PointGroupListOpacity = 0;
                LayerListOpacity = 1;
            }
        }
        private void LayersPortableColorPicker_IsPopupOpenChanged(object sender, bool isOpen)
        {
            if (isOpen) { return; }

            PortableColorPicker colorpicker = sender as PortableColorPicker;
            if (colorpicker is not null)
            {
                var color = colorpicker.SelectedColor;
                foreach (var layer in _selectedLayers)
                {
                    layer.Color = new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, 1.0f);
                }
            }
        }
        private void LayersListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var origin = (DependencyObject)e.OriginalSource;
            if (ItemsControl.ContainerFromElement(layersListView, origin) is not ListViewItem item) return;

            if (!item.IsSelected)
            {
                if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == ModifierKeys.None)
                    layersListView.SelectedItems.Clear();

                item.IsSelected = true;
                item.Focus();
            }
        }

        // Point group related methods
        private async void PointGroupsBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            if (LayerListVisible && _mainPanelIsVisible)
            {
                await Task.Delay(GlobalHelperProperties.PopOutCloseDelay);
            }
            if (pointGroupsBorder.IsMouseOver)
            {
                LayerListVisible = false;
                PointGroupListVisible = true;
                LayerListOpacity = 0;
                PointGroupListOpacity = 1;
            }
        }
        private void PointGroupsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListView lv
                || _ignorePGListViewSelectionChanged) { return; }

            // Maintain anchor unless we are actively doing a SHIFT range
            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                if (lv.SelectedItem != null)
                {
                    _pointGroupAnchorIndex = lv.Items.IndexOf(lv.SelectedItem);
                }
                else if (lv.SelectedItems.Count > 0)
                {
                    _pointGroupAnchorIndex = lv.Items.IndexOf(lv.SelectedItems[lv.SelectedItems.Count - 1]);
                }
                else
                {
                    _pointGroupAnchorIndex = -1;
                }
            }

            _selectedPointGroups.Clear();
            var selectedItems = lv.SelectedItems;

            foreach (PointGroup pg in selectedItems)
            {
                _selectedPointGroups.Add(pg);
            }

            AvailableMergePointGroups.Refresh();
        }
        private void PointGroupsListView_Loaded(object sender, RoutedEventArgs e)
        {
            ListView listview = sender as ListView;

            // Set column widths on each gridview
            GridView pointGroupGridView = listview.View as GridView;
            double pointGroupLTotalWidth = listview.ActualWidth;
            double pointGroupColumnWidth = pointGroupLTotalWidth / pointGroupGridView.Columns.Count;
            if (pointGroupColumnWidth > 0)
            {
                pointGroupGridView.Columns[0].Width = pointGroupColumnWidth * 1.3;
                pointGroupGridView.Columns[1].Width = pointGroupColumnWidth * 0.9;
                pointGroupGridView.Columns[2].Width = pointGroupColumnWidth * 0.9;
                pointGroupGridView.Columns[3].Width = pointGroupColumnWidth * 1;
                pointGroupGridView.Columns[4].Width = pointGroupColumnWidth * 0.9;
            }
        }
        private void DeletePointGroupButton_Click(object sender, RoutedEventArgs e)
        {
            if (CadManager is null || CadManager.CogoPointManager is null) { return; }

            var count = _selectedPointGroups.Sum(pg => pg.Points.Count);
            if (count > 0)
            {
                _pointGroupsMessageBoxOpen = true;
                if (MessageBox.Show($"Deleting the selected point groups will result in the deletion of {count} Cogo Points. Continue?",
                    "Delete Points", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel) == MessageBoxResult.Cancel)
                {
                    _pointGroupsMessageBoxOpen = false;
                    return;
                }
                _pointGroupsMessageBoxOpen = false;
            }

            var copy = _selectedPointGroups.ToList();
            foreach (var pg in copy) { CadManager.CogoPointManager.DeletePointGroup(pg); }

            CadManager.CogoPointCircleVerticesDirty = true;
            CadManager.CogoPointTextVerticesDirty = true;
        }
        private void CellDisplay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount < 2) { return; }
            if (sender is not FrameworkElement fe || VisualTreeHelpers.FindAncestor<ListViewItem>(fe) is not ListViewItem lvi) { return; }

            string field = InferPGFieldNameFromDisplayElement(fe);
            if (string.IsNullOrEmpty(field)) { return; }

            BeginPointGroupListCellEdit(lvi, field);
        }
        private static string InferPGFieldNameFromDisplayElement(FrameworkElement fe)
        {
            var grid = VisualTreeHelpers.FindAncestor<Grid>(fe);
            if (grid?.Name is string s && !string.IsNullOrWhiteSpace(s))
            {
                // Map headers to property names as written in XAML
                return s switch
                {
                    "groupName" => "Name",
                    "groupScale" => "PointScale",
                    _ => null
                };
            }
            return null;
        }
        private void PGListViewInlineEditBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox tb) { return; }

            var lvi = VisualTreeHelpers.FindAncestor<ListViewItem>(tb);
            if (lvi == null) { return; }
            if (lvi.DataContext is not PointGroup pg) { return; }

            // Which field are we currently editing? (set earlier in CellDisplay_MouseDown)
            string field = InlineEdit.GetEditingField(lvi);
            var binding = tb.GetBindingExpression(TextBox.TextProperty);

            if (e.Key == Key.Enter)
            {
                string text = tb.Text;
                string? errorMessage = null;

                switch (field)
                {
                    case "Name":
                        {
                            if (text == pg.Name)
                            {
                                errorMessage = null;
                                break;
                            }
                            if (!CadManager.CogoPointManager.IsValidPointGroupName(text, out string svcError))
                            {
                                errorMessage = svcError;
                            }
                            break;
                        }

                    case "PointScale":
                        {
                            if (!CadManager.CogoPointManager.IsValidPointScale(text, out string svcError))
                            {
                                errorMessage = svcError;
                            }
                            break;
                        }

                    default:
                        {
                            break;
                        }
                }

                if (errorMessage != null)
                {
                    // Mark invalid and KEEP focus in the textbox
                    if (binding != null)
                    {
                        Validation.MarkInvalid(
                            binding,
                            new ValidationError(
                                new DataErrorValidationRule(),  // or a specific rule type
                                binding,
                                errorMessage,
                                null));
                    }

                    e.Handled = true;
                    return;
                }
                else
                {
                    // Valid – clear any old errors and commit the value
                    if (binding != null)
                    {
                        Validation.ClearInvalid(binding);
                        binding.UpdateSource();
                    }

                    // leave edit mode
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
        private void PointGroupsInlineEditBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // If focus leaves the edit box, exit edit (commit if valid, otherwise keep error text synced)
            if (sender is not TextBox tb) { return; }

            var lvi = VisualTreeHelpers.FindAncestor<ListViewItem>(tb);
            if (lvi == null) { return; }

            var binding = tb.GetBindingExpression(TextBox.TextProperty);
            if (!Validation.GetHasError(tb)) { binding?.UpdateSource(); } // commit if valid

            InlineEdit.SetEditingField(lvi, null);
        }
        private static void BeginPointGroupListCellEdit(ListViewItem lvItem, string field)
        {
            InlineEdit.SetEditingField(lvItem, field);
            string? v = field switch
            {
                "Name" => "pgNameEdit",
                "PointScale" => "pgScaleEdit",
                _ => null
            };
            string tBoxName = v;
            if (tBoxName == null) { return; }

            lvItem.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (VisualTreeHelpers.FindByName(lvItem, tBoxName) is TextBox editBox)
                {
                    editBox.Focus();
                    editBox.SelectAll();
                }
            }), DispatcherPriority.Input);
        }
        private void PointGroupsListView_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (!mainPanel.IsMouseOver)
            {
                _isMouseOverPanel = false;
                _hideTimer.Start();
            }
        }
        private void PointGroupsListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject src) { return; }

            FrameworkElement? fe = src as FrameworkElement
                                   ?? VisualTreeHelpers.FindAncestor<FrameworkElement>(src);
            if (fe == null) { return; }

            // Optional: field and cell for “Edit Cell”
            string field = InferPGFieldNameFromDisplayElement(fe);
            _lastContextField = field;
            _lastPointGroupListViewItem = VisualTreeHelpers.FindAncestor<ListViewItem>(fe);
        }
        private void NewPGMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (CadManager is null || CadManager.CogoPointManager is null) { return; }

            bool created = TryCreateNewPointGroup(
                CadManager.CogoPointManager.GetTempPointGroupName(),
                Colors.Black,
                CadManager.CogoPointManager.PointBaseScale);
        }
        private void EditPGMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_lastPointGroupListViewItem == null || string.IsNullOrEmpty(_lastContextField)) { return; }

            BeginPointGroupListCellEdit(_lastPointGroupListViewItem, _lastContextField);
            pgListViewContextMenu.IsOpen = false;
        }
        private void DeletePGMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (CadManager is null || CadManager.CogoPointManager is null) { return; }

            var count = _selectedPointGroups.Sum(pg => pg.Points.Count);
            if (count > 0)
            {
                _pointGroupsMessageBoxOpen = true;
                if (MessageBox.Show($"Deleting the selected point groups will result in the deletion of {count} Cogo Points. Continue?",
                    "Delete Points", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel) == MessageBoxResult.Cancel)
                {
                    _pointGroupsMessageBoxOpen = false;
                    return;
                }
                _pointGroupsMessageBoxOpen = false;
            }

            var copy = _selectedPointGroups.ToList();
            foreach (var pg in copy) { CadManager.CogoPointManager.DeletePointGroup(pg); }

            CadManager.CogoPointCircleVerticesDirty = true;
            CadManager.CogoPointTextVerticesDirty = true;
        }

        // PointGroups listview visibily checkbox methods
        private void PointGroupsVisibilityCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var pg in _selectedPointGroups)
            {
                pg.IsVisible = true;
            }
        }
        private void PointGroupsVisibilityCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var pg in _selectedPointGroups)
            {
                pg.IsVisible = false;
            }
        }
        private void PointGroupsVisibilityCheckBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (PointGroupListColorPickerOpen) { return; }

            if (sender is not CheckBox cbox ||
                VisualTreeHelpers.FindAncestor<ListViewItem>(cbox) is not ListViewItem lvi ||
                ItemsControl.ItemsControlFromItemContainer(lvi) is not ListView lv) { return; }

            int clicked = lv.ItemContainerGenerator.IndexFromContainer(lvi);
            bool shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            bool ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

            if (shift)
            {
                int anchor = _pointGroupAnchorIndex;
                if (anchor < 0)
                {
                    anchor = lv.SelectedItem != null
                        ? lv.Items.IndexOf(lv.SelectedItem)
                        : clicked;
                    _pointGroupAnchorIndex = anchor; // establish one if needed
                }

                int start = Math.Min(anchor, clicked);
                int end = Math.Max(anchor, clicked);

                lv.SelectedItems.Clear();
                for (int i = start; i <= end; i++) { lv.SelectedItems.Add(lv.Items[i]); }

                lvi.Focus();

                return;
            }

            if (lvi.IsSelected) { return; } // If the item is already selected and shift is not pressed, do nothing

            if (ctrl)
            {
                // Toggle or just add; toggle feels more Windows-like:
                lvi.IsSelected = !lvi.IsSelected;

                // Use this as the next SHIFT anchor
                _pointGroupAnchorIndex = clicked;
                lvi.Focus();
                return;
            }

            // Plain click: single select
            lv.SelectedItems.Clear();
            lvi.IsSelected = true;
            _pointGroupAnchorIndex = clicked;
            lvi.Focus();
        }

        // PointGroups listview color picker methods
        private void PointGroupsColorPicker_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (PointGroupListColorPickerOpen) { return; }

            if (sender is not PortableColorPicker cp ||
                VisualTreeHelpers.FindAncestor<ListViewItem>(cp) is not ListViewItem lvi ||
                ItemsControl.ItemsControlFromItemContainer(lvi) is not ListView lv) { return; }

            int clicked = lv.ItemContainerGenerator.IndexFromContainer(lvi);
            bool shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            bool ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

            if (shift)
            {
                int anchor = _pointGroupAnchorIndex;
                if (anchor < 0)
                {
                    anchor = lv.SelectedItem != null
                        ? lv.Items.IndexOf(lv.SelectedItem)
                        : clicked;
                    _pointGroupAnchorIndex = anchor; // establish one if needed
                }

                int start = Math.Min(anchor, clicked);
                int end = Math.Max(anchor, clicked);

                lv.SelectedItems.Clear();
                for (int i = start; i <= end; i++) { lv.SelectedItems.Add(lv.Items[i]); }

                lvi.Focus();

                return;
            }

            if (lvi.IsSelected) { return; } // If the item is already selected and shift is not pressed, do nothing

            if (ctrl)
            {
                // Toggle or just add; toggle feels more Windows-like:
                lvi.IsSelected = !lvi.IsSelected;

                // Use this as the next SHIFT anchor
                _pointGroupAnchorIndex = clicked;
                lvi.Focus();
                return;
            }

            // Plain click: single select
            lv.SelectedItems.Clear();
            lvi.IsSelected = true;
            _pointGroupAnchorIndex = clicked;
            lvi.Focus();
        }
        private void PointGroupsColorPicker_IsPopupOpenChanged(object sender, bool isOpen)
        {
            if (sender is not PortableColorPicker colorpicker ||
                colorpicker.DataContext is not PointGroup openPG) { return; }

            if (isOpen)
            {
                _openColorPickerPG = openPG;
                _prevPointGroupColor = openPG.Color;

                return;
            }
            else
            {
                if (_openColorPickerPG is null) { return; }

                var color = colorpicker.SelectedColor;
                foreach (var pg in _selectedPointGroups)
                {
                    pg.Color = color;
                }
                _openColorPickerPG.Color = color;
                _openColorPickerPG = null;

                return;
            }
        }
        private void PointGroupsColorPicker_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (sender is PortableColorPicker colorpicker)
                {
                    if (_openColorPickerPG is null) { return; }

                    _openColorPickerPG.Color = _prevPointGroupColor;
                    _openColorPickerPG = null;
                    colorpicker.IsPopupOpen = false;
                    e.Handled = true;
                }
            }
            if (e.Key == Key.Enter)
            {
                if (sender is PortableColorPicker colorpicker)
                {
                    if (_openColorPickerPG is null) { return; }

                    var color = colorpicker.SelectedColor;
                    foreach (var pg in _selectedPointGroups)
                    {
                        pg.Color = color;
                    }
                    _openColorPickerPG.Color = color;
                    _openColorPickerPG = null;
                    colorpicker.IsPopupOpen = false;
                    e.Handled = true;
                }
            }
        }

        // New Point Group Creation
        private void NewPointGroupButton_Click(object sender, RoutedEventArgs e)
        {
            if (CadManager is null || CadManager.CogoPointManager is null) { return; }

            bool created = TryCreateNewPointGroup(
                CadManager.CogoPointManager.GetTempPointGroupName(),
                Colors.Black,
                CadManager.CogoPointManager.PointBaseScale);
        }
        private bool TryCreateNewPointGroup(string name, Color color, double scale)
        {
            if (CadManager is null || CadManager.CogoPointManager is null) { return false; }

            if (!CadManager.CogoPointManager.TryCreatePointGroup(name, color, out var pg) || pg == null)
            {
                return false;
            }

            pointGroupsListView.SelectedItem = pg;
            pointGroupsListView.UpdateLayout();
            pointGroupsListView.ScrollIntoView(pg);

            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                var container = pointGroupsListView.ItemContainerGenerator.ContainerFromItem(pg) as ListViewItem;
                if (container == null)
                {
                    // try again once more if virtualization delayed it
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        if (pointGroupsListView.ItemContainerGenerator.ContainerFromItem(pg) is ListViewItem li)
                        {
                            BeginPointGroupListCellEdit(li, "Name");
                        }
                    }));
                }
                else
                {
                    //PointGroupsNameStartRowEdit(container);
                    BeginPointGroupListCellEdit(container, "Name");
                }
            }));

            return true;
        }

        // Point Group Merging
        private void InitializeMergeCollectionView()
        {
            if (CadManager is null || CadManager.CogoPointManager is null || CadManager.CogoPointManager.PointGroups is null) { return; }
            AvailableMergePointGroups = CollectionViewSource.GetDefaultView(CadManager.CogoPointManager.PointGroups);
            AvailableMergePointGroups.Filter = FilterMergePoints;
        }
        private bool FilterMergePoints(object item)
        {
            if (item is PointGroup pg)
            {
                bool isSelected = _selectedPointGroups.Contains(pg);
                return !isSelected;
            }
            return false;
        }
        private void MergePointGroups_Click(object sender, RoutedEventArgs e)
        {
            if (MergePointGroup is null)
            {
                var binding = AvailableMergePointGroupsCBox.GetBindingExpression(ComboBox.SelectedValueProperty);
                if (binding != null)
                {
                    var error = new ValidationError(
                        new DataErrorValidationRule(),
                        binding,
                        "You must select a point group to merge to.",
                        null
                    );
                    Validation.MarkInvalid(binding, error);
                }
                return;
            }
            CadManager.CogoPointManager.MergePointGroups(_selectedPointGroups, MergePointGroup);
            CadManager.GroupedPointsView.Refresh();
        }
        private void AvailableMergePointGroupsCBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var binding = AvailableMergePointGroupsCBox.GetBindingExpression(ComboBox.SelectedItemProperty);
            if (binding != null)
            {
                var error = new ValidationError(
                    new DataErrorValidationRule(),
                    binding,
                    "You must select a point group to merge to.",
                    null
                );
                Validation.ClearInvalid(binding);
            }
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
