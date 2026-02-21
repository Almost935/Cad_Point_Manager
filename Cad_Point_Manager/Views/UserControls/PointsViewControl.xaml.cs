using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.PointRendering;
using Cad_Point_Manager.ViewModels;
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

namespace Cad_Point_Manager.Views.UserControls
{
    /// <summary>
    /// Interaction logic for PointsViewControl.xaml
    /// </summary>
    public partial class PointsViewControl : UserControl
    {
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
        #endregion

        #region Constructors
        public PointsViewControl()
        {
            InitializeComponent();
        }
        #endregion

        #region Methods
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
            }
        }

        private void PointsListInlineEditBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {

        }

        private void InlineEditBox_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void PointsCellDisplay_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private static void OnCadManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LeftHandPopout control && e.NewValue is not null && e.NewValue is CadManager cadManager)
            {
                control.CogoPointSelectionViewModel?.UpdateCadManager(cadManager);
                control.CogoPointSelectionViewModel?.Refresh();
            }
        }
        #endregion
    }
}
