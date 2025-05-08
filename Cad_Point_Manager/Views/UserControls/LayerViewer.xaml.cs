using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.DrawingObjects3D;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Cad_Point_Manager.Views.UserControls
{
    /// <summary>
    /// Interaction logic for LayerViewer.xaml
    /// </summary>
    public partial class LayerViewer : UserControl, INotifyPropertyChanged
    {
        #region Fields
        private const double _panelHideTime = 200;

        private List<ObjectLayer3D> _selectedLayers = [];
        private bool _layerListVisible = true;
        private bool _pointGroupListVisible = true;
        private double _layerListOpacity = 0;
        private double _pointGroupListOpacity = 0;
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
            typeof(LayerViewer),
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
            typeof(LayerViewer),
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
            typeof(LayerViewer),
            new PropertyMetadata(null));
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        public LayerViewer()
        {
            InitializeComponent();

            HideControl();
        }

        private void overallGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            HideControl();
        }

        private void overallGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            ShowControl();
        }

        private void ShowControl()
        {
            // Animate the control sliding into view
            DoubleAnimation slideIn = new DoubleAnimation
            {
                From = 0, // Start at the width of the tab
                To = 1, // Fully visible
                Duration = TimeSpan.FromMilliseconds(_panelHideTime)
            };
            ScaleTransform transform = new ScaleTransform();
            mainPanel.RenderTransform = transform;
            transform.BeginAnimation(ScaleTransform.ScaleXProperty, slideIn);
        }

        private void HideControl()
        {
            LayerListVisible = false;
            PointGroupListVisible = false;

            // Animate the control sliding into view
            DoubleAnimation slideIn = new DoubleAnimation
            {
                From = 1, // Start at the width of the tab
                To = 0, // Fully visible
                Duration = TimeSpan.FromMilliseconds(_panelHideTime)
            };
            ScaleTransform transform = new ScaleTransform();
            mainPanel.RenderTransform = transform;
            transform.BeginAnimation(ScaleTransform.ScaleXProperty, slideIn);
        }


        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void layersListView_Loaded(object sender, RoutedEventArgs e)
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

        private void layersListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void layersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
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

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
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

        private void layersBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            PointGroupListVisible = false;
            LayerListVisible = true;

            layersListView.Focus();
            PointGroupListOpacity = 0;
            LayerListOpacity = 1;
        }

        private void layersBorder_MouseLeave(object sender, MouseEventArgs e)
        {
        }

        private void pointGroupsBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            LayerListVisible = false;
            PointGroupListVisible = true;

            pointGroupsListView.Focus();
            LayerListOpacity = 0;
            PointGroupListOpacity = 1;
        }

        private void pointGroupsBorder_MouseLeave(object sender, MouseEventArgs e)
        {
        }
    }
}
