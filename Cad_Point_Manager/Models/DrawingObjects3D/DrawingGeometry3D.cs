using Cad_Point_Manager.Controls.D3DControl;
using SharpDX;
using System.Windows;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public abstract class DrawingGeometry3D : DrawingObject3D
    {
        public Vector3 Start { get; set; }
        public Vector3 End { get; set; }
        public LineVertex[] Vertices { get; set; } = [];
        public int StartVertexIndex { get; set; }
        public int EndVertexIndex { get; set; }

        public override void MouseEnter()
        {
            this.IsMouseOver = true;

            Span<LineVertex> span = Vertices;
            for (int i = 0; i < span.Length; i++)
            {
                span[i].SetIsMouseOver(true);
            }
        }
        public override void MouseLeave()
        {
            this.IsMouseOver = false;

            Span<LineVertex> span = Vertices;
            for (int i = 0; i < span.Length; i++)
            {
                span[i].SetIsMouseOver(false);
            }
        }

        public override void Select()
        {
            this.IsSelected = true;

            Span<LineVertex> span = Vertices;
            for (int i = 0; i < span.Length; i++)
            {
                span[i].SetIsSelected(true);
            }
        }
        public override void Deselect()
        {
            this.IsSelected = false;

            Span<LineVertex> span = Vertices;
            for (int i = 0; i < span.Length; i++)
            {
                span[i].SetIsSelected(false);
            }
        }

        public abstract bool GeometryInRect(Rect rect);
    }
}
