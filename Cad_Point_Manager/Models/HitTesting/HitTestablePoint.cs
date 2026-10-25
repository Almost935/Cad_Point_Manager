using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Msdf;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using SharpDX;
using System.Windows;
using Point = System.Windows.Point;

namespace Cad_Point_Manager.Models.HitTesting
{
    public class HitTestablePoint : HitTestableObject
    {
        #region Fields
        private const float _boundsSize = 1.0f;

        private Vector2d _position;
        private SignificantPointType _pointType;
        #endregion

        #region Properties
        public Vector2d Position
        {
            get { return _position; }
            set
            {
                _position = value;
                OnPropertyChanged(nameof(Position));
            }
        }
        public SignificantPointType PointType
        {
            get { return _pointType; }
            set
            {
                _pointType = value;
                OnPropertyChanged(nameof(PointType));
            }
        }

        public int Index { get; set; }
        #endregion

        #region Constructors
        public HitTestablePoint(Vector2d position, SignificantPointType pointType)
        {
            Position = position;
            PointType = pointType;
        }
        #endregion

        #region Methods
        public override void MouseEnter()
        {
            this.IsMouseOver = true;
        }
        public override void MouseLeave()
        {
            this.IsMouseOver = false;
        }
        public override void Select()
        {
            this.IsSelected = true;
        }
        public override void Deselect()
        {
            this.IsSelected = false;
        }
        public override void UpdateBounds()
        {
            Bounds = new Rect(
                Position.X - _boundsSize / 2,
                Position.Y - _boundsSize / 2,
                _boundsSize,
                _boundsSize);
        }
        public override double DistanceToPoint(Vector2d p, MsdfAtlas atlas = null)
        {
            return p.DistanceTo(Position);
        }
        #endregion

        #region Static Methods
        public static bool EqualsWithTolerance(HitTestablePoint p1, HitTestablePoint p2, float tolerance)
        {
            return Math.Abs(p1.Position.X - p2.Position.X) <= tolerance &&
                   Math.Abs(p1.Position.Y - p2.Position.Y) <= tolerance;
        }

        public static bool EqualsWithTolerance2D(HitTestablePoint p1, HitTestablePoint p2, float tolerance)
        {
            return Math.Abs(p1.Position.X - p2.Position.X) <= tolerance &&
                  Math.Abs(p1.Position.Y - p2.Position.Y) <= tolerance;
        }
        #endregion
    }
}
