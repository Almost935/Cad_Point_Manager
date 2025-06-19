using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.PointRendering;
using netDxf.Header;
using SharpDX;
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

using Point = System.Windows.Point;

namespace Cad_Point_Manager.Views.UserControls
{
    /// <summary>
    /// Interaction logic for CogoPointUserControl.xaml
    /// </summary>
    public partial class CogoPointUserControl : UserControl, INotifyPropertyChanged
    {
        #region Dependency Properties
        public static readonly DependencyProperty CanvasPositionProperty =
            DependencyProperty.Register(nameof(CanvasPosition), typeof(Point), typeof(CogoPointUserControl),
                new PropertyMetadata(new Point(0,0), OnPointPositionChanged));
        public Point CanvasPosition
        {
            get => (Point)GetValue(CanvasPositionProperty);
            set => SetValue(CanvasPositionProperty, value);
        }

        public static readonly DependencyProperty CanvasTextInfoPositionProperty =
            DependencyProperty.Register(nameof(CanvasTextInfoPosition), typeof(Point), typeof(CogoPointUserControl),
                new PropertyMetadata(new Point(0, 0)));
        public Point CanvasTextInfoPosition
        {
            get => (Point)GetValue(CanvasTextInfoPositionProperty);
            set => SetValue(CanvasTextInfoPositionProperty, value);
        }

        public static readonly DependencyProperty PointGroupProperty =
            DependencyProperty.Register(nameof(PointGroup), typeof(PointGroup), typeof(CogoPointUserControl),
                new PropertyMetadata(null));
        public PointGroup PointGroup
        {
            get => (PointGroup)GetValue(PointGroupProperty);
            set => SetValue(PointGroupProperty, value);
        }

        public static readonly DependencyProperty PointScaleProperty =
            DependencyProperty.Register(nameof(PointScale), typeof(double), typeof(CogoPointUserControl),
                new PropertyMetadata(1.0));
        public double PointScale
        {
            get => (double)GetValue(PointScaleProperty);
            set => SetValue(PointScaleProperty, value);
        }

        public static readonly DependencyProperty PointNumberProperty =
            DependencyProperty.Register(nameof(PointNumber), typeof(int), typeof(CogoPointUserControl),
                new PropertyMetadata(1));
        public int PointNumber
        {
            get => (int)GetValue(PointNumberProperty);
            set => SetValue(PointNumberProperty, value);
        }

        public static readonly DependencyProperty ElevationProperty =
            DependencyProperty.Register(nameof(Elevation), typeof(double), typeof(CogoPointUserControl),
                new PropertyMetadata(0.0));
        public double Elevation
        {
            get => (double)GetValue(ElevationProperty);
            set => SetValue(ElevationProperty, value);
        }

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(CogoPointUserControl),
                new PropertyMetadata(""));
        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public static readonly DependencyProperty PointNumberIsVisibleProperty =
            DependencyProperty.Register(nameof(PointNumberIsVisible), typeof(bool), typeof(CogoPointUserControl),
                new PropertyMetadata(true));
        public bool PointNumberIsVisible
        {
            get => (bool)GetValue(PointNumberIsVisibleProperty);
            set => SetValue(PointNumberIsVisibleProperty, value);
        }

        public static readonly DependencyProperty ElevationIsVisibleProperty =
            DependencyProperty.Register(nameof(ElevationIsVisible), typeof(bool), typeof(CogoPointUserControl),
                new PropertyMetadata(true));
        public bool ElevationIsVisible
        {
            get => (bool)GetValue(ElevationIsVisibleProperty);
            set => SetValue(ElevationIsVisibleProperty, value);
        }

        public static readonly DependencyProperty DescriptionIsVisibleProperty =
            DependencyProperty.Register(nameof(DescriptionIsVisible), typeof(bool), typeof(CogoPointUserControl),
                new PropertyMetadata(true));
        public bool DescriptionIsVisible
        {
            get => (bool)GetValue(DescriptionIsVisibleProperty);
            set => SetValue(DescriptionIsVisibleProperty, value);
        }
        #endregion

        #region Constructors
        public CogoPointUserControl() { }
        //public CogoPointUserControl(CogoPoint cogoPoint)
        //{
        //    PointGroup = cogoPoint.PointGroup;
        //    Position = cogoPoint.PointPosition;
        //    PointNumber = cogoPoint.PointNumber;
        //    Elevation = cogoPoint.Elevation;
        //    Description = cogoPoint.Description;
        //    PointScale = cogoPoint.PointGroup.PointScale;

        //    InitializeComponent();
        //}
        #endregion

        #region Methods
        private static void OnPointPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            
        }
        public void InitializeTextinfoPosition(Point initialPosition)
        {
            CanvasTextInfoPosition = initialPosition;
        }
        public void UpdateTextInfoPosition(Point newPosition)
        {
            CanvasTextInfoPosition = newPosition;
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
