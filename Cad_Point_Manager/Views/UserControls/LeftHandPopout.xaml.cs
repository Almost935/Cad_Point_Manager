using Cad_Point_Manager.Common.Collections;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.PointRendering;
using Cad_Point_Manager.Services;
using Cad_Point_Manager.ViewModels;
using Cad_Point_Manager.Views.Assorted;
using Cad_Point_Manager.Views.ValidationRules;
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
        private bool _viewsTabVisible = true;
        private double _viewsTabOpacity = 0;
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
                if (cadManager?.CogoPointManager?.PointGroups is ObservableCollection<PointGroup> pgs)
                {
                    pgs.CollectionChanged += control.PointGroups_CollectionChanged;
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
            ViewsTabVisible = false;
            ViewsTabOpacity = 0;
        }

        private void PropertiesBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            PointsTabVisible = false;
            PointsTabOpacity = 0;
            PropertiesTabVisible = true;
            PropertiesTabOpacity = 1;
            ViewsTabOpacity = 0;
            ViewsTabVisible = false;
        }

        // Views list
        private void ViewsListView_Loaded(object sender, RoutedEventArgs e)
        {

        }
        private void ViewsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        private void ViewsBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            PointsTabVisible = false;
            PointsTabOpacity = 0;
            PropertiesTabVisible = false;
            PropertiesTabOpacity = 0;
            ViewsTabVisible = true;
            ViewsTabOpacity = 1;
        }


        // ----------  DOUBLE-CLICK TO ENTER EDIT ----------
        private void CellDisplay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount < 2) { return; }
            if (sender is not FrameworkElement fe) { return; }

            string field = InferFieldNameFromDisplayElement(fe);

            if (string.IsNullOrEmpty(field)) { return; }

            var lvi = VisualTreeHelpers.FindAncestor<ListViewItem>(fe);
            if (lvi == null) { return; }
            
            InlineEdit.SetEditingField(lvi, field);

            fe.Dispatcher.BeginInvoke(new Action(() =>
            {
                var root = VisualTreeHelpers.FindAncestor<Grid>(fe) ?? (DependencyObject)fe;
                var editBox = VisualTreeHelpers.FindDescendantByName<TextBox>(root, "tbEdit");
                if (editBox != null)
                {
                    editBox.IsReadOnly = false;
                    editBox.Focus();
                    editBox.SelectAll();
                }
            }), DispatcherPriority.Input);
        }
        private static string InferFieldNameFromDisplayElement(FrameworkElement fe)
        {
            var grid = VisualTreeHelpers.FindAncestor<Grid>(fe);
            if (grid?.Name is string s && !string.IsNullOrWhiteSpace(s))
            {
                // Map headers to property names as written in XAML
                return s switch
                {
                    "pointNumber" => "PointNumber",
                    "northing" => "Northing",
                    "easting" => "Easting",
                    "elevation" => "Elevation",
                    "description" => "Description",
                    _ => null
                };
            }
            return null;
        }

        // ----------  COMMIT/CANCEL/EXIT EDIT ----------
        private void InlineEditBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox tb) return;

            var lvi = VisualTreeHelpers.FindAncestor<ListViewItem>(tb);
            if (lvi == null) return;

            var binding = tb.GetBindingExpression(TextBox.TextProperty);

            if (e.Key == Key.Enter)
            {
                // Validate via existing rules (you already use UpdateSourceTrigger=Explicit)
                binding?.UpdateSource();
                // If invalid, WPF will keep the error—stay in edit
                if (Validation.GetHasError(tb)) { e.Handled = true; return; }

                // leave edit mode
                InlineEdit.SetEditingField(lvi, null);
                e.Handled = true;

                // move focus to next cell
                (tb as UIElement)?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                return;
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
        private void InlineEditBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // If focus leaves the edit box, exit edit (commit if valid, otherwise keep error text synced)
            if (sender is not TextBox tb) return;
            var lvi = VisualTreeHelpers.FindAncestor<ListViewItem>(tb);
            if (lvi == null) return;

            var binding = tb.GetBindingExpression(TextBox.TextProperty);
            if (!Validation.GetHasError(tb))
                binding?.UpdateSource(); // commit if valid

            InlineEdit.SetEditingField(lvi, null);
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
        private void PropertiesPointGroupComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool found = CadManager.CogoPointManager.TryGetPointGroup(CogoPointSelectionViewModel.PointGroup, out PointGroup? pg);
            if (found)
            {
                foreach (var point in CogoPointSelectionViewModel.SelectedPoints)
                {
                    if (point.PointGroup.Name != CogoPointSelectionViewModel.PointGroup)
                    {
                        point.PointGroup = found ? pg : null;
                    }
                }
            }
        }


        // Testing
        public int CountRealizedItems(DependencyObject root)
        {
            int count = 0;
            var stack = new Stack<DependencyObject>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var d = stack.Pop();
                if (d is ListViewItem) count++;
                int n = VisualTreeHelper.GetChildrenCount(d);
                for (int i = 0; i < n; i++) stack.Push(VisualTreeHelper.GetChild(d, i));
            }
            return count;
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
