using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.DrawingObjects3D;
using Cad_Point_Manager.Models.PointRendering;
using ColorPicker;
using SharpDX;
using System.Collections.Concurrent;
using System.ComponentModel;
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
    /// Interaction logic for RightHandPopout.xaml
    /// </summary>
    public partial class RightHandPopout : UserControl, INotifyPropertyChanged
    {
        #region Fields
        private const double _panelHideTime = 200;

        private readonly List<ObjectLayer3D> _selectedLayers = [];
        private readonly List<PointGroup> _selectedPointGroups = [];
        private readonly object _pendingRedrawLock = new();
        private readonly HashSet<CogoPoint> _pendingRedraw = new();
        private DispatcherOperation _redrawOp;
        private bool _ignorePointGroupSelectionChanged = false;

        private bool _layerListVisible = true;
        private double _layerListOpacity = 0;
        private bool _pointGroupListVisible = true;
        private double _pointGroupListOpacity = 0;
        private bool _pointGroupListColorPickerOpen = false;
        private string _newPointGroupName = "";
        private Vector4 _newPointGroupColor = new(0, 0, 0, 1);
        private double _newPointGroupScale = 1;
        private bool _newPointColorPickerToggleOpen = false;
        private ICollectionView _availableMergePointGroups;
        private PointGroup _mergePointGroup = null;

        private readonly DispatcherTimer _hideTimer = new();
        private bool _isMouseOverPanel = false;
        private ScaleTransform _mainPanelTransform = new();

        private bool _pointGroupBeingEdited = false;
        private string _previousPointGroupName = string.Empty;
        private string _previousPointGroupScale = string.Empty;
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
        public Vector4 NewPointGroupColor
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
            if (!_isMouseOverPanel && !PointGroupListColorPickerOpen && !NewPointColorPickerToggleOpen)
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
                layerListGridView.Columns[1].Width = layerListColumnWidth * 0.6;
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
            if (CadManager is not null)
            {
                CadManager.LineVerticesDirty = true;
                CadManager.TextVerticesDirty = true;
            }
        }
        private void LayerCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var layer in _selectedLayers)
            {
                layer.IsVisible = false;
            }
            if (CadManager is not null)
            {
                CadManager.LineVerticesDirty = true;
                CadManager.TextVerticesDirty = true;
            }
        }
        private void LayersBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            PointGroupListVisible = false;
            LayerListVisible = true;

            Validation.ClearInvalid(NewPointGroupScaleTextBox.GetBindingExpression(TextBox.TextProperty));
            PointGroupListOpacity = 0;
            LayerListOpacity = 1;
        }

        private void PointGroupsBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            LayerListVisible = false;
            PointGroupListVisible = true;

            LayerListOpacity = 0;
            PointGroupListOpacity = 1;
            ValidatePointGroupScale();
        }
        private void PointGroupsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listView = sender as ListView;
            if (listView is null) { return; }

            if (_ignorePointGroupSelectionChanged)
            {
                return;
            }

            _selectedPointGroups.Clear();
            var selectedItems = listView.SelectedItems;

            foreach (KeyValuePair<string, PointGroup> kvp in selectedItems)
            {
                _selectedPointGroups.Add(kvp.Value);
            }

            AvailableMergePointGroups.Refresh();
        }
        private void PointGroupsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var pg in _selectedPointGroups)
            {
                pg.IsVisible = true;
            }
            if (CadManager is not null)
            {
                CadManager.PointTextVerticesDirty = true;
                CadManager.PointCircleVerticesDirty = true;
            }
        }
        private void PointGroupsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var pg in _selectedPointGroups)
            {
                pg.IsVisible = false;
            }
            if (CadManager is not null)
            {
                CadManager.PointTextVerticesDirty = true;
                CadManager.PointCircleVerticesDirty = true;
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


        // Point Group Scale
        private void PointGroupScaleBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                if (sender is Border border &&
                    border.Child is TextBox textbox &&
                    textbox.DataContext is KeyValuePair<string, PointGroup> keyValuePair)
                {
                    var pg = keyValuePair.Value;
                    _pointGroupBeingEdited = true;
                    _previousPointGroupName = pg.Name;
                    textbox.IsReadOnly = false;

                    e.Handled = true;

                    textbox.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        textbox.Focus();
                        textbox.SelectAll();
                    }), DispatcherPriority.Input);
                }
            }

            // Swallow click event if _selectedPointGroups is more than 1 so that the selection isn't messed up.
            if (e.ClickCount == 1 &&
                _selectedPointGroups.Count > 1 &&
                sender is Border border2 &&
                border2.Child is TextBox textbox2 &&
                textbox2.DataContext is KeyValuePair<string, PointGroup> keyValuePair2)
            {
                if (_selectedPointGroups.Contains(keyValuePair2.Value))
                {
                    e.Handled = true;
                }
            }
        }
        private async void PointScaleTextbox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.DataContext is KeyValuePair<string, PointGroup> keyValuePair)
            {
                //var pg = keyValuePair.Value;
                //e.Handled = true;

                //if (_pointGroupBeingEdited)
                //{
                //    pg.Name = _previousPointGroupName;
                //    var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                //    binding?.UpdateTarget();
                //    _pointGroupBeingEdited = false;
                //}
                //textBox.IsReadOnly = true;

                e.Handled = true;

                var pg = keyValuePair.Value;
                var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                binding?.ValidateWithoutUpdate();
                var textBoxHasError = Validation.GetHasError(textBox);

                if (textBoxHasError)
                {
                    textBox.SelectAll();
                    return;
                }
                else
                {
                    binding?.UpdateSource();
                    List<CogoPoint> points = [];
                    foreach (var selectedPG in _selectedPointGroups)
                    {
                        selectedPG.PointScale = pg.PointScale;
                        points.AddRange(selectedPG.Points);
                    }

                    await QueueCogoPointRedrawAsync(points);
                    textBox.IsReadOnly = true;
                    _pointGroupBeingEdited = false;
                    CadManager.UpdateHitTestableObjectTree();
                    CadManager.LineVerticesDirty = true;
                }
            }
        }
        private async void PointScaleTextbox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender is TextBox textBox && textBox.DataContext is KeyValuePair<string, PointGroup> keyValuePair)
                {
                    e.Handled = true;

                    var pg = keyValuePair.Value;
                    var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                    binding?.ValidateWithoutUpdate();
                    var textBoxHasError = Validation.GetHasError(textBox);

                    if (textBoxHasError)
                    {
                        textBox.SelectAll();
                        return;
                    }
                    else
                    {
                        binding?.UpdateSource();
                        List<CogoPoint> points = [];
                        foreach (var selectedPG in _selectedPointGroups)
                        {
                            selectedPG.PointScale = pg.PointScale;
                            points.AddRange(selectedPG.Points);
                        }
                        
                        await QueueCogoPointRedrawAsync(points);
                        textBox.IsReadOnly = true;
                        _pointGroupBeingEdited = false;
                        CadManager.UpdateHitTestableObjectTree();
                        CadManager.LineVerticesDirty = true;

                        return;
                    }
                }
            }
            if (e.Key == Key.Escape)
            {
                if (sender is TextBox textBox && textBox.DataContext is PointGroup pg)
                {
                    e.Handled = true;

                    pg.Name = _previousPointGroupName;
                    var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                    binding?.UpdateTarget();
                    textBox.IsReadOnly = true;
                    _pointGroupBeingEdited = false;
                }
            }
        }
        private bool ValidatePointGroupScale()
        {
            var binding = NewPointGroupScaleTextBox.GetBindingExpression(TextBox.TextProperty);
            //binding?.UpdateSource();
            binding?.ValidateWithoutUpdate();
            return Validation.GetHasError(NewPointGroupScaleTextBox);
        }

        // Point Group Name
        private void PointGroupNameBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                if (sender is Border border && border.Child is TextBox textbox && textbox.DataContext is PointGroup pg)
                {
                    _pointGroupBeingEdited = true;
                    _previousPointGroupName = pg.Name;
                    e.Handled = true;
                    textbox.IsReadOnly = false;
                    textbox.Focus();

                    textbox.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        textbox.SelectAll();
                    }), DispatcherPriority.Input);
                }
            }
        }
        private void PointGroupNameTextbox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            if (textBox != null && textBox.DataContext is PointGroup pg)
            {
                e.Handled = true;

                if (_pointGroupBeingEdited)
                {
                    pg.Name = _previousPointGroupName;
                    var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                    binding?.UpdateTarget();
                    _pointGroupBeingEdited = false;
                }
                textBox.IsReadOnly = true;
            }
        }
        private void PointGroupNameTextbox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender is TextBox textBox && textBox.DataContext is PointGroup pg)
                {
                    e.Handled = true;

                    var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                    binding?.ValidateWithoutUpdate();
                    var textBoxHasError = Validation.GetHasError(textBox);

                    if (textBoxHasError)
                    {
                        textBox.SelectAll();
                        return;
                    }
                    else
                    {
                        binding?.UpdateSource();
                        textBox.IsReadOnly = true;
                        _pointGroupBeingEdited = false;
                        return;
                    }
                }
            }
            if (e.Key == Key.Escape)
            {
                if (sender is TextBox textBox && textBox.DataContext is PointGroup pg)
                {
                    e.Handled = true;

                    pg.Name = _previousPointGroupName;
                    var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                    binding?.UpdateTarget();
                    textBox.IsReadOnly = true;
                    _pointGroupBeingEdited = false;
                }
            }
        }

        private void NewPointGroupButton_Click(object sender, RoutedEventArgs e)
        {
            if (NewPointGroupName is null || NewPointGroupScale <= 0) { return; }

            string name = NewPointGroupName.Trim();
            double scale = NewPointGroupScale;
            Vector4 color = NewPointGroupColor;

            bool nameHasError = !CadManager.CogoPointManager.IsValidPointGroupName(NewPointGroupName, out string errorMessage);
            bool scaleHasError = ValidatePointGroupScale();

            if (nameHasError || scaleHasError)
            {
                if (nameHasError)
                {
                    var binding = NewPointGroupNameTextBox.GetBindingExpression(TextBox.TextProperty);
                    if (binding != null)
                    {
                        var error = new ValidationError(
                            new DataErrorValidationRule(),
                            binding,
                            errorMessage,
                            null
                        );
                        Validation.MarkInvalid(binding, error);
                    }
                    return;
                }
            }
            else
            {
                CadManager.CogoPointManager.TryCreatePointGroup(name, color, scale, out var pg);
                ResetCreatePointGroup();
            }
        }

        private void CancelNewPointGroupButton_Click(object sender, RoutedEventArgs e)
        {
            ResetCreatePointGroup();
        }
        private void ResetCreatePointGroup()
        {
            NewPointGroupName = string.Empty;
            Validation.ClearInvalid(NewPointGroupNameTextBox.GetBindingExpression(TextBox.TextProperty));
            Validation.ClearInvalid(NewPointGroupScaleTextBox.GetBindingExpression(TextBox.TextProperty));
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
        private void PortableColorPicker_IsPopupOpenChanged(object sender, bool isOpen)
        {
            if (isOpen) { return; }

            PortableColorPicker colorpicker = sender as PortableColorPicker;
            if (colorpicker is not null)
            {
                ConcurrentBag<CogoPoint> pointsToUpdate = [];
                var color = colorpicker.SelectedColor;
                foreach (var pg in _selectedPointGroups)
                {
                    pg.Color = new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, 1.0f);
                    pg.UpdateWindowsColor();
                }

                QueueCogoPointRedraw(SelectedCogoPoints);
            }
        }

        public void QueueCogoPointRedraw(IEnumerable<CogoPoint> points)
        {
            if (points is null) return;

            lock (_pendingRedrawLock)
            {
                foreach (var p in points) { _pendingRedraw.Add(p); }

                if (_redrawOp == null ||
                    _redrawOp.Status == DispatcherOperationStatus.Completed ||
                    _redrawOp.Status == DispatcherOperationStatus.Aborted)
                {
                    _redrawOp = Dispatcher.InvokeAsync(() =>
                    {
                        List<CogoPoint> batch;
                        lock (_pendingRedrawLock)
                        {
                            if (_pendingRedraw.Count == 0) return;
                            batch = _pendingRedraw.ToList();
                            _pendingRedraw.Clear();
                        }

                        using (Dispatcher.CurrentDispatcher.DisableProcessing())
                        {
                            for (int i = 0; i < batch.Count; i++)
                                batch[i].RedrawAllVisuals();
                        }
                    }, DispatcherPriority.Render);
                }
            }
        }
        // Change the signature to return a Task you can await.
        public Task QueueCogoPointRedrawAsync(IEnumerable<CogoPoint> points)
        {
            if (points is null) return Task.CompletedTask;

            lock (_pendingRedrawLock)
            {
                foreach (var p in points) _pendingRedraw.Add(p);

                // If a batch is already queued/running, await that same operation.
                if (_redrawOp is not null &&
                    (_redrawOp.Status == DispatcherOperationStatus.Pending ||
                     _redrawOp.Status == DispatcherOperationStatus.Executing))
                {
                    return _redrawOp.Task; // awaitable
                }

                // Queue a new batch.
                _redrawOp = Dispatcher.InvokeAsync(() =>
                {
                    List<CogoPoint> batch;
                    lock (_pendingRedrawLock)
                    {
                        if (_pendingRedraw.Count == 0) return; // nothing to do
                        batch = _pendingRedraw.ToList();
                        _pendingRedraw.Clear();
                    }

                    using (Dispatcher.CurrentDispatcher.DisableProcessing())
                    {
                        for (int i = 0; i < batch.Count; i++)
                            batch[i].RedrawAllVisuals();
                    }
                }, DispatcherPriority.Render);

                return _redrawOp.Task; // awaitable
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
