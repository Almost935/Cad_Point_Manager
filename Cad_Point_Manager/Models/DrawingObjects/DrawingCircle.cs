using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.DrawingObjects.HelperClasses;
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
        public DrawingCircle(Circle circle, ObjectLayer layer, Vector4 objectColor, ColorType colorType, LineType lineType, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingCircle;
            Layer = layer;
            ObjectColor = objectColor;
            ColorType = colorType;
            LineType = lineType;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock = block;
            EntityObject = circle;

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

        public override void UpdateVertices(ResCache resCache, uint layerId, uint objectId, uint lineTypeId)
        {
            if (EntityObject is Circle circle)
            {
                Array.Clear(LineInstances);

                NumberOfSegments = CalculateSegments(Radius, Sweep);

                var vertices = circle.ToPolyline2D(NumberOfSegments).Vertexes;
                List<LineInstance> lineInstances = [];
                double accumulatedDistance = 0.0;

                for (int i = 0; i < vertices.Count; i++)
                {
                    LineInstanceFlags flags = LineInstanceFlags.None;

                    if (i == 0)
                    {
                        flags |= LineInstanceFlags.ForceStartVisible;
                    }
                    if (i == vertices.Count - 2)
                    {
                        flags |= LineInstanceFlags.ForceEndVisible;
                    }

                    int next = (i + 1) % vertices.Count;

                    lineInstances.Add(new LineInstance(
                        vertices[i].Position.ToSharpDXVector2(), vertices[next].Position.ToSharpDXVector2(), 
                        layerId, objectId, (float)accumulatedDistance, (uint)flags, Length));
                    
                    double segmentLength = Vector2.Distance(vertices[next].Position.ToSharpDXVector2(), vertices[i].Position.ToSharpDXVector2());
                    accumulatedDistance += segmentLength;
                }

                LineInstances = lineInstances.ToArray();
                Start = LineInstances.First().Start.ToSharpDXVector3();
                End = LineInstances.Last().End.ToSharpDXVector3();

                UpdateBounds();
            }
            else
            {
                throw new ArgumentException("entity must be of type Circle");
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
