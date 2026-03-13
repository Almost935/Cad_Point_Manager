using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.PointRendering;
using Cad_Point_Manager.Services;
using Cad_Point_Manager.ViewModels;
using Cad_Point_Manager.Views.Assorted;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Cad_Point_Manager.Views.UserControls
{
    /// <summary>
    /// Interaction logic for PointsViewControl.xaml
    /// </summary>
    public partial class PointsViewControl : UserControl
    {
        #region Fields
        private ValidationService _validationService = new();

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
        #endregion

        #region Properties
        #endregion

        #region Dependency Objects
        public CadManager CadManager
        {
            get { return (CadManager)GetValue(CadManagerProperty); }
            set { SetValue(CadManagerProperty, value); }
        }
        public static readonly DependencyProperty CadManagerProperty =
        DependencyProperty.Register(
            nameof(CadManager),
            typeof(CadManager),
            typeof(PointsViewControl),
            new PropertyMetadata(null, OnCadManagerChanged));

        public PointsViewModel ViewModel
        {
            get => (PointsViewModel)GetValue(ViewModelProperty);
            private set => SetValue(ViewModelProperty, value);
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(PointsViewModel), typeof(PointsViewControl));

        #endregion

        #region Constructors
        public PointsViewControl()
        {
            InitializeComponent();
        }
        #endregion

        #region Methods
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

            string? currentField = PointsInferFieldNameFromDisplayElement(tb);
            string? editingField = InlineEdit.GetEditingField(lvi);

            if (!string.IsNullOrEmpty(editingField) &&
                currentField != null &&
                !string.Equals(editingField, currentField, StringComparison.Ordinal))
            {
                return;
            }

            InlineEdit.SetEditingField(lvi, null);
        }

        private void PointsListView_Loaded(object sender, RoutedEventArgs e)
        {
            ListView listview = sender as ListView;

            GridView pointsGridView = listview.View as GridView;
            double pointsListTotalWidth = pointsListView.ActualWidth;
            double pointsListColumnWidth = pointsListTotalWidth / pointsGridView.Columns.Count;
            if (pointsListColumnWidth > 0)
            {
                pointsGridView.Columns[0].Width = pointsListColumnWidth * 0.75;
                pointsGridView.Columns[1].Width = pointsListColumnWidth * 1.0;
                pointsGridView.Columns[2].Width = pointsListColumnWidth * 1.0;
                pointsGridView.Columns[3].Width = pointsListColumnWidth * 1.0;
                pointsGridView.Columns[4].Width = pointsListColumnWidth * 1.25;
                pointsGridView.Columns[5].Width = pointsListColumnWidth * 1.0;
            }
        }
        private void PointsListInlineEditBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox tb) { return; }

            var lvi = VisualTreeHelpers.FindAncestor<ListViewItem>(tb);
            if (lvi == null) { return; }
            if (lvi.DataContext is not CogoPoint cp) { return; }

            string field = InlineEdit.GetEditingField(lvi);
            var binding = tb.GetBindingExpression(TextBox.TextProperty);

            bool isNewPointRow = _isCreatingNewPoint && ReferenceEquals(cp, _newPoint);

            if (e.Key == Key.Enter)
            {
                string text = tb.Text;
                string? errorMessage = null;

                switch (field)
                {
                    case "PointNumber":
                        {
                            if (!_validationService.ValidatePointNumberChange(text, cp, CadManager.CogoPointManager, out string svcError))
                            {
                                errorMessage = svcError;
                            }
                            break;
                        }
                    case "Northing":
                    case "Easting":
                    case "Elevation":
                        {
                            if (!double.TryParse(text, out _))
                            {
                                errorMessage = $"{field} must be a valid number.";
                            }
                            break;
                        }
                    case "Description":
                        {
                            if (!_validationService.ValidateString(text, out string svcError))
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

                    if (field == "Northing" ||
                        field == "Easting")
                    {
                        CadManager.CogoPointManager.UpdateCogoPointTree();
                    }

                    e.Handled = true;

                    if (isNewPointRow)
                    {
                        // ------- NEW POINT MODE: go to next field or finish -------
                        int idx = Array.IndexOf(_newPointFieldOrder, field);
                        if (idx >= 0 && idx < _newPointFieldOrder.Length - 1)
                        {
                            // Next field in the wizard
                            _newPointFieldIndex = idx + 1;
                            string nextField = _newPointFieldOrder[_newPointFieldIndex];

                            InlineEdit.SetEditingField(lvi, nextField);

                            lvi.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                string tboxName = GetPointsPropertyFromTBox(nextField);
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
                            _isCreatingNewPoint = false;
                            _newPoint = null;
                            _newPointFieldIndex = -1;

                            CadManager.CogoPointCircleVerticesDirty = true;
                            CadManager.CogoPointTextVerticesDirty = true;
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
                if (isNewPointRow)
                {
                    // Cancel creation of this new point completely
                    if (CadManager?.CogoPointManager != null && _newPoint != null)
                    {
                        CadManager.CogoPointManager.DeletePoint(_newPoint);
                        CadManager.CogoPointCircleVerticesDirty = true;
                        CadManager.CogoPointTextVerticesDirty = true;
                    }

                    _isCreatingNewPoint = false;
                    _newPoint = null;
                    _newPointFieldIndex = -1;

                    InlineEdit.SetEditingField(lvi, null);
                    pointsListView.SelectedItem = null;

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
        private static string PointsInferFieldNameFromDisplayElement(FrameworkElement fe)
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
        private void PointsCellDisplay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount < 2) { return; }
            if (sender is not FrameworkElement fe || VisualTreeHelpers.FindAncestor<ListViewItem>(fe) is not ListViewItem lvi) { return; }

            string field = PointsInferFieldNameFromDisplayElement(fe);
            if (string.IsNullOrEmpty(field)) { return; }

            BeginPointsListCellEdit(lvi, field);
        }
        private void BeginPointsListCellEdit(ListViewItem lvi, string field)
        {
            if (lvi == null) { return; }

            InlineEdit.SetEditingField(lvi, field);
            string tboxName = GetPointsPropertyFromTBox(field);
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

        private void PointsListViewNewPoint_Click(object sender, RoutedEventArgs e)
        {
            if (CadManager is null || CadManager.CogoPointManager is null) { return; }
            if (CadManager.CogoPointManager.PointGroups.Count == 0)
            {
                MessageBox.Show("You must create a point group before you can create a point.");
                return;
            }

            bool created = TryCreateNewPoint(CadManager.CogoPointManager.GetNextAvailablePointNumber(_lastCreatedPointNumber),
                new Vector3(0, 0, 0), ActivePointGroup, 0, "");
        }
        private void PointsListViewRenamePoint_Click(object sender, RoutedEventArgs e)
        {
            if (_lastPointsListItem == null) { return; }

            BeginPointsListCellEdit(_lastPointsListItem, "PointNumber");
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
            if (CadManager is null || CadManager.CogoPointManager is null) { return; }

            var pointsToDelete = new List<CogoPoint>(_selectedPoints);
            foreach (var point in pointsToDelete)
            {
                CadManager.CogoPointManager.DeletePoint(point);
            }
            CadManager.CogoPointCircleVerticesDirty = true;
            CadManager.CogoPointTextVerticesDirty = true;
        }

        private static void OnCadManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not PointsViewControl control) { return; }
            control.ViewModel = new PointsViewModel(e.NewValue as CadManager);
        }
        #endregion
    }
}
