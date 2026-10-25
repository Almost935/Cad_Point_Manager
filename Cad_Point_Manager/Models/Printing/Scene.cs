using Cad_Point_Manager.Common;
using SharpDX;
using System.ComponentModel;
using System.Windows;

namespace Cad_Point_Manager.Models.Printing
{
    public class Scene : INotifyPropertyChanged
    {
        #region Fields
        private string _name = string.Empty;
        private Rect _bounds = Rect.Empty;
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
        public Rect Bounds
        {
            get => _bounds;
            set
            {
                if (_bounds != value)
                {
                    _bounds = value;
                    OnPropertyChanged(nameof(Bounds));
                    OnPropertyChanged(nameof(BoundsLeft));
                    OnPropertyChanged(nameof(BoundsTop));
                    OnPropertyChanged(nameof(BoundsRight));
                    OnPropertyChanged(nameof(BoundsBottom));
                    OnPropertyChanged(nameof(BoundsWidth));
                    OnPropertyChanged(nameof(BoundsHeight));
                }
            }
        }

        public Guid SceneId { get; set; } = Guid.NewGuid();
        public double ZoomStep { get; set; } = 1;
        public double ZoomFactor { get; set; } = 1.0f;
        public Vector2d Translation { get; set; } = Vector2d.Zero;

        public double Zoom => Math.Pow(ZoomFactor, ZoomStep);
        public Matrix3x2 TestMatrix { get; set; } = Matrix.Identity;

        public double BoundsLeft => _bounds.X;
        public double BoundsTop => _bounds.Y;
        public double BoundsRight => _bounds.X + _bounds.Width;
        public double BoundsBottom => _bounds.Y + _bounds.Height;
        public double BoundsWidth => _bounds.Width;
        public double BoundsHeight => _bounds.Height;
        #endregion

        #region Methods
        public RectangleF GetViewportFitBounds(ViewportF viewport)
        {
            if (viewport.Width <= 0 || viewport.Height <= 0)
                return RectangleF.Empty;

            if (Bounds.Width <= 0 || Bounds.Height <= 0)
                return RectangleF.Empty;

            double scaleX = Bounds.Width / viewport.Width;
            double scaleY = Bounds.Height / viewport.Height;

            double scale = Math.Max(scaleX, scaleY);

            double fitWidth = viewport.Width * scale;
            double fitHeight = viewport.Height * scale;

            double centerX = Bounds.X + Bounds.Width / 2.0;
            double centerY = Bounds.Y + Bounds.Height / 2.0;

            return new RectangleF(
                (float)(centerX - fitWidth / 2.0),
                (float)(centerY - fitHeight / 2.0),
                (float)fitWidth,
                (float)fitHeight
            );
        }
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
