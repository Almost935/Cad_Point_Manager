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

            LayerColor = new(polyline2D.Layer.Color.R / 255, polyline2D.Layer.Color.G / 255, polyline2D.Layer.Color.B / 255, 1);
            if (polyline2D.Color.IsByLayer) { Color = LayerColor; }
            else { Color = new(polyline2D.Color.R / 255, polyline2D.Color.G / 255, polyline2D.Color.B / 255, 1); }

            if (LayerColor == new Vector4(1, 1, 1, 1)) { Color = new(0, 0, 0, 1); }
            if (Color == new Vector4(1, 1, 1, 1)) { Color = new(0, 0, 0, 1); }

            var start = polyline2D.Vertexes.First();
            var end = polyline2D.Vertexes.Last();
            StartVertex = new(new Vector3((float)start.Position.X, (float)start.Position.Y, 0), Color);
            EndVertex = new(new Vector3((float)end.Position.X, (float)end.Position.Y, 0), Color);

            var entities = polyline2D.Explode();
            foreach (var entity in entities)
            {
                if (entity is Line line)
                {
                    DrawingLine3D drawingLine3D = new(line);
                    DrawingSegment3Ds.Add(drawingLine3D);
                }
                else if (entity is Arc arc)
                {
                    DrawingArc3D drawingArc3D = new(arc);
                    DrawingSegment3Ds.Add(drawingArc3D);
                }
            }
        }

        public DrawingPolyline3D(Polyline3D polyline3D)
        {
            Type = DrawingObject3dType.DrawingPolyline3D;

            LayerColor = new(polyline3D.Layer.Color.R / 255, polyline3D.Layer.Color.G / 255, polyline3D.Layer.Color.B / 255, 1);
            if (polyline3D.Color.IsByLayer) { Color = LayerColor; }
            else { Color = new(polyline3D.Color.R / 255, polyline3D.Color.G / 255, polyline3D.Color.B / 255, 1); }

            if (LayerColor == new Vector4(1, 1, 1, 1)) { Color = new(0, 0, 0, 1); }
            if (Color == new Vector4(1, 1, 1, 1)) { Color = new(0, 0, 0, 1); }

            var start = polyline3D.Vertexes.First();
            var end = polyline3D.Vertexes.Last();
            StartVertex = new(new Vector3((float)start.X, (float)start.Y, 0), Color);
            EndVertex = new(new Vector3((float)end.X, (float)end.Y, 0), Color);

            var entities = polyline3D.Explode();
            foreach (var entity in entities)
            {
                if (entity is Line line)
                {
                    DrawingLine3D drawingLine3D = new(line);
                    DrawingSegment3Ds.Add(drawingLine3D);
                }
                else if (entity is Arc arc)
                {
                    DrawingArc3D drawingArc3D = new(arc);
                    DrawingSegment3Ds.Add(drawingArc3D);
                }
            }
        }
    }
}
