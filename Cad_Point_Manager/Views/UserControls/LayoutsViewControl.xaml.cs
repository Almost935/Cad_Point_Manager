using Cad_Point_Manager.Controls;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cad_Point_Manager.Views.UserControls
{
    /// <summary>
    /// Interaction logic for LayoutsViewControl.xaml
    /// </summary>
    public partial class LayoutsViewControl : UserControl
    {
        #region Dependency Properties
        public static readonly DependencyProperty CadManagerProperty =
            DependencyProperty.Register(
                nameof(CadManager),
                typeof(CadManager3D),
                typeof(LayoutsViewControl),
                new PropertyMetadata(null, OnCadManagerChanged));
        public CadManager3D? CadManager
        {
            get => (CadManager3D?)GetValue(CadManagerProperty);
            set => SetValue(CadManagerProperty, value);
        }

        public static readonly DependencyProperty ActiveLayoutProperty =
           DependencyProperty.Register(
               nameof(ActiveLayout),
               typeof(Layout),
               typeof(LayoutsViewControl),
               new PropertyMetadata(null, OnActiveLayoutChanged));
        public Layout? ActiveLayout
        {
            get => (Layout?)GetValue(ActiveLayoutProperty);
            set => SetValue(ActiveLayoutProperty, value);
        }

        public static readonly DependencyProperty RendererProperty =
            DependencyProperty.Register(
                nameof(Renderer),
                typeof(D3dDxfControl),
                typeof(LayoutsViewControl),
                new PropertyMetadata(null, OnRendererChanged));
        public D3dDxfControl? Renderer
        {
            get => (D3dDxfControl?)GetValue(RendererProperty);
            set => SetValue(RendererProperty, value);
        }

        public static readonly DependencyProperty ScenesProperty =
            DependencyProperty.Register(
                nameof(Scenes),
                typeof(IEnumerable<Scene>),
                typeof(LayoutsViewControl),
                new PropertyMetadata(null));
        public IEnumerable<Scene>? Scenes
        {
            get => (IEnumerable<Scene>?)GetValue(ScenesProperty);
            set => SetValue(ScenesProperty, value);
        }
        #endregion

        #region Constructors
        public LayoutsViewControl()
        {
            InitializeComponent();
        }
        #endregion

        #region Methods
        private void LayoutsListView_Loaded(object sender, RoutedEventArgs e)
        {
            DoubleClickSetListView listview = sender as DoubleClickSetListView;

            GridView layoutssGridView = listview.View as GridView;
            double layoutsListTotalWidth = layoutsListView.ActualWidth;
            double layoutsListColumnWidth = layoutsListTotalWidth / layoutssGridView.Columns.Count;
            if (layoutsListColumnWidth > 0)
            {
                layoutssGridView.Columns[0].Width = layoutsListColumnWidth * 1.0;
                layoutssGridView.Columns[1].Width = layoutsListColumnWidth * 1.0;
                layoutssGridView.Columns[2].Width = layoutsListColumnWidth * 1.0;
            }
        }

        private void LayoutsListInlineEditBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {

        }

        private void InlineEditBox_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void LayoutsCellDisplay_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private static void OnCadManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (LayoutsViewControl)d;
            var newValue = e.NewValue as CadManager3D;
            if (newValue is not null && newValue.Layouts.Count > 0)
            {
                ctrl.layoutsListView.ActiveObject = newValue.Layouts.First();
                ctrl.ActiveLayout = newValue.Layouts.First();
            }
        }
        private static void OnActiveLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (LayoutsViewControl)d;
            if (ctrl is not null)
            {
                ctrl.LayoutPreviewControl.RebuildAsync();
            }
        }
        private static void OnRendererChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (LayoutsViewControl)d;
            if (ctrl is not null)
            {
                ctrl.LayoutPreviewControl.RebuildAsync();
            }
        }
        #endregion
    }
}
