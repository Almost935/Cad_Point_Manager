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

        private SigPointVertex _sigPointVertex;
        #endregion

        #region Properties
        public Vector3 Position;
        public SigPointVertex SigPointVertex
        {
            get { return _sigPointVertex; }
            set
            {
                _sigPointVertex = value;
            }
        }
        #endregion

        #region Constructors
        public HitTestablePoint(Vector3 position)
        {
            Position = position;
            _sigPointVertex = new(Position, 0, 0);
        }
        #endregion

        #region Methods
        public override void MouseEnter()
        {
            this.IsMouseOver = true;
            _sigPointVertex.SetIsMouseOver(true);
        }
        public override void MouseLeave()
        {
            this.IsMouseOver = false;
            _sigPointVertex.SetIsMouseOver(false);
        }
        public override void Select()
        {
            this.IsSelected = true;
            _sigPointVertex.SetIsSelected(true);
        }
        public override void Deselect()
        {
            this.IsSelected = false;
            _sigPointVertex.SetIsSelected(false);
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
