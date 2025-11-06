using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.DrawingObjects3D;
using Cad_Point_Manager.Models.PointRendering;
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

        private readonly List<ObjectLayer3D> _selectedLayers = [];
        private readonly List<PointGroup> _selectedPointGroups = [];

        private bool _layerListVisible = true;
        private double _layerListOpacity = 0;
        private bool _layerListColorPickerOpen = false;
        private bool _pointGroupListVisible = true;
        private double _pointGroupListOpacity = 0;
        private bool _pointGroupListColorPickerOpen = false;

        private string _newPointGroupName = "";
        private Color _newPointGroupColor = Colors.Black;
        private double _newPointGroupScale = 1;
        private bool _newPointColorPickerToggleOpen = false;
        private ICollectionView _availableMergePointGroups;
        private PointGroup _mergePointGroup = null;

        private bool _pointGroupNameBeingEdited = false;
        private bool _pointGroupScaleBeingEdited = false;
        private string _previousPointGroupName = string.Empty;
        private double _previousPointGroupScale = 1;
        private PointGroup _editPointGroup;
        private bool _newPointGroupBeingEdited = false;
        private PointGroup _newPointGroup;

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
        public string NewPointGroupName
        {
            get { return _newPointGroupName; }
            set
            {
                _newPointGroupName = value;
                OnPropertyChanged(nameof(NewPointGroupName));
            }
        }
        public Color NewPointGroupColor
        {
            get { return _newPointGroupColor; }
            set
            {
                _newPointGroupColor = value;
                OnPropertyChanged(nameof(NewPointGroupColor));
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

        public CadManager3D CadManager
        {
            get { return (CadManager3D)GetValue(CadManagerProperty); }
            set { SetValue(CadManagerProperty, value); }
        }
        public static readonly DependencyProperty CadManagerProperty =
        DependencyProperty.Register(
            nameof(CadManager),
            typeof(CadManager3D),
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

            pointGroupsListView.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent,
                new MouseButtonEventHandler(PointGroupsListView_PreviewMouseLeftButtonDown), handledEventsToo: true);
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
            if (!_isMouseOverPanel && !PointGroupListColorPickerOpen && !NewPointColorPickerToggleOpen && !LayerListColorPickerOpen)
            {
                HideControl();
            }
        }

        private void OverallGrid_MouseLeave(object sender, MouseEventArgs e)
        {
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
            LayerListVisible = false;
            PointGroupListVisible = false;

            DoubleAnimation slideOut = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(_panelHideTime),
                FillBehavior = FillBehavior.HoldEnd
            };
            _mainPanelTransform.BeginAnimation(ScaleTransform.ScaleXProperty, slideOut);
        }

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
                layerListGridView.Columns[0].Width = layerListColumnWidth * 1.4;
                layerListGridView.Columns[1].Width = layerListColumnWidth * 1.0;
                layerListGridView.Columns[2].Width = layerListColumnWidth * 0.6;
            }
        }
        private void LayersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedLayers.Clear();
            var selectedItems = (sender as ListView).SelectedItems;

            foreach (var selectedItem in selectedItems)
            {
                if (selectedItem is KeyValuePair<string, ObjectLayer3D> selectedLayer)
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
        private void LayersBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            PointGroupListVisible = false;
            LayerListVisible = true;
            PointGroupListOpacity = 0;
            LayerListOpacity = 1;
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

        private void PointGroupsBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            LayerListVisible = false;
            PointGroupListVisible = true;
            LayerListOpacity = 0;
            PointGroupListOpacity = 1;
        }
        private void PointGroupsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListView listView) { return; }

            _selectedPointGroups.Clear();
            var selectedItems = listView.SelectedItems;

            foreach (PointGroup pg in selectedItems)
            {
                _selectedPointGroups.Add(pg);
            }

            AvailableMergePointGroups.Refresh();
        }
        private void PointGroupsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var pg in _selectedPointGroups)
            {
                pg.IsVisible = true;
            }
        }
        private void PointGroupsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var pg in _selectedPointGroups)
            {
                pg.IsVisible = false;
            }
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
                pointGroupGridView.Columns[0].Width = pointGroupColumnWidth * 1;
                pointGroupGridView.Columns[1].Width = pointGroupColumnWidth * 1;
                pointGroupGridView.Columns[2].Width = pointGroupColumnWidth * 1;
                pointGroupGridView.Columns[3].Width = pointGroupColumnWidth * 1;
            }
        }
        private void PointGroupsListView_PreviewKeyDownOrUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                return;
            }
        }
        private void PointGroupsListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //var origin = (DependencyObject)e.OriginalSource;
            //var item = ItemsControl.ContainerFromElement(pointGroupsListView, origin) as ListViewItem;
            //if (item == null) return;

            //if (!item.IsSelected)
            //{
            //    if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == ModifierKeys.None)
            //        pointGroupsListView.SelectedItems.Clear();

            //    item.IsSelected = true;
            //    item.Focus();
            //}
        }

        // New Point Group Creation
        private void NewPointGroupButton_Click(object sender, RoutedEventArgs e)
        {
            if (CadManager is null || CadManager.CogoPointManager is null) { return; }

            var tempName = CadManager.CogoPointManager.GetTempPointGroupName();
            var color = Colors.Black;
            double scale = CadManager?.PointBaseScale ?? 1.0;

            if (!CadManager.CogoPointManager.TryCreatePointGroup(tempName, color, out var pg) || pg == null)
                return;

            pg = CadManager.CogoPointManager.PointGroups.LastOrDefault(p => p.Name.Equals(tempName, StringComparison.OrdinalIgnoreCase));
            if (pg.Equals(default(KeyValuePair<string, PointGroup>))) return;

            _newPointGroupBeingEdited = true;
            _newPointGroup = pg;
            _previousPointGroupName = pg.Name;

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
                        if (pointGroupsListView.ItemContainerGenerator.ContainerFromItem(pg) is ListViewItem li) { StartRowEdit(li); }
                    }));
                }
                else
                {
                    StartRowEdit(container);
                }
            }));
        }
        private void StartRowEdit(ListViewItem row)
        {
            // Name TextBox
            var nameTb = VisualTreeHelpers.FindByName(row, "PointGroupNameTextBox") as TextBox;
            if (nameTb != null)
            {
                nameTb.IsReadOnly = false;
                nameTb.Focus();
                nameTb.SelectAll();
            }
        }

        // Point Group Scale
        private void PointGroupScaleBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border &&
                    border.Child is TextBox textbox &&
                    textbox.DataContext is PointGroup pg)
            {
                if (e.ClickCount > 1)
                {
                    _pointGroupScaleBeingEdited = true;
                    _previousPointGroupScale = pg.PointScale;
                    _editPointGroup = pg;
                    textbox.IsReadOnly = false;

                    e.Handled = true;

                    textbox.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        textbox.Focus();
                        textbox.SelectAll();
                    }), DispatcherPriority.Input);
                }
                else
                {
                    // Swallow click event if _selectedPointGroups is more than 1 so that the selection isn't messed up.
                    if (_selectedPointGroups.Contains(pg)) { e.Handled = true; }
                }
            }
        }
        private void PointScaleTextbox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.DataContext is PointGroup)
            {
                e.Handled = true;

                if (_pointGroupScaleBeingEdited)
                {
                    EndPointGroupScaleEditMode(textBox);
                    _pointGroupScaleBeingEdited = false;
                    _editPointGroup = null;
                }
                textBox.IsReadOnly = true;
            }
        }
        private void PointScaleTextbox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender is TextBox textBox && textBox.DataContext is PointGroup)
                {
                    e.Handled = true;

                    if (_pointGroupScaleBeingEdited)
                    {
                        bool isValid = EndPointGroupScaleEditMode(textBox);

                        if (isValid)
                        {
                            foreach (var pg in _selectedPointGroups)
                            {
                                pg.PointScale = _editPointGroup.PointScale;
                            }
                            RefreshPointGroupTextBoxes("PointScaleTextbox", _selectedPointGroups);
                        }

                        _pointGroupScaleBeingEdited = false;
                        _editPointGroup = null;
                    }
                    textBox.IsReadOnly = true;
                }
            }
            if (e.Key == Key.Escape)
            {
                if (sender is TextBox textbox && textbox.DataContext is PointGroup)
                {
                    e.Handled = true;

                    if (_pointGroupScaleBeingEdited)
                    {
                        _editPointGroup.PointScale = _previousPointGroupScale;
                        _pointGroupScaleBeingEdited = false;
                        var binding = textbox.GetBindingExpression(TextBox.TextProperty);
                        binding.UpdateTarget();
                        _editPointGroup = null;
                    }
                    textbox.IsReadOnly = true;
                }
            }
        }
        private bool EndPointGroupScaleEditMode(TextBox textBox)
        {
            if (textBox.DataContext is PointGroup)
            {
                bool isValid = CadManager.CogoPointManager.IsValidPointScale(textBox.Text, out string errorMessage);
                var binding = textBox.GetBindingExpression(TextBox.TextProperty);

                if (!isValid)
                {
                    binding.UpdateTarget();
                }
                else
                {
                    binding?.UpdateSource();
                }

                textBox.IsReadOnly = true;
                return isValid;
            }
            return false;
        }

        // Point Group Name
        private void PointGroupNameBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            if (sender is Border border &&
                   border.Child is TextBox textbox && textbox.DataContext is PointGroup pg)
            {
                if (e.ClickCount > 1)
                {
                    _pointGroupNameBeingEdited = true;
                    _previousPointGroupName = pg.Name;
                    _editPointGroup = pg;
                    e.Handled = true;
                    textbox.IsReadOnly = false;
                    textbox.Focus();

                    textbox.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        textbox.SelectAll();
                    }), DispatcherPriority.Input);
                }
                //else
                //{
                //    // Swallow click event if _selectedPointGroups is more than 1 so that the selection isn't messed up.
                //    if (_selectedPointGroups.Contains(pg))
                //    {
                //        e.Handled = true;
                //    }
                //}
            }
        }
        private void PointGroupNameTextbox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.DataContext is PointGroup)
            {
                e.Handled = true;

                if (_newPointGroupBeingEdited)
                {
                    EndPointGroupNameEditMode(textBox);
                    _newPointGroupBeingEdited = false;
                    _newPointGroup = null;
                }
                if (_pointGroupNameBeingEdited)
                {
                    EndPointGroupNameEditMode(textBox);
                    _pointGroupNameBeingEdited = false;
                    _editPointGroup = null;
                }
                textBox.IsReadOnly = true;
            }
        }
        private void PointGroupNameTextbox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender is TextBox textBox && textBox.DataContext is PointGroup pg)
                {
                    e.Handled = true;

                    if (_newPointGroupBeingEdited)
                    {
                        EndPointGroupNameEditMode(textBox);
                        _newPointGroupBeingEdited = false;
                        _newPointGroup = null;
                    }
                    if (_pointGroupNameBeingEdited)
                    {
                        EndPointGroupNameEditMode(textBox);
                        _pointGroupNameBeingEdited = false;
                        _editPointGroup = null;
                    }
                    textBox.IsReadOnly = true;
                }
            }
            if (e.Key == Key.Escape)
            {
                if (sender is TextBox textBox && textBox.DataContext is PointGroup)
                {
                    e.Handled = true;

                    if (_newPointGroupBeingEdited)
                    {
                        CadManager.CogoPointManager.DeletePointGroup(_newPointGroup);
                        _newPointGroupBeingEdited = false;
                        var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                        binding.UpdateTarget();
                        _newPointGroup = null;
                    }
                    if (_pointGroupNameBeingEdited)
                    {
                        _editPointGroup.Name = _previousPointGroupName;
                        _pointGroupNameBeingEdited = false;
                        var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                        binding.UpdateTarget();
                        _editPointGroup = null;
                    }
                    textBox.IsReadOnly = true;
                }
            }
        }
        private void EndPointGroupNameEditMode(TextBox textBox)
        {
            if (textBox.DataContext is PointGroup)
            {
                var binding = textBox.GetBindingExpression(TextBox.TextProperty);

                if (!CadManager.CogoPointManager.IsValidPointGroupName(textBox.Text, out string errorMessage))
                {
                    binding.UpdateTarget();
                }
                else
                {
                    binding?.UpdateSource();
                }

                textBox.IsReadOnly = true;
            }
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
            if (item is KeyValuePair<string, PointGroup> keyValuePair)
            {
                bool isSelected = _selectedPointGroups.Contains(keyValuePair.Value);
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
            CadManager.PointsView.Refresh();
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

        // Point Group Color Picker
        private void PointGroupsPortableColorPicker_IsPopupOpenChanged(object sender, bool isOpen)
        {
            if (isOpen) { return; }

            PortableColorPicker colorpicker = sender as PortableColorPicker;
            if (colorpicker is not null)
            {
                var color = colorpicker.SelectedColor;
                foreach (var pg in _selectedPointGroups)
                {
                    pg.Color = color;
                }
            }
        }

        private void RefreshPointGroupTextBoxes(string textBoxName, IEnumerable<PointGroup> groups)
        {
            foreach (var pg in groups)
            {
                var lvi = pointGroupsListView.ItemContainerGenerator.ContainerFromItem(pg) as ListViewItem;
                if (lvi == null) continue; // not realized (virtualized) -> binding will update next time it's realized

                // You already use a helper like this elsewhere
                var tb = VisualTreeHelpers.FindByName(lvi, textBoxName) as TextBox;
                tb?.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
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
