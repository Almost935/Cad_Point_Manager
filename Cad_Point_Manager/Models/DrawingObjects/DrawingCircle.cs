using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using netDxf.Entities;
using PdfSharpCore.Drawing;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System.Windows;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingCircle : DrawingCurve
    {
        #region Fields
        private Circle _dxfCircle => EntityObject as Circle;
        #endregion

        #region Properties
        public float Circumference { get; set; }
        #endregion

        #region Constructor
        public DrawingCircle(Circle circle, ObjectLayer layer, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingCircle;
            Layer = layer;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock3D = block;
            EntityObject = circle;
            ColorByLayer = EntityObject.Color.IsByLayer;

            UpdateColor();
            UpdateData();
        }
        #endregion

        #region Methods
        public override void UpdateData()
        {
            if (EntityObject is Circle circle)
            {
                Radius = (float)circle.Radius;
                StartAngle = 0;
                EndAngle = 360;
                Sweep = EndAngle - StartAngle;
                RadiusPoint = new Vector3((float)circle.Center.X, (float)circle.Center.Y, (float)circle.Center.Z);
                Length = (float)((Sweep / 360) * (2 * Math.PI * Radius));
            }
            else
            {
                throw new ArgumentException("entity must be of type Circle");
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
            // Center in world (DXF)
            var cWorld = new Vector2(RadiusPoint.X, RadiusPoint.Y);

            // Center in PDF (points)
            var cPdf = PdfDrawingHelpers.WorldToPdf(cWorld, worldToPdf);

            // Compute radius in PDF points by transforming a known radius point.
            // Pick a point one radius to the +X direction in world.
            var rWorldPt = new Vector2(RadiusPoint.X + Radius, RadiusPoint.Y);
            var rPdfPt = PdfDrawingHelpers.WorldToPdf(rWorldPt, worldToPdf);

            double rPts = Math.Sqrt(
                (rPdfPt.X - cPdf.X) * (rPdfPt.X - cPdf.X) +
                (rPdfPt.Y - cPdf.Y) * (rPdfPt.Y - cPdf.Y));

            if (rPts <= 1e-6) { return; }

            // Bounding rect for the ellipse/circle in PDF coords
            var rect = new XRect(cPdf.X - rPts, cPdf.Y - rPts, 2 * rPts, 2 * rPts);

            // Draw a true vector circle (PDF curve, not line segments)
            gfx.DrawEllipse(pen, rect);
        }

        public override void UpdateVertices(uint layerId, uint objectId)
        {
            if (EntityObject is Circle circle)
            {
                Array.Clear(Vertices);

                NumberOfSegments = CalculateSegments(Radius, Sweep);

                var vertices = circle.ToPolyline2D(NumberOfSegments).Vertexes;
                List<LineVertex> lineVertices = [];

                for (int i = 0; i < vertices.Count; i++)
                {
                    if (i == vertices.Count - 1)
                    {
                        LineVertex start = new(
                            new Vector3((float)vertices[i].Position.X, (float)vertices[i].Position.Y, 0), layerId, objectId);
                        LineVertex end = new(
                            new Vector3((float)vertices[0].Position.X, (float)vertices[0].Position.Y, 0), layerId, objectId);
                        lineVertices.Add(start);
                        lineVertices.Add(end);

                        break;
                    }

                    LineVertex s = new(
                        new Vector3((float)vertices[i].Position.X, (float)vertices[i].Position.Y, 0), layerId, objectId);
                    LineVertex e = new(
                        new Vector3((float)vertices[i + 1].Position.X, (float)vertices[i + 1].Position.Y, 0), layerId, objectId);

                    lineVertices.Add(s);
                    lineVertices.Add(e);
                }

                Vertices = lineVertices.ToArray();
                Start = Vertices.First().Position;
                End = Vertices.Last().Position;

                UpdateBounds();
            }
            else
            {
                throw new ArgumentException("entity must be of type Arc");
            }
        }

        public override void UpdateBounds()
        {
            Bounds = Rect.Empty;

            if (_dxfCircle is not null)
            {
                var samplePoints = _dxfCircle.ToPolyline2D(4).Vertexes;
                foreach (var vertex in samplePoints)
                {
                    Bounds = Rect.Union(Bounds, new System.Windows.Point(vertex.Position.X, vertex.Position.Y));
                }
            }
        }

        public override double DistanceToPoint(System.Windows.Point point)
        {
            // Calculate the distance from the point to the center of the circle
            double dx = point.X - RadiusPoint.X;
            double dy = point.Y - RadiusPoint.Y;
            double distanceToCenter = Math.Sqrt(dx * dx + dy * dy);

            if (distanceToCenter >= Radius)
            {
                return distanceToCenter - Radius;
            }
            else
            {
                return Radius - distanceToCenter;
            }
        }

        public override bool GeometryInRect(Rect rect)
        {
            if (Bounds.IsEmpty || rect.IsEmpty)
            {
                return false;
            }

            // Check if the circle's center is within the rectangle
            if (BoundsInRect(rect))
            {
                return true;
            }

            return false;
        }
        #endregion
    }
}
