using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.DrawingObjects.HelperClasses;
using netDxf.Entities;
using PdfSharpCore.Drawing;
using SharpDX;
using System.Diagnostics;
using System.Windows;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingEllipse : DrawingCurve
    {
        #region Properties
        public float Rotation { get; private set; }
        public float MajorAxis { get; private set; }
        public float MinorAxis { get; private set; }
        public bool IsLargeArc { get; private set; }
        public Vector3 MidPoint { get; set; }

        private Ellipse DxfEllipse => EntityObject as Ellipse;
        #endregion

        #region Constructors
        public DrawingEllipse(
            Ellipse ellipse, ObjectLayer layer, Vector4 objectColor, ColorType colorType,
            LineType lineType, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingEllipse;
            Layer = layer;
            ObjectColor = objectColor;
            ColorType = colorType;
            LineType = lineType;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock = block;
            EntityObject = ellipse;

            UpdateColor();
            UpdateData();
        }
        #endregion

        #region Methods
        public override void UpdateData()
        {
            if (EntityObject is not Ellipse ellipse) { throw new ArgumentException(); }

            RadiusPoint = ellipse.Center.ToSharpDXVector3();
            Rotation = (float)ellipse.Rotation;
            MajorAxis = (float)ellipse.MajorAxis;
            MinorAxis = (float)(ellipse.MinorAxis * ellipse.MajorAxis);
            StartAngle = (float)ellipse.StartAngle;
            EndAngle = (float)ellipse.EndAngle;
            Sweep = EndAngle - StartAngle;

            if (Sweep < 0) { Sweep += 360; }
            if (Math.Abs(Sweep) < 1e-6) { Sweep = 360.0f; }

            IsLargeArc = Sweep >= 180;

            double effectiveRadius = Math.Min(MajorAxis, MinorAxis);
            NumberOfSegments = CalculateSegments(effectiveRadius, Sweep);

            var vertices = ellipse.ToPolyline2D(NumberOfSegments).Vertexes;
            SamplePoints = vertices.Select(v => new System.Windows.Point(v.Position.X, v.Position.Y)).ToList();
            Start = vertices.First().Position.ToSharpDXVector3();
            End = vertices.Last().Position.ToSharpDXVector3();

            UpdateBounds();
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
        public override void UpdateVertices(ResCache resCache, uint layerId, uint objectId, uint lineTypeId)
        {
            if (EntityObject is not Ellipse ellipse) { throw new ArgumentException(); }

            var vertices = ellipse.ToPolyline2D(NumberOfSegments).Vertexes;
            List<LineInstance> lines = [];

            if (vertices.Count < 2)
            {
                LineInstances = [];
                UpdateBounds();
                return;
            }

            double accumulatedDistance = 0.0;

            bool closed = Math.Abs(Sweep - 360f) < 0.001f;

            for (int i = 0; i < vertices.Count - 1; i++)
            {
                Vector2 start = vertices[i].Position.ToSharpDXVector2();
                Vector2 end = vertices[i + 1].Position.ToSharpDXVector2();

                LineInstanceFlags flags = LineInstanceFlags.None;

                if (!closed)
                {
                    if (i == 0)
                    {
                        flags |= LineInstanceFlags.ForceStartVisible;
                    }

                    if (i == vertices.Count - 2)
                    {
                        flags |= LineInstanceFlags.ForceEndVisible;
                    }
                }

                lines.Add(
                    new LineInstance(start, end, layerId, objectId, (float)accumulatedDistance, (uint)flags));

                double dx = (double)end.X - start.X;
                double dy = (double)end.Y - start.Y;

                double segmentLength = Math.Sqrt(dx * dx + dy * dy);

                accumulatedDistance += segmentLength;
            }

            if (closed)
            {
                Vector2 first = vertices[0].Position.ToSharpDXVector2();
                Vector2 last = vertices[^1].Position.ToSharpDXVector2();

                if (Vector2.DistanceSquared(first, last) > 1e-12f)
                {
                    lines.Add(
                        new LineInstance(last, first, layerId, objectId, (float)accumulatedDistance, (uint)LineInstanceFlags.None));
                }
            }

            LineInstances = lines.ToArray();

            UpdateBounds();
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

            foreach (var p in SamplePoints)
            {
                Bounds = Rect.Union(Bounds, p);
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
