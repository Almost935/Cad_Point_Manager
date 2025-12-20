using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Models.Printing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for LayoutControl.xaml
    /// </summary>
    public partial class LayoutControl : UserControl, INotifyPropertyChanged
    {
        #region Fields
        #endregion

        #region Properties
        #endregion

        #region Dependency Properties
        public static readonly DependencyProperty RendererProperty =
            DependencyProperty.Register(
                nameof(Renderer),
                typeof(D3dDxfControl),
                typeof(LayoutControl),
                new PropertyMetadata(null));
        public D3dDxfControl? Renderer
        {
            get => (D3dDxfControl?)GetValue(RendererProperty);
            set => SetValue(RendererProperty, value);
        }

        public static readonly DependencyProperty ScenesProperty =
            DependencyProperty.Register(
                nameof(Scenes),
                typeof(IEnumerable<Scene>),
                typeof(LayoutControl),
                new PropertyMetadata(null));
        public IEnumerable<Scene>? Scenes
        {
            get => (IEnumerable<Scene>?)GetValue(ScenesProperty);
            set => SetValue(ScenesProperty, value);
        }

        public static readonly DependencyProperty LayoutProperty =
            DependencyProperty.Register(
                nameof(Layout),
                typeof(Layout),
                typeof(LayoutControl),
                new PropertyMetadata(null, OnLayoutChanged));
        public Layout? Layout
        {
            get => (Layout?)GetValue(LayoutProperty);
            set => SetValue(LayoutProperty, value);
        }

        public static readonly DependencyProperty PageWidthProperty =
            DependencyProperty.Register(
                nameof(PageWidth),
                typeof(double),
                typeof(LayoutControl),
                new PropertyMetadata(36.0));
        public double PageWidth
        {
            get => (double)GetValue(PageWidthProperty);
            set => SetValue(PageWidthProperty, value);
        }

        public static readonly DependencyProperty PageHeightProperty =
            DependencyProperty.Register(
                nameof(PageHeight),
                typeof(double),
                typeof(LayoutControl),
                new PropertyMetadata(24.0));
        public double PageHeight
        {
            get => (double)GetValue(PageHeightProperty);
            set => SetValue(PageHeightProperty, value);
        }
        #endregion

        #region Constructors
        public LayoutControl()
        {
            InitializeComponent();
        }
        #endregion

        #region Methods
        private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LayoutPreviewControl c)
                _ = c.RebuildAsync();
        }
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }
}
