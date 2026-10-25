using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Models.DrawingObjects.HelperClasses;
using SharpDX;
using System.Windows;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public abstract class DrawingGeometry : DrawingObject
    {
        public Vector2d Start { get; set; }
        public Vector2d End { get; set; }
        public LineInstance[] LineInstances { get; set; } = [];
        public int StartVertexIndex { get; set; }
        public int EndVertexIndex { get; set; }

        public abstract void UpdateVertices(
            ResCache resCache, uint layerId, uint objectId, uint lineTypeId, Vector2d renderOrigin);

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

        public abstract bool GeometryInRect(Rect rect);
    }
}
