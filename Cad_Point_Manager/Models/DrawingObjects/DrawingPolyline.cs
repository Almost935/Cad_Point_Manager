using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using netDxf.Entities;
using PdfSharpCore.Drawing;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System.Windows;

using Vector3 = SharpDX.Vector3;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingPolyline : DrawingGeometry
    {
        #region Properties
        public float Length { get; set; }
        public bool IsClosed { get; set; }
        public List<DrawingSegment> DrawingSegments { get; set; } = [];
        public int NumberOfSegments => DrawingSegments.Count;
        #endregion

        #region Constructors
        //private DrawingPolyline() { Type = DrawingObject3dType.DrawingPolyline; }

        public DrawingPolyline(Polyline2D polyline2D, ObjectLayer layer, Vector4 objectColor, ColorType colorType, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingPolyline;
            Layer = layer;
            ObjectColor = objectColor;
            ColorType = colorType;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock = block;
            EntityObject = polyline2D;

            UpdateColor();
            UpdateData();
        }

        public DrawingPolyline(Polyline3D polyline3D, ObjectLayer layer, Vector4 objectColor, ColorType colorType, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingPolyline;
            Layer = layer;
            ObjectColor = objectColor;
            ColorType = colorType;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock = block;
            EntityObject = polyline3D;

            UpdateColor();
            UpdateData();
        }
        #endregion

        #region Methods
        public override void UpdateData()
        {
            if (EntityObject is Polyline2D polyline2d)
            {
                IsClosed = polyline2d.IsClosed;
                Length = 0;
                foreach (var segment in DrawingSegments)
                {
                    Length += segment.Length;
                }
            }
            else if (EntityObject is Polyline3D polyline3d)
            {
                IsClosed = polyline3d.IsClosed;
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
        public override void DrawToPdf(
           XGraphics gfx,
           System.Windows.Media.Matrix worldToPdf,
           XPen pen)
        {
            var segments = DrawingSegments.ToArray();

            foreach (var segment in segments)
            {
                segment.DrawToPdf(gfx, worldToPdf, pen);
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

        public override bool GeometryInRect(Rect rect)
        {
            if (Bounds.IsEmpty || rect.IsEmpty)
            {
                return false;
            }

            foreach (var geometry in DrawingSegments)
            {
                if (!geometry.BoundsInRect(rect))
                {
                    break;
                }
            }

            return false;
        }

        public override void UpdateVertices(ResCache resCache, uint layerId, uint objectId)
        {
            if (EntityObject is Polyline2D polyline2D)
            {
                var start = polyline2D.Vertexes.First();
                var end = polyline2D.Vertexes.Last();
                Start = new Vector3((float)start.Position.X, (float)start.Position.Y, 0);
                End = new Vector3((float)end.Position.X, (float)end.Position.Y, 0);

                var entities = polyline2D.Explode();
                var vertices = polyline2D.Vertexes;

                foreach (var e in entities)
                {
                    var colorType = DxfHelpers.GetColorType(e);
                    Vector4 color = DxfHelpers.GetEntityObjectColor(e, DrawingBlock?.DxfInsert);
                    var obj = DxfHelpers.GetDrawingSegment(e, Layer, color, colorType, DrawingBlock);

                    if (obj is not null)
                    {
                        obj.UpdateVertices(resCache, layerId, objectId);
                        DrawingSegments.Add(obj);
                    }
                }

                for (int i = 0; i < vertices.Count - 1; i++)
                {
                    var segment = DrawingSegments[i];
                    if (segment is DrawingArc arc)
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
                UpdateBounds();
            }

            else if (EntityObject is Polyline3D polyline3D)
            {
                var start = polyline3D.Vertexes.First();
                var end = polyline3D.Vertexes.Last();
                Start = new Vector3((float)start.X, (float)start.Y, 0);
                End = new Vector3((float)end.X, (float)end.Y, 0);

                var entities = polyline3D.Explode();
                var vertices = polyline3D.Vertexes;

                foreach (var e in entities)
                {
                    var colorType = DxfHelpers.GetColorType(e);
                    Vector4 color = DxfHelpers.GetEntityObjectColor(e, DrawingBlock.DxfInsert);
                    var obj = DxfHelpers.GetDrawingSegment(e, Layer, color, colorType, DrawingBlock);

                    if (obj is not null)
                    {
                        obj.UpdateVertices(resCache, layerId, objectId);
                        DrawingSegments.Add(obj);
                    }
                }

                for (int i = 0; i < vertices.Count - 1; i++)
                {
                    var segment = DrawingSegments[i];
                    if (segment is DrawingArc arc)
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
                UpdateBounds();
            }
            else
            {
                throw new ArgumentException("entity must be of type Polyline2D or Polyline3D");
            }
        }
        #endregion

    }
}
