using SharpDX;
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
        private float _pointScale = 1.00f;
        private float _textHeight;
        private float _baseTextHeight = 1.00f;
        private DxfPoint[] _points = [];
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
        public float PointScale
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
        public float BaseTextHeight
        {
            get => _baseTextHeight;
            set
            {
                if (_baseTextHeight != value)
                {
                    _baseTextHeight = value;
                    OnPropertyChanged(nameof(BaseTextHeight));
                }
            }
        }
        public DxfPoint[] Points
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

        #region Methods
        public PointGroup(string name, Vector4 color, float pointScale, float baseTextHeight)
        {
            Name = name;
            Color = color;
            PointScale = pointScale;
            BaseTextHeight = baseTextHeight;
            TextHeight = baseTextHeight * pointScale;
        }

        public void UpdatePointsScale(float newTextHeight)
        {

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
