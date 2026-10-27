using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Msdf;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.DrawingObjects.HelperClasses;
using netDxf.Entities;
using PdfSharpCore.Drawing;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System.Diagnostics;
using System.Windows;

using Vector3 = SharpDX.Vector3;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingArc : DrawingCurve
    {
        #region Properties
        public bool IsLargeArc { get; set; }
        public Vector2d MidPoint { get; set; }

        private Arc DxfArc => EntityObject as Arc;
        #endregion

        #region Constructor
        public DrawingArc(
            Arc arc, ObjectLayer layer, Vector4 objectColor, ColorType colorType,
            LineType lineType, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingArc;
            Layer = layer;
            ObjectColor = objectColor;
            ColorType = colorType;
            LineType = lineType;
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
                RadiusPoint = new(arc.Center.X, arc.Center.Y);
                Sweep = EndAngle - StartAngle;
                if (Sweep < 0) { Sweep += 360; }
                IsLargeArc = Sweep >= 180;
                Length = (float)((Sweep * (Math.PI / 180)) * Radius);

                var vertices = arc.ToPolyline2D(10).Vertexes;
                SamplePoints = vertices
                    .Select(v => new Vector2d(v.Position.X, v.Position.Y)).ToList();
                UpdateArcMidpoint();
                Start = vertices.First().Position.ToVector2d();
                End = vertices.Last().Position.ToVector2d();
                UpdateBounds();
            }
            else
            {
                throw new ArgumentException("entity must be of type Arc");
            }
        }
        public override void DrawToPdf(XGraphics gfx, System.Windows.Media.Matrix worldToPdf, XPen pen)
        {
            var cPdf = PdfDrawingHelpers.WorldToPdf(new Vector2d(RadiusPoint.X, RadiusPoint.Y), worldToPdf);

            var rWorldPt = new Vector2d(RadiusPoint.X + Radius, RadiusPoint.Y);
            var rPdfPt = PdfDrawingHelpers.WorldToPdf(rWorldPt, worldToPdf);

            double rPts = Math.Sqrt(
                (rPdfPt.X - cPdf.X) * (rPdfPt.X - cPdf.X) +
                (rPdfPt.Y - cPdf.Y) * (rPdfPt.Y - cPdf.Y));

            if (rPts <= 0.00001) { return; }

            var rect = new XRect(cPdf.X - rPts, cPdf.Y - rPts, 2 * rPts, 2 * rPts);
            double start = PdfDrawingHelpers.NormalizeDeg(360.0 - StartAngle);
            double sweep = -Sweep;

            gfx.DrawArc(pen, rect, start, sweep);
        }
        public override void UpdateVertices(
            ResCache resCache, uint layerId, uint objectId, uint lineTypeId, Vector2d renderOrigin)
        {
            if (EntityObject is not Arc arc)
                throw new ArgumentException("entity must be of type Arc");

            Array.Clear(LineInstances);

            NumberOfSegments = CalculateSegments(Radius, Sweep);
            var vertices = arc.ToPolyline2D(NumberOfSegments).Vertexes;

            List<LineInstance> lineInstances = [];
            double accumulatedDistance = 0.0;

            for (int i = 0; i < vertices.Count - 1; i++)
            {
                var start = vertices[i].Position.ToVector2d().ToGpuRelative(renderOrigin);
                var end = vertices[i + 1].Position.ToVector2d().ToGpuRelative(renderOrigin);

                LineInstanceFlags flags = LineInstanceFlags.None;
                if (i == 0)
                {
                    flags |= LineInstanceFlags.ForceStartVisible;
                }
                if (i == vertices.Count - 2)
                {
                    flags |= LineInstanceFlags.ForceEndVisible;
                }

                LineInstance lineInstance = new(
                    start, end, layerId, objectId, (float)accumulatedDistance, (uint)flags, (float)Length);
                lineInstances.Add(lineInstance);

                double segmentLength = Vector2.Distance(end, start);
                accumulatedDistance += segmentLength;
            }

            LineInstances = lineInstances.ToArray();

            UpdateBounds();
        }
        public override double DistanceToPoint(Vector2d point, MsdfAtlas atlas = null)
        {
            double startRad = StartAngle * Math.PI / 180;
            double endRad = EndAngle * Math.PI / 180;

            double dx = point.X - RadiusPoint.X;
            double dy = point.Y - RadiusPoint.Y;
            double distanceToCenter = Math.Sqrt(dx * dx + dy * dy);

            double pointAngle = Math.Atan2(dy, dx);
            if (pointAngle < 0)
                pointAngle += 2 * Math.PI;

            bool withinArc = (startRad <= endRad && pointAngle >= startRad && pointAngle <= endRad) ||
                             (startRad > endRad && (pointAngle >= startRad || pointAngle <= endRad));

            if (withinArc)
            {
                return Math.Abs(distanceToCenter - Radius);
            }
            else
            {
                double startX = RadiusPoint.X + Radius * Math.Cos(startRad);
                double startY = RadiusPoint.Y + Radius * Math.Sin(startRad);
                double endX = RadiusPoint.X + Radius * Math.Cos(endRad);
                double endY = RadiusPoint.Y + Radius * Math.Sin(endRad);

                double distanceToStart = Math.Sqrt(
                    (point.X - startX) * (point.X - startX) + (point.Y - startY) * (point.Y - startY));
                double distanceToEnd = Math.Sqrt(
                    (point.X - endX) * (point.X - endX) + (point.Y - endY) * (point.Y - endY));

                return Math.Min(distanceToStart, distanceToEnd);
            }
        }

        public override void UpdateBounds()
        {
            // Initialize bounds from the arc endpoints.
            Rect bounds = new Rect(Start.ToPoint(), End.ToPoint());

            // Include cardinal points that lie within the arc sweep.
            for (int i = 0; i < 4; i++)
            {
                double angle = i * 90.0;

                double relativeAngle =
                    ((angle - StartAngle) % 360.0 + 360.0) % 360.0;

                if (Sweep >= 360.0 || relativeAngle <= Sweep)
                {
                    double radians = angle * Math.PI / 180.0;

                    double x = RadiusPoint.X + Radius * Math.Cos(radians);
                    double y = RadiusPoint.Y + Radius * Math.Sin(radians);

                    bounds.Union(new System.Windows.Point(x, y));
                }
            }

            bounds.Inflate(
                bounds.Width * GlobalHelperProperties.GeometryBoundsInflationFactor,
                bounds.Height * GlobalHelperProperties.GeometryBoundsInflationFactor);

            Bounds = bounds;
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
            double midAngle = StartAngle + (Sweep / 2); // Midpoint angle in degrees
            double midAngleRadians = midAngle * Math.PI / 180; // Convert to radians

            // Calculate midpoint in XY plane
            double midX = RadiusPoint.X + (float)(Radius * Math.Cos(midAngleRadians));
            double midY = RadiusPoint.Y + (float)(Radius * Math.Sin(midAngleRadians));

            MidPoint = new(midX, midY);
        }
        #endregion
    }
}
