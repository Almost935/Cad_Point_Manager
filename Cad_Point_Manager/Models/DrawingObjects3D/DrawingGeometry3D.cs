using Cad_Point_Manager.Controls.D3DControl;
using SharpDX.Direct2D1;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public abstract class DrawingGeometry3D : DrawingObject3D
    {
        public LineVertex StartVertex { get; set; }
        public LineVertex EndVertex { get; set; }
        public List<LineVertex> Vertices { get; set; } = [];
        public int StartVertexIndex { get; set; }
        public int EndVertexIndex { get; set; }
        public Geometry Geometry2D { get; set; }

        public override void MouseEnter()
        {
            this.IsMouseOver = true;

            for (int i = 0; i < Vertices.Count; i++)
            {
                var vertex = Vertices[i];
                vertex.IsVisible = 0.0f;
                Vertices[i] = vertex;
            }
        }
        public override void MouseLeave()
        {
            this.IsMouseOver = false;

            for (int i = 0; i < Vertices.Count; i++)
            {
                var vertex = Vertices[i];
                vertex.IsVisible = 1.0f;
                Vertices[i] = vertex;
            }
        }
    }
}
