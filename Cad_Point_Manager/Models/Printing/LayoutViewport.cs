using SharpDX;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Cad_Point_Manager.Models.Printing
{
    public class LayoutViewport : INotifyPropertyChanged
    {
        #region Fields
        private Rect _localRectIn;
        private Rect _bounds;
        #endregion

        #region Properties
        public Rect LocalRectIn
        {
            get => _localRectIn;
            set
            {
                if (_localRectIn != value)
                {
                    _localRectIn = value;
                    OnPropertyChanged(nameof(LocalRectIn));
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
                }
            }
        }

        public Guid Id { get; init; } = Guid.NewGuid();
        public bool ShowBorder { get; set; } = true;
        public Matrix SceneMatrix { get; set; } = Matrix.Identity;
        #endregion

        #region Constructors
        public LayoutViewport(Rect localRectIn, Rect bounds)
        {
            _localRectIn = localRectIn;
            _bounds = bounds;
        }
        #endregion

        #region NotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
