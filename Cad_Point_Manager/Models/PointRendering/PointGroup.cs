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
        private bool _isVisible = true;
        private float _textHeight;
        private float _pointMarkerSize;
        private float _baseSizeFactor = 1.00f;
        private ObservableCollection<DxfPoint> _points = [];
        private bool _colorToggleOpen = false;
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
        public ObservableCollection<DxfPoint> Points
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
        #endregion

        #region Constructors
        public PointGroup(string name, Vector4 color, float textHeight, float markerSize)
        {
            Name = name;
            Color = color;
            TextHeight = textHeight;
            PointMarkerSize = markerSize;
        }
        #endregion

        #region Methods
        public void AddPoint(int pointNum, Vector3 position)
        {
            Points.Add(new(this, pointNum, position, TextHeight, PointMarkerSize));
        }

        public override string ToString()
        {
            return Name;
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
