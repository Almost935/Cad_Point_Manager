using Cad_Point_Manager.Helpers;
using netDxf.Entities;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System.Windows;
using Vector3 = SharpDX.Vector3;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class DrawingPolyline3D : DrawingGeometry3D
    {
        #region Properties
        public float Length { get; set; }
        public bool IsClosed { get; set; }
        public List<DrawingSegment3D> DrawingSegments { get; set; } = [];
        public int NumberOfSegments => DrawingSegments.Count;
        #endregion

        #region Constructors
        private DrawingPolyline3D() { Type = DrawingObject3dType.DrawingPolyline3D; }

        public DrawingPolyline3D(Polyline2D polyline2D, ObjectLayer3D layer, bool isPartOfBlock = false, DrawingBlock3D block = null)
        {
            Type = DrawingObject3dType.DrawingPolyline3D;
            Layer = layer;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock3D = block;
            EntityObject = polyline2D;

            UpdateColor();
            UpdateData(polyline2D);
        }

        public DrawingPolyline3D(Polyline3D polyline3D, ObjectLayer3D layer, bool isPartOfBlock = false, DrawingBlock3D block = null)
        {
            Type = DrawingObject3dType.DrawingPolyline3D;
            Layer = layer;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock3D = block;
            EntityObject = polyline3D;

            UpdateColor();
            UpdateData(polyline3D);
        }
        #endregion

        #region Methods
        public override void UpdateData(EntityObject entity)
        {
            if (entity is Polyline2D polyline2d)
            {
                IsClosed = polyline2d.IsClosed;

                UpdateVertices(polyline2d);
                UpdateBounds();

                Length = 0;
                foreach (var segment in DrawingSegments)
                {
                    Length += segment.Length;
                }
            }
            else if (entity is Polyline3D polyline3d)
            {
                IsClosed = polyline3d.IsClosed;

                UpdateVertices(polyline3d);
                UpdateBounds();

                Length = 0;
                foreach (var segment in DrawingSegments)
                {
                    Length += segment.Length;
                }
            }
            else
            {
                throw new ArgumentException("entity must be of type Polyline2D or Polyline3D");
            }
        }

        public override void UpdateBounds()
        {
            Bounds = Rect.Empty;

            foreach (var segment in DrawingSegments)
            {
                Bounds = Rect.Union(Bounds, segment.Bounds);
            }
        }

        public override double DistanceToPoint(System.Windows.Point point)
        {
            double distance = double.MaxValue;

            Parallel.ForEach(DrawingSegments, segment =>
            {
                var d = segment.DistanceToPoint(point);
                if (d < distance)
                {
                    distance = d;
                }
            });

            return distance;
        }

        public override void DrawToD2dDeviceContext(DeviceContext1 deviceContext, Factory2 factory, Brush brush, float thickness, StrokeStyle1 strokeStyle)
        {
            PathGeometry pathGeometry = new(factory);
            using (var geometrySink = pathGeometry.Open())
            {
                geometrySink.BeginFigure(new RawVector2(Vertices[0].Position.X, Vertices[0].Position.Y), FigureBegin.Hollow);
                for (int i = 0; i < Vertices.Length / 2; i++)
                {
                    int index = 2 * i + 1;
                    geometrySink.AddLine(new RawVector2(Vertices[index].Position.X, Vertices[index].Position.Y));
                }
                geometrySink.EndFigure(FigureEnd.Open);
                geometrySink.Close();
            }

            deviceContext.DrawGeometry(pathGeometry, brush, thickness, strokeStyle);
        }


        public void UpdateVertices(EntityObject entity)
        {
            if (entity is Polyline2D polyline2D)
            {
                var start = polyline2D.Vertexes.First();
                var end = polyline2D.Vertexes.Last();
                Start = new Vector3((float)start.Position.X, (float)start.Position.Y, 0);
                End = new Vector3((float)end.Position.X, (float)end.Position.Y, 0);

                var entities = polyline2D.Explode();
                var vertices = polyline2D.Vertexes;

                foreach (var e in entities)
                {
                    var obj = DxfHelpers.GetDrawingSegment3D(e, Layer);
                    if (obj is not null)
                    {
                        DrawingSegments.Add(obj);
                    }
                }

                for (int i = 0; i < vertices.Count - 1; i++)
                {
                    var segment = DrawingSegments[i];
                    if (segment is DrawingArc3D arc)
                    {
                        var startArcVertex = arc.Vertices.First().Position;
                        Vector3 dxfStartVertex = new((float)vertices[i].Position.X, (float)vertices[i].Position.Y, 0);
                        var d = Vector3.Distance(startArcVertex, dxfStartVertex);

                        if (d > 0)
                        {
                            arc.Vertices.Reverse();
                        }
                    }
                }
                Vertices = DrawingSegments.SelectMany(s => s.Vertices).ToArray();
            }

            else if (entity is Polyline3D polyline3D)
            {
                var start = polyline3D.Vertexes.First();
                var end = polyline3D.Vertexes.Last();
                Start = new Vector3((float)start.X, (float)start.Y, 0);
                End = new Vector3((float)end.X, (float)end.Y, 0);

                var entities = polyline3D.Explode();
                var vertices = polyline3D.Vertexes;

                foreach (var e in entities)
                {
                    var obj = DxfHelpers.GetDrawingSegment3D(e, Layer);
                    if (obj is not null)
                    {
                        DrawingSegments.Add(obj);
                    }
                }

                // Loop through vertices to verify that drawing arcs are correctly aligned. Autocad always draws arcs counter-clockwise
                // so need to find the correct start and end vertices
                for (int i = 0; i < vertices.Count - 1; i++)
                {
                    var segment = DrawingSegments[i];
                    if (segment is DrawingArc3D arc)
                    {
                        var startArcVertex = arc.Vertices.First().Position;
                        Vector3 dxfStartVertex = new((float)vertices[i].X, (float)vertices[i].Y, 0);
                        var d = Vector3.Distance(startArcVertex, dxfStartVertex);

                        if (d > 0)
                        {
                            arc.Vertices.Reverse();
                        }
                    }
                }
                Vertices = DrawingSegments.SelectMany(s => s.Vertices).ToArray();
            }
            else
            {
                throw new ArgumentException("entity must be of type Polyline2D or Polyline3D");
            }
        }
        #endregion

    }
}
