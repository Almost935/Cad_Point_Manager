using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.DrawingObjects3D;
using Cad_Point_Manager.Models.PointRendering;
using ColorPicker;
using SharpDX;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

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
        private bool _pointGroupListVisible = true;
        private double _layerListOpacity = 0;
        private double _pointGroupListOpacity = 0;

        private readonly DispatcherTimer _hideTimer = new();
        private bool _isMouseOverPanel = false;
        private ScaleTransform _mainPanelTransform = new();
        private bool _isColorPickerOpen;
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
            new PropertyMetadata(null));

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

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        public RightHandPopout()
        {
            InitializeComponent();

            mainPanel.RenderTransform = _mainPanelTransform;

            HideControl();

            _hideTimer.Interval = TimeSpan.FromSeconds(1);
            _hideTimer.Tick += HideTimer_Tick;
        }

        private void HideTimer_Tick(object sender, EventArgs e)
        {
            _hideTimer.Stop();
            if (!_isMouseOverPanel && !_isColorPickerOpen)
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

            //layersListView.Focus();
            PointGroupListOpacity = 0;
            LayerListOpacity = 1;
        }

        private void PointGroupsBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            LayerListVisible = false;
            PointGroupListVisible = true;

            //pointGroupsListView.Focus();
            LayerListOpacity = 0;
            PointGroupListOpacity = 1;
        }
        private void PointGroupsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedPointGroups.Clear();
            var selectedItems = (sender as ListView).SelectedItems;

            foreach (PointGroup pointGroup in selectedItems)
            {
                if (pointGroup is not null)
                {
                    _selectedPointGroups.Add(pointGroup);
                }
            }
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
                pointGroupGridView.Columns[0].Width = pointGroupColumnWidth * 1.6;
                pointGroupGridView.Columns[1].Width = pointGroupColumnWidth * 0.7;
                pointGroupGridView.Columns[1].Width = pointGroupColumnWidth * 0.7;
            }
        }

        private void UpdateIsColorPickerOpen()
        {
            _isColorPickerOpen = false;
            foreach (var pg in PointGroupCollectionView)
            {
                if (pg is PointGroup pointGroup)
                {
                    if (pointGroup.ColorToggleOpen)
                    {
                        _isColorPickerOpen = true;
                        return;
                    }
                }
            }
        }
        private void ColorPicker_IsPopupOpenChanged(object? sender, bool e)
        {
            if (sender is PortableColorPicker colorPicker)
            {
                UpdateIsColorPickerOpen();

                if (!colorPicker.IsPopupOpen)
                {
                    var binding = colorPicker.GetBindingExpression(PortableColorPicker.SelectedColorProperty);
                    binding?.UpdateSource();
                    CadManager.PointTextVerticesDirty = true;
                }
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
