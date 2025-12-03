using Cad_Point_Manager.Common;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Cad_Point_Manager.Models.HitTesting
{
    public abstract class HitTestableObject : ValidationBase, INotifyPropertyChanged
    {
        #region Fields

        #endregion

        #region Properties
        public Rect Bounds { get; set; } = Rect.Empty;
        public bool IsMouseOver { get; set; } = false;
        public bool IsSelected { get; set; } = false;
        #endregion

        #region Methods
        public abstract double DistanceToPoint(Point p);
        public abstract void UpdateBounds();
        public abstract void MouseEnter();
        public abstract void MouseLeave();
        public abstract void Select();
        public abstract void Deselect();

        public bool BoundsInRect(Rect rect)
        {
            if (Bounds.IsEmpty || rect.IsEmpty)
            {
                return false;
            }

            if (Bounds.IntersectsWith(rect) || Bounds.Contains(rect) || rect.Contains(Bounds))
            {
                return true;
            }
            return false;
        }
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
