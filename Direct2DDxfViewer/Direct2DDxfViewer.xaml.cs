using Direct2DDxfViewer.Direct2DControl;
using Direct2DDXFViewer.DrawingObjects;
using netDxf;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace Direct2DDXFViewer
{
    /// <summary>
    /// Interaction logic for Direct2DDxfViewer.xaml
    /// </summary>
    public partial class Direct2DDxfViewer : UserControl, INotifyPropertyChanged
    {
        #region Fields

        #endregion

        #region Properties

        #endregion

        #region Dependency Properties
        public ObjectLayerManager LayerManagerValue
        {
            get { return (ObjectLayerManager)GetValue(LayerManagerValueProperty); }
            set { SetValue(LayerManagerValueProperty, value); }
        }

        public static readonly DependencyProperty LayerManagerValueProperty =
       DependencyProperty.Register(
           nameof(LayerManagerValue),
           typeof(ObjectLayerManager),
           typeof(Direct2DDxfViewer),
           new PropertyMetadata(null, OnLayerManagerChanged));

        public Point DxfPointerCoordsValue
        {
            get { return (Point)GetValue(DxfPointerCoordsValueProperty); }
            set { SetValue(DxfPointerCoordsValueProperty, value); }
        }

        public static readonly DependencyProperty DxfPointerCoordsValueProperty =
       DependencyProperty.Register(
           nameof(DxfPointerCoordsValue),
           typeof(Point),
           typeof(Direct2DDxfViewer),
           new PropertyMetadata(null));
        #endregion

        #region Constructor
        public Direct2DDxfViewer()
        {
            InitializeComponent();
        }
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Methods
        private static void OnLayerManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            int x = 0;
            //if (d is Direct2DDxfViewer control)
            //{
            //    control.LayerManagerValue = (ObjectLayerManager)e.NewValue;
            //}
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void ZoomToExtents()
        {
            dxfControl.ZoomToExtents();
        }
        #endregion

   
    }
}
