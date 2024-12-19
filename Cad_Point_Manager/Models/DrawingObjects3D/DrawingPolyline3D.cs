using Cad_Point_Manager.Controls.D3DControl;
using netDxf;
using netDxf.Entities;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Vector3 = SharpDX.Vector3;
using Vector4 = SharpDX.Vector4;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class DrawingPolyline3D : DrawingObject3D
    {
        public int StartVertexIndex { get; set; }
        public int EndVertexIndex { get; set; }
        public Vertex StartVertex { get; set; }
        public Vertex EndVertex { get; set; }
        public List<DrawingSegment3D> DrawingSegment3Ds { get; set; } = [];

        private DrawingPolyline3D() { Type = DrawingObject3dType.DrawingLine3D; }

        public DrawingPolyline3D(Polyline2D polyline2D)
        {
            Type = DrawingObject3dType.DrawingPolyline3D;

            Color = new(0, 0, 0, 1);

            var start = polyline2D.Vertexes.First();
            var end = polyline2D.Vertexes.Last();
            StartVertex = new(new Vector3((float)start.Position.X, (float)start.Position.Y, 0), Color);
            EndVertex = new(new Vector3((float)end.Position.X, (float)end.Position.Y, 0), Color);

            var entities = polyline2D.Explode();
            foreach (var entity in entities)
            {
                if (entity is Line line)
                {
                    DrawingSegment3Ds.Add(new DrawingLine3D(line));
                }
                else if (entity is Arc arc)
                {
                    DrawingSegment3Ds.Add(new DrawingArc3D(arc));
                }
            }
        }

        public DrawingPolyline3D(Polyline3D polyline3D)
        {
            Type = DrawingObject3dType.DrawingPolyline3D;

            Color = new(0, 0, 0, 1);

            var start = polyline3D.Vertexes.First();
            var end = polyline3D.Vertexes.Last();
            StartVertex = new(new Vector3((float)start.X, (float)start.Y, 0), Color);
            EndVertex = new(new Vector3((float)end.X, (float)end.Y, 0), Color);

            var entities = polyline3D.Explode();
            foreach (var entity in entities)
            {
                if (entity is Line line)
                {
                    DrawingSegment3Ds.Add(new DrawingLine3D(line));
                }
                else if (entity is Arc arc)
                {
                    DrawingSegment3Ds.Add(new DrawingArc3D(arc));
                }
            }
        }
    }
}
