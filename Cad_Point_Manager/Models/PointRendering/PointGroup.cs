using Cad_Point_Manager.Controls.D3DControl;
using SharpDX;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Cad_Point_Manager.Models.PointRendering
{
    public class PointGroup : INotifyPropertyChanged
    {
        #region Fields
        private string _name;
        private Vector4 _color = new(0, 0, 0, 1);
        private System.Windows.Media.Color _windowsColor = System.Windows.Media.Color.FromArgb(0, 0, 0, 1);
        private bool _isVisible = true;
        private float _textHeight;
        private float _pointMarkerSize;
        private float _baseSizeFactor = 1.00f;
        private ObservableCollection<CogoPoint> _points = [];
        private bool _colorToggleOpen = false;
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
        public System.Windows.Media.Color WindowsColor
        {
            get => _windowsColor;
            set
            {
                if (_windowsColor != value)
                {
                    _windowsColor = value;
                    OnPropertyChanged(nameof(WindowsColor));
                }
            }
        }
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
        public float TextHeight
        {
            get => _textHeight;
            set
            {
                if (_textHeight != value)
                {
                    _textHeight = value;
                    OnPropertyChanged(nameof(TextHeight));
                }
            }
        }
        public float PointMarkerSize {             
            get => _pointMarkerSize;
            set
            {
                if (_pointMarkerSize != value)
                {
                    _pointMarkerSize = value;
                    OnPropertyChanged(nameof(PointMarkerSize));
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
        public bool ColorToggleOpen
        {
            get => _colorToggleOpen;
            set
            {
                if (_colorToggleOpen != value)
                {
                    _colorToggleOpen = value;
                    OnPropertyChanged(nameof(ColorToggleOpen));
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
        public float BaseSizeFactor
        {
            get => _baseSizeFactor;
            set
            {
                if (_baseSizeFactor != value)
                {
                    _baseSizeFactor = value;
                    OnPropertyChanged(nameof(BaseSizeFactor));
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
        #endregion

        #region Constructors
        public PointGroup(string name, Vector4 color, float textHeight, float markerSize, CogoPointManager cogoPointManager)
        {
            Name = name;
            Color = color;
            WindowsColor = System.Windows.Media.Color.FromArgb((byte)(color.W * 255), (byte)(color.X * 255), (byte)(color.Y * 255), (byte)(color.Z * 255));
            TextHeight = textHeight;
            PointMarkerSize = markerSize;
            CogoPointManager = cogoPointManager;
        }
        #endregion

        #region Methods
        public void AddPoint(int pointNum, Vector3 position, float elevation = 0, string description = "")
        {
            Points.Add(new(this, pointNum, position, TextHeight, PointMarkerSize, CogoPointManager, elevation, description));
        }

        public override string ToString()
        {
            return Name;
        }

        public void UpdateWindowsColor()
        {
            WindowsColor = System.Windows.Media.Color.FromArgb(
                (byte)(Color.W * 255),
                (byte)(Color.X * 255),
                (byte)(Color.Y * 255),
                (byte)(Color.Z * 255));
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
