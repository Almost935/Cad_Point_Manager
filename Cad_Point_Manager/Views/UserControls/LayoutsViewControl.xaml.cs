using Cad_Point_Manager.Controls;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.Printing;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cad_Point_Manager.Views.UserControls
{
    /// <summary>
    /// Interaction logic for LayoutsViewControl.xaml
    /// </summary>
    public partial class LayoutsViewControl : UserControl, INotifyPropertyChanged
    {
        #region Fields
        private bool _previewNeedsReload = false;
        #endregion

        #region Properties

        #endregion

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
        // Layout related methods
        public void ReloadPreview()
        {
            LayoutPreviewControl.RebuildAsync();
        }

        private void LayoutsListView_Loaded(object sender, RoutedEventArgs e)
        {
            DoubleClickSetListView listview = sender as DoubleClickSetListView;

            GridView layoutssGridView = listview.View as GridView;
            double layoutsListTotalWidth = layoutsListView.ActualWidth;
            double layoutsListColumnWidth = layoutsListTotalWidth / layoutssGridView.Columns.Count;
            if (layoutsListColumnWidth > 0)
            {
                layoutssGridView.Columns[0].Width = layoutsListColumnWidth * 1.15;
                layoutssGridView.Columns[1].Width = layoutsListColumnWidth * 0.75;
                layoutssGridView.Columns[2].Width = layoutsListColumnWidth * 0.75;
                layoutssGridView.Columns[3].Width = layoutsListColumnWidth * 1.35;
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

        private void Layouts_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //foreach (var layout in CadManager.Layouts)
            //{
            //    Debug.WriteLine($"layout.Name: {layout.Name}");
            //}
            if (CadManager is not null && CadManager.Layouts.Count > 0 && ActiveLayout is null)
            {
                layoutsListView.ActiveObject = CadManager.Layouts.First();
                ActiveLayout = CadManager.Layouts.First();
            }
        }

        private static void OnCadManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (LayoutsViewControl)d;
            var oldValue = e.OldValue as CadManager3D;
            if (oldValue is not null)
            {
                oldValue.Layouts.CollectionChanged -= ctrl.Layouts_CollectionChanged;
            }
            var newValue = e.NewValue as CadManager3D;
            if (newValue is not null)
            {
                if (oldValue is not null)
                {
                    oldValue.Layouts.CollectionChanged -= ctrl.Layouts_CollectionChanged;
                }
                newValue.Layouts.CollectionChanged += ctrl.Layouts_CollectionChanged;
            }
        }
        private static void OnActiveLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (LayoutsViewControl)d;
            if (ctrl is not null) { ctrl.LayoutPreviewControl.RebuildAsync(); }

            if (e.OldValue is Layout oldLayout) { oldLayout.Viewport.PropertyChanged -= ctrl.ActiveLayoutViewport_PropertyChanged; }

            if (e.NewValue is Layout newLayout) { newLayout.Viewport.PropertyChanged += ctrl.ActiveLayoutViewport_PropertyChanged; }
        }
        private async void ActiveLayoutViewport_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LayoutViewport.Scene) ||
                e.PropertyName == nameof(LayoutViewport.LocalRect) ||
                e.PropertyName == nameof(LayoutViewport.ShowBorder))
            {
                // If PropertyChanged can come from a background thread, marshal to UI thread
                if (!Dispatcher.CheckAccess())
                {
                    await Dispatcher.InvokeAsync(() => LayoutPreviewControl.RebuildAsync());
                    return;
                }

                LayoutPreviewControl.RebuildAsync();
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

        // Scene related methods
        private void ScenesListView_Loaded(object sender, RoutedEventArgs e)
        {
            //ListView listview = sender as ListView;

            //// Set column widths on each gridview
            //GridView scenesGridView = listview.View as GridView;
            //double scenesGridViewTotalWidth = scenesListView.ActualWidth;
            //double scenesGridViewColumnWidth = scenesGridViewTotalWidth / scenesGridView.Columns.Count;
            //if (scenesGridViewColumnWidth > 0)
            //{
            //    scenesGridView.Columns[0].Width = scenesGridViewColumnWidth * 1.0;
            //    scenesGridView.Columns[1].Width = scenesGridViewColumnWidth * 1.0;
            //    scenesGridView.Columns[2].Width = scenesGridViewColumnWidth * 1.0;
            //    scenesGridView.Columns[3].Width = scenesGridViewColumnWidth * 1.0;
            //    scenesGridView.Columns[4].Width = scenesGridViewColumnWidth * 1.0;
            //}
        }
        private void ScenesListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }
        private void ScenesListView_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {

        }

        private void InsertSceneMenuItem_Click(object sender, RoutedEventArgs e)
        {

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
