using SharpDX;
using System.ComponentModel;

namespace Cad_Point_Manager.Models.Printing
{
    public class Scene : INotifyPropertyChanged
    {
        #region Fields
        private string _name = string.Empty;
        private RectangleF _bounds = RectangleF.Empty;
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
        public RectangleF Bounds
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
        public int ZoomStep { get; set; } = 1;
        public float ZoomFactor { get; set; } = 1.0f;
        public Vector2 Translation { get; set; } = Vector2.Zero;

        public float Zoom => (float)Math.Pow(ZoomFactor, ZoomStep);
        public Matrix3x2 TestMatrix { get; set; } = Matrix.Identity;

        public float BoundsLeft => _bounds.X;
        public float BoundsTop => _bounds.Y;
        public float BoundsRight => _bounds.X + _bounds.Width;
        public float BoundsBottom => _bounds.Y + _bounds.Height;
        public float BoundsWidth => _bounds.Width;
        public float BoundsHeight => _bounds.Height;
        #endregion

        #region Methods
        public RectangleF GetViewportFitBounds(ViewportF viewport)
        {
            if (viewport.Width <= 0 || viewport.Height <= 0)
                return RectangleF.Empty;

            if (Bounds.Width <= 0 || Bounds.Height <= 0)
                return RectangleF.Empty;

            float scaleX = Bounds.Width / viewport.Width;
            float scaleY = Bounds.Height / viewport.Height;

            float scale = Math.Max(scaleX, scaleY);

            float fitWidth = viewport.Width * scale;
            float fitHeight = viewport.Height * scale;

            float centerX = Bounds.X + Bounds.Width / 2f;
            float centerY = Bounds.Y + Bounds.Height / 2f;

            return new RectangleF(
                centerX - fitWidth / 2f,
                centerY - fitHeight / 2f,
                fitWidth,
                fitHeight
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
