using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Extensions;
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
    public class DrawingArc : DrawingCurve
    {
        #region Properties
        public bool IsLargeArc { get; set; }
        public Vector3 MidPoint { get; set; }

        private Arc DxfArc => EntityObject as Arc;
        #endregion

        #region Constructor
        public DrawingArc(Arc arc, ObjectLayer layer, Vector4 objectColor, ColorType colorType, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingArc;
            Layer = layer;
            ObjectColor = objectColor;
            ColorType = colorType;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock = block;
            EntityObject = arc;

            UpdateColor();
            UpdateData();
        }
        #endregion

        #region Methods
        public override void UpdateData()
        {
            if (EntityObject is Arc arc)
            {
                Radius = (float)arc.Radius;
                StartAngle = (float)arc.StartAngle;
                EndAngle = (float)arc.EndAngle;
                RadiusPoint = new Vector3((float)arc.Center.X, (float)arc.Center.Y, (float)arc.Center.Z);
                Sweep = EndAngle - StartAngle;
                if (Sweep < 0) { Sweep += 360; }
                IsLargeArc = Sweep >= 180;
                Length = (float)((Sweep * (Math.PI / 180)) * Radius);

                var vertices = arc.ToPolyline2D(10).Vertexes;
                SamplePoints = vertices
                    .Select(v => new System.Windows.Point(v.Position.X, v.Position.Y)).ToList();
                UpdateArcMidpoint();
                Start = vertices.First().Position.ToSharpDXVector3();
                End = vertices.Last().Position.ToSharpDXVector3();
            }
            else
            {
                throw new ArgumentException("entity must be of type Arc");
            }
        }
        public override void DrawToD2dDeviceContext(DeviceContext1 deviceContext, Factory2 factory, Brush brush, float thickness, StrokeStyle1 strokeStyle)
        {
            
        }
        public override void DrawToPdf(
            XGraphics gfx,
            System.Windows.Media.Matrix worldToPdf,
            XPen pen)
        {
            // 1) Center in PDF coords
            var cPdf = PdfDrawingHelpers.WorldToPdf(new SharpDX.Vector2(RadiusPoint.X, RadiusPoint.Y), worldToPdf);

            // 2) Radius in PDF "points"
            //    (compute by transforming one known radius point; this automatically respects your camera scale)
            var rWorldPt = new SharpDX.Vector2(RadiusPoint.X + Radius, RadiusPoint.Y);
            var rPdfPt = PdfDrawingHelpers.WorldToPdf(rWorldPt, worldToPdf);

            double rPts = Math.Sqrt(
                (rPdfPt.X - cPdf.X) * (rPdfPt.X - cPdf.X) +
                (rPdfPt.Y - cPdf.Y) * (rPdfPt.Y - cPdf.Y));

            if (rPts <= 0.00001) { return; }

            // 3) Bounding rect for the circle in PDF coords
            var rect = new XRect(cPdf.X - rPts, cPdf.Y - rPts, 2 * rPts, 2 * rPts);

            // 4) Convert DXF (CCW, Y-up) angles to PDF page (Y-down) angles.
            //    When you flip Y, CCW becomes CW. A simple conversion is:
            //      start' = 360 - start
            //      sweep' = -sweep
            double start = PdfDrawingHelpers.NormalizeDeg(360.0 - StartAngle);
            double sweep = -Sweep;

            // Optional: if you ever store arcs that cross 0 and Sweep got weird, you can recompute sweep:
            // double end = NormalizeDeg(360.0 - EndAngle);
            // double sweep = ComputeSweepCW(start, end);

            gfx.DrawArc(pen, rect, start, sweep);
        }
        public override void UpdateVertices(ResCache resCache, uint layerId, uint objectId)
        {
            if (EntityObject is Arc arc)
            {
                Array.Clear(LineInstances);

                NumberOfSegments = CalculateSegments(Radius, Sweep);
                var vertices = arc.ToPolyline2D(NumberOfSegments).Vertexes;
                List<LineInstance> lineInstances = [];

                for (int i = 0; i < vertices.Count; i++)
                {
                    if (i == vertices.Count - 1) { break; }

                    LineInstance lineInstance = new()
                    {
                        Start = new((float)vertices [i].Position.X, (float)vertices [i].Position.Y),
                        End = new((float)vertices [i + 1].Position.X, (float)vertices [i + 1].Position.Y),
                        LayerId = layerId,
                        ObjectId = objectId,
                    };

                    lineInstances.Add(lineInstance);
                }

                LineInstances = lineInstances.ToArray();

                UpdateBounds();
            }
            else
            {
                throw new ArgumentException("entity must be of type Arc");
            }
        }                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           
        public override double DistanceToPoint(System.Windows.Point point)
        {
            // Convert angles to radians
            double startRad = StartAngle * Math.PI / 180;
            double endRad = EndAngle * Math.PI / 180;

            // Calculate the distance from the point to the center of the circle
            double dx = point.X - RadiusPoint.X;
            double dy = point.Y - RadiusPoint.Y;
            double distanceToCenter = Math.Sqrt(dx * dx + dy * dy);

            // Calculate the angle of the point relative to the center
            double pointAngle = Math.Atan2(dy, dx);
            if (pointAngle < 0) pointAngle += 2 * Math.PI; // Normalize angle to [0, 2*PI]

            // Check if the point is within the angular range of the arc
            bool withinArc = (startRad <= endRad && pointAngle >= startRad && pointAngle <= endRad) ||
                             (startRad > endRad && (pointAngle >= startRad || pointAngle <= endRad));

            if (withinArc)
            {
                // Point is within the angular range of the arc
                return Math.Abs(distanceToCenter - Radius);
            }
            else
            {
                // Point is outside the angular range, calculate distance to the closest arc endpoint
                double startX = RadiusPoint.X + Radius * Math.Cos(startRad);
                double startY = RadiusPoint.Y + Radius * Math.Sin(startRad);
                double endX = RadiusPoint.X + Radius * Math.Cos(endRad);
                double endY = RadiusPoint.Y + Radius * Math.Sin(endRad);

                double distanceToStart = Math.Sqrt((point.X - startX) * (point.X - startX) + (point.Y - startY) * (point.Y - startY));
                double distanceToEnd = Math.Sqrt((point.X - endX) * (point.X - endX) + (point.Y - endY) * (point.Y - endY));

                return Math.Min(distanceToStart, distanceToEnd);
            }
        }
        public override void UpdateBounds()
        {
            Bounds = Rect.Empty;

            if (DxfArc is not null)
            {
                foreach (var point in SamplePoints)
                {
                    Bounds = Rect.Union(Bounds, point);
                }
            }
        }
        public override bool GeometryInRect(Rect rect)
        {
            if (rect.Contains(Bounds))
            {
                return true;
            }
            return false;
        }
        public void UpdateArcMidpoint()
        {
            // Calculate the midpoint angle
            float midAngle = StartAngle + (Sweep / 2); // Midpoint angle in degrees
            double midAngleRadians = midAngle * Math.PI / 180; // Convert to radians

            // Calculate midpoint in XY plane
            float midX = RadiusPoint.X + (float)(Radius * Math.Cos(midAngleRadians));
            float midY = RadiusPoint.Y + (float)(Radius * Math.Sin(midAngleRadians));

            // Interpolate the Z coordinate along the arc
            float startZ = Start.Z;
            float endZ = End.Z;
            float midZ = startZ + ((endZ - startZ) * (midAngle - StartAngle) / Sweep);

            MidPoint = new(midX, midY, midZ);
        }
        #endregion
    }
}
