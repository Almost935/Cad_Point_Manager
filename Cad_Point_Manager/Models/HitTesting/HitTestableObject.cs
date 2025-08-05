using Cad_Point_Manager.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cad_Point_Manager.Models.HitTesting
{
    public abstract class HitTestableObject : ValidationBase, INotifyPropertyChanged
    {
        #region Fields
        private bool _isSelected = false;
        private bool _isMouseOver = false;
        #endregion

        #region Properties
        public Rect Bounds { get; set; } = Rect.Empty;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }
        public bool IsMouseOver
        {
            get => _isMouseOver;
            set
            {
                if (_isMouseOver != value)
                {
                    _isMouseOver = value;
                    OnPropertyChanged(nameof(IsMouseOver));
                }
            }
        }
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
        public bool RectContainsRect(Rect rect)
        {
            if (Bounds.IsEmpty || rect.IsEmpty)
            {
                return false;
            }

            if (rect.Contains(Bounds))
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
