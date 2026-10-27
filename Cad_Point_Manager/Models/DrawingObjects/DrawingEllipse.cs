using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Msdf;
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
        public double Rotation { get; private set; }
        public double MajorAxis { get; private set; }
        public double MinorAxis { get; private set; }
        public bool IsLargeArc { get; private set; }
        public Vector2d MidPoint { get; set; }

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

            RadiusPoint = ellipse.Center.ToVector2d();
            Rotation = ellipse.Rotation;
            MajorAxis = ellipse.MajorAxis;
            MinorAxis = ellipse.MinorAxis;
            StartAngle = ellipse.StartAngle;
            EndAngle = ellipse.EndAngle;
            Sweep = EndAngle - StartAngle;

            if (Sweep < 0) { Sweep += 360; }
            if (Math.Abs(Sweep) < 1e-6) { Sweep = 360.0f; }

            IsLargeArc = Sweep >= 180;

            double effectiveRadius = Math.Min(MajorAxis, MinorAxis);
            NumberOfSegments = CalculateSegments(effectiveRadius, Sweep);

            var vertices = ellipse.ToPolyline2D(NumberOfSegments).Vertexes;
            SamplePoints = vertices.Select(v => v.Position.ToVector2d()).ToList();

            Start = vertices.First().Position.ToVector2d();
            End = vertices.Last().Position.ToVector2d();

            UpdateBounds();
        }
        public override void DrawToPdf(
            XGraphics gfx, System.Windows.Media.Matrix worldToPdf, XPen pen)
        {
            var cPdf = PdfDrawingHelpers.WorldToPdf(RadiusPoint, worldToPdf);

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
                Vector2 start = vertices[i].Position.ToVector2d().ToGpuRelative(renderOrigin);
                Vector2 end = vertices[i + 1].Position.ToVector2d().ToGpuRelative(renderOrigin);

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
                    new LineInstance(start, end, layerId, objectId, (float)accumulatedDistance, (uint)flags, (float)Length));

                double dx = (double)end.X - start.X;
                double dy = (double)end.Y - start.Y;

                double segmentLength = Math.Sqrt(dx * dx + dy * dy);

                accumulatedDistance += segmentLength;
            }

            if (closed)
            {
                Vector2 first = vertices[0].Position.ToVector2d().ToGpuRelative(renderOrigin);
                Vector2 last = vertices[^1].Position.ToVector2d().ToGpuRelative(renderOrigin);

                if (Vector2.DistanceSquared(first, last) > 1e-12f)
                {
                    lines.Add(
                        new LineInstance(last, first, layerId, objectId, (float)accumulatedDistance, (uint)LineInstanceFlags.None, (float)Length));
                }
            }

            LineInstances = lines.ToArray();

            UpdateBounds();
        }
        public override double DistanceToPoint(Vector2d point, MsdfAtlas atlas = null)
        {
            if (SamplePoints is null || SamplePoints.Count < 2)
            {
                return double.MaxValue;
            }

            double minDistance = double.MaxValue;

            for (int i = 0; i < SamplePoints.Count - 1; i++)
            {
                double distance = DistanceToSegment(
                    point, SamplePoints[i], SamplePoints[i + 1]);

                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }

            bool closed = Math.Abs(Sweep - 360f) < 0.001f;

            if (closed)
            {
                double distance = DistanceToSegment(
                    point, SamplePoints[^1], SamplePoints[0]);

                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }

            return minDistance;
        }

        private static double DistanceToSegment(Vector2d p, Vector2d a, Vector2d b)
        {
            Vector2d d = b - a;
            double lengthSquared = d.LengthSquared();

            if (lengthSquared <= 1e-12)
            {
                double px = p.X - a.X;
                double py = p.Y - a.Y;

                return Math.Sqrt(px * px + py * py);
            }

            double t =
                ((p.X - a.X) * d.X +
                 (p.Y - a.Y) * d.Y) /
                lengthSquared;

            t = Math.Clamp(t, 0.0, 1.0);

            double closestX = a.X + t * d.X;
            double closestY = a.Y + t * d.Y;

            double distX = p.X - closestX;
            double distY = p.Y - closestY;

            return Math.Sqrt(distX * distX + distY * distY);
        }

        public override void UpdateBounds()
        {
            double a = MajorAxis * 0.5;
            double b = MinorAxis * 0.5;

            double rotation = Rotation * Math.PI / 180.0;
            double cosR = Math.Cos(rotation);
            double sinR = Math.Sin(rotation);

            const double TwoPi = 2.0 * Math.PI;

            // Convert netDxf polar angles to ellipse parameter angles.
            double ToParameter(double degrees)
            {
                double angle = degrees * Math.PI / 180.0;

                double t = Math.Atan2(
                    a * Math.Sin(angle),
                    b * Math.Cos(angle));

                return (t + TwoPi) % TwoPi;
            }

            double start = ToParameter(StartAngle);
            double end = ToParameter(EndAngle);

            double sweep = (end - start + TwoPi) % TwoPi;
            bool closed = Math.Abs(Sweep - 360.0) < 1e-6;

            Vector2d PointAt(double t)
            {
                double x = a * Math.Cos(t);
                double y = b * Math.Sin(t);

                return new Vector2d(
                    RadiusPoint.X + x * cosR - y * sinR,
                    RadiusPoint.Y + x * sinR + y * cosR);
            }

            bool WithinSweep(double t)
            {
                if (closed)
                    return true;

                double delta = (t - start + TwoPi) % TwoPi;
                return delta <= sweep + 1e-10;
            }

            // Initialize using the actual DXF-derived endpoints.
            Rect bounds = new(Start.ToPoint(), End.ToPoint());

            // Parameters of the X and Y extrema.
            double tx = Math.Atan2(-b * sinR, a * cosR);
            double ty = Math.Atan2(b * cosR, a * sinR);

            void Include(double t)
            {
                if (WithinSweep(t))
                    bounds.Union(PointAt(t));
            }

            Include(tx);
            Include(tx + Math.PI);
            Include(ty);
            Include(ty + Math.PI);

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
            double midAngle = StartAngle + (Sweep / 2);
            double midAngleRadians = midAngle * Math.PI / 180; 

            double midX = RadiusPoint.X + (Radius * Math.Cos(midAngleRadians));
            double midY = RadiusPoint.Y + (Radius * Math.Sin(midAngleRadians));

            MidPoint = new(midX, midY);
        }
        #endregion
    }
}
