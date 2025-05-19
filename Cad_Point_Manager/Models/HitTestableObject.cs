using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cad_Point_Manager.Models
{
    public abstract class HitTestableObject
    {
        #region Properties
        public Rect Bounds { get; set; } = Rect.Empty;
        public bool IsSelected { get; set; } = false;
        public bool IsMouseOver { get; set; } = false;
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
            if (Bounds.IsEmpty || rect.IsEmpty) { return false; }

            if (Bounds.IntersectsWith(rect) || Bounds.Contains(rect) || rect.Contains(Bounds)) { return true; }

            return false;
        }

        #endregion
    }
}
