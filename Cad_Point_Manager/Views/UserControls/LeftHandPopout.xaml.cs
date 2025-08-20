
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.PointRendering;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using TextBox = System.Windows.Controls.TextBox;
using Cad_Point_Manager.ViewModels;
using System.Collections.Specialized;
using Cad_Point_Manager.Views.ValidationRules;
using System.Windows.Data;
using Cad_Point_Manager.Services;

namespace Cad_Point_Manager.Views.UserControls
{
    /// <summary>
    /// Interaction logic for LeftHandPopout.xaml
    /// </summary>
    public partial class LeftHandPopout : UserControl, INotifyPropertyChanged
    {
        #region Fields
        private const double _panelHideTime = 200;

        private readonly DispatcherTimer _hideTimer = new();
        private bool _isMouseOverPanel = false;
        private ScaleTransform _mainPanelTransform = new();
        private ValidationService _validationService = new();

        // DxfPoint editing fields
        private int _previousPointNumber;
        private double _previousPointNorthing;
        private double _previousPointEasting;
        private double _previousPointElevation;
        private string _previousPointDescription;
        private bool _pointBeingEdited = false;

        private readonly List<CogoPoint> _selectedPoints = [];
        private bool _pointsTabVisible = true;
        private double _pointsTabOpacity = 0;
        private bool _propertiesTabVisible = true;
        private double _propertiesTabOpacity = 0;
        private CogoPointSelectionViewModel _cogoPointSelectionViewModel;
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
            get { return _propertiesTabOpacity; }
            set
            {
                _propertiesTabOpacity = value;
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
            new PropertyMetadata(null, OnCadManagerChanged));

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

        public static readonly DependencyProperty SelectedCogoPointsProperty =
            DependencyProperty.Register(
            nameof(SelectedCogoPoints),
            typeof(ObservableCollection<CogoPoint>),
            typeof(LeftHandPopout),
            new FrameworkPropertyMetadata(
                new ObservableCollection<CogoPoint>(),
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedCogoPointsChanged));
        public ObservableCollection<CogoPoint> SelectedCogoPoints
        {
            get => (ObservableCollection<CogoPoint>)GetValue(SelectedCogoPointsProperty);
            set
            {
                SetValue(SelectedCogoPointsProperty, value);
                CogoPointSelectionViewModel?.Refresh();
            }
        }
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

