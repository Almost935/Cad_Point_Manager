using SharpDX;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

namespace Cad_Point_Manager.Models.PointRendering
{
    public class PointGroup : INotifyPropertyChanged
    {
        #region Fields
        public readonly ContainerVisual VisualContainer = new();

        private string _name;
        private Vector4 _color = new(0, 0, 0, 1);
        //private System.Windows.Media.Color _windowsColor = System.Windows.Media.Color.FromArgb(0, 0, 0, 1);
        //private System.Windows.Media.SolidColorBrush _groupBrush;
        private bool _isVisible = true;
        private ObservableCollection<CogoPoint> _points = [];
        private CogoPointManager _cogoPointManager;
        private double _pointScale = 1;
        #endregion

        #region Properties
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }
        public Vector4 Color
        {
            get => _color;
            set
            {
                if (_color != value)
                {
                    _color = value;
                    OnPropertyChanged(nameof(Color));
                    UpdateWindowsColor();
                }
            }
        }
        //public System.Windows.Media.Color WindowsColor
        //{
        //    get => _windowsColor;
        //    set
        //    {
        //        if (_windowsColor != value)
        //        {
        //            _windowsColor = value;
        //            OnPropertyChanged(nameof(WindowsColor));
        //        }
        //    }
        //}
        //public SolidColorBrush GroupBrush
        //{
        //    get => _groupBrush;
        //    set
        //    {
        //        if (_groupBrush != value)
        //        {
        //            _groupBrush = value;
        //            OnPropertyChanged(nameof(GroupBrush));
        //        }
        //    }
        //}
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnPropertyChanged(nameof(IsVisible));
                }
            }
        }
        public ObservableCollection<CogoPoint> Points
        {
            get => _points;
            set
            {
                if (_points != value)
                {
                    _points = value;
                    OnPropertyChanged(nameof(Points));
                }
            }
        }
        public CogoPointManager CogoPointManager
        {
            get => _cogoPointManager;
            set
            {
                if (_cogoPointManager != value)
                {
                    _cogoPointManager = value;
                    OnPropertyChanged(nameof(CogoPointManager));
                }
            }
        }
        public double PointScale
        {
            get => _pointScale;
            set
            {
                if (_pointScale != value)
                {
                    _pointScale = value;
                    OnPropertyChanged(nameof(PointScale));
                }
            }
        }

        public double FontBaseSize { get; set; } = 4;
        public double MarkerBaseSize { get; set; } = 0.75;
        public SolidColorBrush GroupBrush { get; }
        public Pen SharedPen { get; }
        #endregion

        #region Constructors
        public PointGroup(string name, Vector4 color, CogoPointManager cogoPointManager, double pointScale)
        {
            Name = name;
            Color = color;
            WindowsColor = System.Windows.Media.Color.FromArgb((byte)(color.W * 255), (byte)(color.X * 255), (byte)(color.Y * 255), (byte)(color.Z * 255));
            GroupBrush = new(WindowsColor);
            CogoPointManager = cogoPointManager;
            PointScale = pointScale;
        }
        #endregion

        #region Methods
        public bool DeletePoint(CogoPoint point)
        {
            if (point != null)
            {
                Points.Remove(point);
                return true;
            }
            return false;
        }

        public CogoPoint AddPoint(int pointNum, Vector3 position, float elevation = 0, string description = "")
        {
            CogoPoint cogoPoint = new(this, pointNum, position, CogoPointManager, elevation, description);
            Points.Add(cogoPoint);
            return cogoPoint;
        }
        public bool TryAddPoint(int pointNum, Vector3 position, out CogoPoint cogoPoint, float elevation = 0, string description = "")
        {
            if (Points.Any(p => p.PointNumber == pointNum))
            {
                cogoPoint = null;
                return false;
            }
            cogoPoint = new(this, pointNum, position, CogoPointManager, elevation, description);
            Points.Add(cogoPoint);
            return true;
        }
        public bool TryAddPoint(CogoPoint point)
        {
            if (Points.Any(p => p.PointNumber == point.PointNumber))
            {
                return false;
            }
            Points.Add(point);
            return true;
        }

        public override string ToString()
        {
            return Name;
        }

        public void Redraw()
        {
            foreach (var point in Points)
            {
                point.RedrawAllVisuals();
            }
        }

        public void UpdateAllVisualTransforms(System.Windows.Media.Matrix matrix)
        {
            foreach (var point in Points)
            {
                point.UpdateAllVisualTransforms(matrix);
            }
        }
        public void UpdateMarkerVisualTransforms(System.Windows.Media.Matrix matrix)
        {
            foreach (var point in Points)
            {
                point.UpdateMarkerVisualTransform(matrix);
            }
        }
        public void UpdateTextVisualTransforms(System.Windows.Media.Matrix matrix)
        {
            foreach (var point in Points)
            {
                point.UpdateTextVisualTransform(matrix);
            }
        }

        public void UpdateWindowsColor()
        {
            WindowsColor = System.Windows.Media.Color.FromArgb(
                (byte)(Color.W * 255),
                (byte)(Color.X * 255),
                (byte)(Color.Y * 255),
                (byte)(Color.Z * 255));
            GroupBrush = new(WindowsColor);
        }

        public void MergeToPointGroup(PointGroup newPG)
        {
            foreach (var point in Points)
            {
                point.UpdatePointGroup(newPG);
            }
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            //if (propertyName == nameof(WindowsBrush) || propertyName == nameof(IsVisible) || propertyName == nameof(PointScale))
            //{
            //    Redraw();
            //}
        }
        #endregion
    }
}
