using Cad_Point_Manager.Models.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cad_Point_Manager.Views.UserControls
{
    /// <summary>
    /// Interaction logic for LayoutsListViewControl.xaml
    /// </summary>
    public partial class LayoutsListViewControl : UserControl
    {
        #region Dependency Properties
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
        #endregion
        public LayoutsListViewControl()
        {
            InitializeComponent();
        }

        #region Methods
        private void LayoutsListInlineEditBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {

        }
        private static void OnActiveLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (LayoutsViewControl)d;
            if (ctrl is not null) { ctrl.LayoutPreviewControl.RebuildAsync(); }

            if (e.OldValue is Layout oldLayout) { oldLayout.Viewport.PropertyChanged -= ctrl.ActiveLayoutViewport_PropertyChanged; }

            if (e.NewValue is Layout newLayout) { newLayout.Viewport.PropertyChanged += ctrl.ActiveLayoutViewport_PropertyChanged; }
        }
        #endregion
    }
}