        #region Methods
        private static void OnCadManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LeftHandPopout control && e.NewValue is not null && e.NewValue is CadManager3D cadManager)
            {

                control.CogoPointSelectionViewModel?.UpdateCadManager(cadManager);
                control.CogoPointSelectionViewModel?.Refresh();

                // Subscribe to PointGroups collection changes
                if (cadManager?.CogoPointManager?.PointGroups is ObservableCollection<KeyValuePair<string, PointGroup>> pg)
                {
                    pg.CollectionChanged += control.PointGroups_CollectionChanged;
                }
            }
        }
        private void PointGroups_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            CogoPointSelectionViewModel?.UpdateDisplayedPointGroups();
        }

        private void SelectedCogoPoints_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            CogoPointSelectionViewModel?.Refresh();
        }
        private static void OnSelectedCogoPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LeftHandPopout control)
            {
                if (e.OldValue is ObservableCollection<CogoPoint> oldCollection)
                {
                    oldCollection.CollectionChanged -= control.SelectedCogoPoints_CollectionChanged;
                }

                if (e.NewValue is ObservableCollection<CogoPoint> newCollection)
                {
                    newCollection.CollectionChanged += control.SelectedCogoPoints_CollectionChanged;
                    if (control.CogoPointSelectionViewModel is null)
                    {
                        control.CogoPointSelectionViewModel = new(control.CadManager, newCollection);
                    }
                    control.CogoPointSelectionViewModel.SelectedPoints = newCollection;
                }

                control.CogoPointSelectionViewModel?.Refresh();
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
            PointsTabVisible = false;

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
                if (selectedItem is KeyValuePair<string, CogoPoint> selectedPoint)
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
            PointsTabVisible = true;
            PointsTabOpacity = 1;
            PropertiesTabVisible = false;
            PropertiesTabOpacity = 0;
        }

        private void PropertiesBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            PointsTabVisible = false;
            PointsTabOpacity = 0;
            PropertiesTabVisible = true;
            PropertiesTabOpacity = 1;
        }

        // Point Number Editing
        private void PointNumberBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                if (sender is Border border && border.Child is TextBox textbox && textbox.DataContext is CogoPoint point)
                {
                    _pointBeingEdited = true;
                    _previousPointNumber = point.PointNumber;
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

            if (textBox != null && textBox.DataContext is CogoPoint point)
            {
                e.Handled = true;

                if (_pointBeingEdited)
                {
                    point.PointNumber = _previousPointNumber;
                    var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                    binding?.UpdateTarget();
                    _pointBeingEdited = false;
                }
                textBox.IsReadOnly = true;
            }
        }
        private void PointNumberTextbox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender is TextBox textBox && textBox.DataContext is CogoPoint point)
                {
                    e.Handled = true;

                    point.ClearErrors(nameof(point.PointNumber));
                    var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                    Validation.ClearInvalid(binding);
                    binding?.ValidateWithoutUpdate();
                    var textBoxHasError = Validation.GetHasError(textBox);

                    if (textBoxHasError)
                    {
                        textBox.SelectAll();
                        return;
                    }
                    else
                    {
                        var parsable = Int32.TryParse(textBox.Text, out var newPointNum);
                        if (parsable)
                        {
                            var pointNumExists = CadManager.CogoPointManager.PointExists(newPointNum);
                            if (pointNumExists)
                            {
                                var bindingExpr = textBox.GetBindingExpression(TextBox.TextProperty);
                                if (bindingExpr != null)
                                {
                                    var error = new ValidationError(
                                        new DataErrorValidationRule(),
                                        bindingExpr,
                                        "Point number already exists",
                                        null);

                                    Validation.MarkInvalid(bindingExpr, error);
                                }
                                textBox.SelectAll();
                                return;
                            }
                        }

                        binding?.UpdateSource();
                        var pointNumberHasError = point.HasPointNumberError;

                        if (pointNumberHasError)
                        {
                            textBox.SelectAll();
                            return;
                        }
                        else
                        {
                            binding?.UpdateTarget();
                            textBox.IsReadOnly = true;
                            _pointBeingEdited = false;
                            return;
                        }
                    }
                }
            }
            if (e.Key == Key.Escape)
            {
                if (sender is TextBox textBox && textBox.DataContext is CogoPoint point)
                {
                    e.Handled = true;
                    point.PointNumber = _previousPointNumber;
                    var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                    binding?.UpdateTarget();
                    textBox.IsReadOnly = true;
                    _pointBeingEdited = false;
                }
            }
        }

        // Point Northing Editing
        private void PointNorthingBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                if (sender is Border border && border.Child is TextBox textbox && textbox.DataContext is CogoPoint point)
                {
                    _pointBeingEdited = true;
                    _previousPointNorthing = point.Northing;
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
        private void PointNorthingTextbox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            if (textBox != null && textBox.DataContext is CogoPoint point)
            {
                e.Handled = true;

                if (_pointBeingEdited)
                {
                    point.Northing = _previousPointNorthing;
                    var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                    binding?.UpdateTarget();
                    _pointBeingEdited = false;
                }
                textBox.IsReadOnly = true;
            }
        }
        private void PointNorthingTextbox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender is TextBox textBox && textBox.DataContext is CogoPoint point)
                {
                    e.Handled = true;

                    point.ClearErrors(nameof(point.Northing));
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
                        _pointBeingEdited = false;
                        return;
                    }
                }
            }
            if (e.Key == Key.Escape)
            {
                if (sender is TextBox textBox && textBox.DataContext is CogoPoint point)
                {
                    e.Handled = true;
                    point.Northing = _previousPointNorthing;
                    var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                    binding?.UpdateTarget();
                    textBox.IsReadOnly = true;
                    _pointBeingEdited = false;
                }
            }
        }

        // Point Easting Editing
        private void PointEastingBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                if (sender is Border border && border.Child is TextBox textbox && textbox.DataContext is CogoPoint point)
                {
                    _pointBeingEdited = true;
                    _previousPointEasting = point.Easting;
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
        private void PointEastingTextbox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            if (textBox != null && textBox.DataContext is CogoPoint point)
            {
                e.Handled = true;

                if (_pointBeingEdited)
                {
                    point.Easting = _previousPointEasting;
                    var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                    binding?.UpdateTarget();
                    _pointBeingEdited = false;
                }
                textBox.IsReadOnly = true;
            }
        }
        private void PointEastingTextbox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender is TextBox textBox && textBox.DataContext is CogoPoint point)
                {
                    e.Handled = true;

                    point.ClearErrors(nameof(point.Easting));
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
                        _pointBeingEdited = false;
                        return;
                    }
                }
            }
            if (e.Key == Key.Escape)
            {
                if (sender is TextBox textBox && textBox.DataContext is CogoPoint point)
                {
                    e.Handled = true;
                    point.Easting = _previousPointEasting;
                    var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                    binding?.UpdateTarget();
                    textBox.IsReadOnly = true;
                    _pointBeingEdited = false;
                }
            }
        }

        // Point Elevation Editing
        private void PointElevationBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                if (sender is Border border && border.Child is TextBox textbox && textbox.DataContext is CogoPoint point)
                {
                    _pointBeingEdited = true;
                    _previousPointElevation = point.Elevation;
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
        private void PointElevationTextbox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            if (textBox != null && textBox.DataContext is CogoPoint point)
            {
                e.Handled = true;

                if (_pointBeingEdited)
                {
                    point.Elevation = _previousPointElevation;
                    var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                    binding?.UpdateTarget();
                    _pointBeingEdited = false;
                }
                textBox.IsReadOnly = true;
            }
        }
        private void PointElevationTextbox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender is TextBox textBox && textBox.DataContext is CogoPoint point)
                {
                    e.Handled = true;

                    point.ClearErrors(nameof(point.Elevation));
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
                        _pointBeingEdited = false;
                        return;
                    }
                }
            }
            if (e.Key == Key.Escape)
            {
                if (sender is TextBox textBox && textBox.DataContext is CogoPoint point)
                {
                    e.Handled = true;
                    point.Elevation = _previousPointElevation;
                    var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                    binding?.UpdateTarget();
                    textBox.IsReadOnly = true;
                    _pointBeingEdited = false;
                }
            }
        }

        // Point Description Editing
        private void PointDescriptionBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                if (sender is Border border && border.Child is TextBox textbox && textbox.DataContext is CogoPoint point)
                {
                    _pointBeingEdited = true;
                    _previousPointDescription = point.Description;
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
        private void PointDescriptionTextbox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            if (textBox != null && textBox.DataContext is CogoPoint point)
            {
                e.Handled = true;

                if (_pointBeingEdited)
                {
                    point.Description = _previousPointDescription;
                    var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                    binding?.UpdateTarget();
                    _pointBeingEdited = false;
                }
                textBox.IsReadOnly = true;
            }
        }
        private void PointDescriptionTextbox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender is TextBox textBox && textBox.DataContext is CogoPoint point)
                {
                    e.Handled = true;

                    point.ClearErrors(nameof(point.Description));
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
                        _pointBeingEdited = false;
                        return;
                    }
                }
            }
            if (e.Key == Key.Escape)
            {
                if (sender is TextBox textBox && textBox.DataContext is CogoPoint point)
                {
                    e.Handled = true;
                    point.Description = _previousPointDescription;
                    var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                    binding?.UpdateTarget();
                    textBox.IsReadOnly = true;
                    _pointBeingEdited = false;
                }
            }
        }

        // Shared Properties Methods
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
                var pointNumExists = CadManager.CogoPointManager.PointExists(pointNum);
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

                bool isInt = int.TryParse(textbox.Text, out int pointNum);
                if (!isInt)
                {
                    Validation.MarkInvalid(BindingOperations.GetBindingExpression(textbox, TextBox.TextProperty),
                            new ValidationError(new DataErrorValidationRule(), textbox.GetBindingExpression(TextBox.TextProperty), "Point number must be a valid integer.", null));
                }

                var isValid = _validationService.ValidatePointNumber(pointNum, CadManager.CogoPointManager, out string errorMessage);

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
        private void PropertiesDescriptionTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
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
