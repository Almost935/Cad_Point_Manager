using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Point = System.Windows.Point;

namespace Cad_Point_Manager.Models.HitTesting
{
    public class HitTestablePoint : HitTestableObject
    {
        #region Fields
        private const float _boundsSize = 1.0f;
        #endregion

        #region Properties
        public Vector3 Position;
        public SigPointVertex SigPointVertex { get; set; }
        #endregion

        #region Constructors
        public HitTestablePoint(Vector3 position)
        {
            Position = position;
            SigPointVertex = new(Position);
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
            throw new NotImplementedException();
        }
        public override void Deselect()
        {
            throw new NotImplementedException();
        }
        public override void UpdateBounds()
        {
            Bounds = new Rect(
                Position.X - _boundsSize / 2,
                Position.Y - _boundsSize / 2,
                _boundsSize ,
                _boundsSize);
        }
        public override double DistanceToPoint(Point p)
        {
            return MathHelpers.PointToPointDistance(p, Position.ToPoint());
        }
        #endregion
    }
}
