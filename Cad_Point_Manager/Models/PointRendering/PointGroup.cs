using Cad_Point_Manager.Common.Collections;
using Cad_Point_Manager.Controls.D3DControl;
using SharpDX;
using SharpDX.DirectWrite;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace Cad_Point_Manager.Models.PointRendering
{
    public class PointGroup : INotifyPropertyChanged
    {
        #region Fields
        private const float _markerToPointScaleFactor = 0.5f;

        private string _name;
        private Color _color = Colors.Black;
        private bool _isVisible = true;
        private BatchableObservableCollection<CogoPoint> _points = [];
        private CogoPointManager _cogoPointManager;
        private double _pointScale = 1;
        private float _pointInfoBaseXoffset = 0;
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
        public Color Color
        {
            get => _color;
            set
            {
                if (_color != value)
                {
                    _color = value;
                    OnPropertyChanged(nameof(Color));
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
        public BatchableObservableCollection<CogoPoint> Points
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
        public float PointInfoBaseXoffset
        {
            get => _pointInfoBaseXoffset;
            set
            {
                if (_pointInfoBaseXoffset != value)
                {
                    _pointInfoBaseXoffset = value;
                    OnPropertyChanged(nameof(PointInfoBaseXoffset));
                }
            }
        }

        public double FontBaseSize { get; set; } = 4;
        public double MarkerBaseSize { get; set; } = 0.75;
        public GroupState PointGroupState { get; set; }        
        #endregion

        #region Constructors
        public PointGroup(string name, Color color, CogoPointManager cogoPointManager, double pointScale)
        {
            Name = name;
            Color = color;
            CogoPointManager = cogoPointManager;
            PointScale = pointScale;
            UpdatePointInfoBaseXoffset();
        }
        public PointGroup() { }
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
            return $"PointGroup Name: {Name}";
        }

        public void MergeToPointGroup(PointGroup newPG)
        {
            foreach (var point in Points)
            {
                point.UpdatePointGroup(newPG);
            }
        }

        public void UpdatePointInfoBaseXoffset()
        {
            PointInfoBaseXoffset = (float)(FontBaseSize * PointScale * _markerToPointScaleFactor);
        }
        #endregion

        #region Equality
        public override bool Equals(object? obj)
        {
            return Equals(obj as PointGroup);
        }

        public bool Equals(PointGroup? other)
        {
            if (other is null)
                return false;

            // Compare only by Name (case-insensitive is common for names)
            return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            // Use a consistent hash for the Name
            return StringComparer.OrdinalIgnoreCase.GetHashCode(Name ?? string.Empty);
        }

        public static bool operator ==(PointGroup? left, PointGroup? right)
        {
            return EqualityComparer<PointGroup>.Default.Equals(left, right);
        }

        public static bool operator !=(PointGroup? left, PointGroup? right)
        {
            return !(left == right);
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
