
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

        private readonly List<DxfPoint> _selectedPoints = [];
        private bool _pointsListVisible = true;
        private double _pointsListOpacity = 0;

        private readonly DispatcherTimer _hideTimer = new();
        private bool _isMouseOverPanel = false;
        private ScaleTransform _mainPanelTransform = new();
        #endregion

        #region Properties
        public bool PointsListVisible
        {
            get { return _pointsListVisible; }
            set
            {
                _pointsListVisible = value;
                OnPropertyChanged(nameof(PointsListVisible));
            }
        }
        public double PointsListOpacity
        {
            get { return _pointsListOpacity; }
            set
            {
                _pointsListOpacity = value;
                OnPropertyChanged(nameof(PointsListOpacity));
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

        public CadManager3D CadManager
        {
            get { return (CadManager3D)GetValue(CadManagerProperty); }
            set { SetValue(CadManagerProperty, value); }
        }

        public static readonly DependencyProperty CadManagerProperty =
        DependencyProperty.Register(
            nameof(CadManager),
            typeof(CadManager3D),
            typeof(LeftHandPopout),
            new PropertyMetadata(null));

        public ICollectionView PointsCollectionView
        {
            get { return (ICollectionView)GetValue(PointsCollectionViewProperty); }
            set { SetValue(PointsCollectionViewProperty, value); }
        }

        public static readonly DependencyProperty PointsCollectionViewProperty =
        DependencyProperty.Register(
            nameof(PointsCollectionView),
            typeof(ICollectionView),
            typeof(LeftHandPopout),
            new PropertyMetadata(null));
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        public LeftHandPopout()
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
            if (!_isMouseOverPanel)
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
            PointsListVisible = false;

            DoubleAnimation slideOut = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(_panelHideTime),
                FillBehavior = FillBehavior.HoldEnd
            };
            _mainPanelTransform.BeginAnimation(ScaleTransform.ScaleXProperty, slideOut);
        }

        private void PointsListView_Loaded(object sender, RoutedEventArgs e)
        {
            ListView listview = sender as ListView;

            // Set column widths on each gridview
            GridView pointsGridView = listview.View as GridView;
            double pointsListTotalWidth = listview.ActualWidth;
            double pointsListColumnWidth = pointsListTotalWidth / pointsGridView.Columns.Count;
            if (pointsListColumnWidth > 0)
            {
                pointsGridView.Columns[0].Width = pointsListColumnWidth * 0.75;
                pointsGridView.Columns[1].Width = pointsListColumnWidth * 1.0;
                pointsGridView.Columns[2].Width = pointsListColumnWidth * 1.0;
                pointsGridView.Columns[3].Width = pointsListColumnWidth * 1.0;
                pointsGridView.Columns[4].Width = pointsListColumnWidth * 1.25;
            }
        }
        private void PointsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedPoints.Clear();
            var selectedItems = (sender as ListView).SelectedItems;

            foreach (var selectedItem in selectedItems)
            {
                if (selectedItem is KeyValuePair<string, DxfPoint> selectedPoint)
                {
                    if (selectedPoint.Value is not null)
                    {
                        _selectedPoints.Add(selectedPoint.Value);
                    }
                }
            }
        }
        private void PointsBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            PointsListVisible = true;
            PointsListOpacity = 1;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                if (sender is Border border && border.Child is TextBox textbox)
                {
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

        private void PointNumberTextbox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null)
            {
                //var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                //binding?.UpdateTarget();
                textBox.IsReadOnly = true;

                //textBox.IsReadOnly = true;
                //if (Validation.GetHasError(textBox))
                //{
                //    binding?.UpdateTarget();
                //}
            }
        }
        private void PointNumberTextbox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender is TextBox textBox && textBox.DataContext is DxfPoint point)
                {
                    e.Handled = true;

                    point.ClearErrors(nameof(point.PointNumber));
                    var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                    binding?.ValidateWithoutUpdate();
                    //binding?.UpdateSource();

                    var textBoxHasError = Validation.GetHasError(textBox);

                    if (textBoxHasError)
                    {
                        return;
                    }
                    else
                    {
                        binding?.UpdateSource();
                        var pointNumberHasError = point.HasPointNumberError;

                        if (pointNumberHasError) { return; }
                        else
                        {
                            binding?.UpdateTarget();
                            textBox.IsReadOnly = true;
                            return;
                        }
                    }
                }
            }
                if (e.Key == Key.Escape)
            {

            }
        }
    }
}
