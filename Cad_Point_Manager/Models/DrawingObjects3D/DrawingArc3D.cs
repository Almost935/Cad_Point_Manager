using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using netDxf;
using netDxf.Entities;
using System.Windows;
using System;


using Vector2 = SharpDX.Vector2;
using Vector3 = SharpDX.Vector3;
using Vector4 = SharpDX.Vector4;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class DrawingArc3D : DrawingCurve3D
    {
        #region Fields
        private Arc _dxfArc => EntityObject as Arc;
        #endregion

        #region Properties
        public bool IsLargeArc { get; set; }
        public Vector3 MidPoint { get; set; }
        #endregion

        #region Constructor
        private DrawingArc3D() { Type = DrawingObject3dType.DrawingLine3D; }

        public DrawingArc3D(Arc arc, ObjectLayer3D layer, bool isPartOfBlock = false, DrawingBlock3D block = null)
        {
            Type = DrawingObject3dType.DrawingArc3D;
            Layer = layer;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock3D = block;
            EntityObject = arc;

            UpdateColor();
            UpdateData(arc);
        }
        #endregion

        #region Methods
        public override void UpdateData(EntityObject entity)
        {
            if (entity is Arc arc)
            {
                Radius = (float)arc.Radius;
                StartAngle = (float)arc.StartAngle;
                EndAngle = (float)arc.EndAngle;
                RadiusPoint = new Vector3((float)arc.Center.X, (float)arc.Center.Y, (float)arc.Center.Z);
                Sweep = EndAngle - StartAngle;
                if (Sweep < 0) { Sweep += 360; }
                IsLargeArc = Sweep >= 180;
                Length = (float)((Sweep * (Math.PI / 180)) * Radius);

                UpdateArcMidpoint();
                UpdateVertices(arc);
                UpdateBounds();
            }
            else
            {
                throw new ArgumentException("entity must be of type Arc");
            }
        }

        public override void UpdateVertices(EntityObject entity)
        {
            if (entity is Arc arc)
            {
                Vertices.Clear();

                NumberOfSegments = CalculateSegments(Radius, Sweep);
                var vertices = arc.ToPolyline2D(NumberOfSegments).Vertexes;

                for (int i = 0; i < vertices.Count; i++)
                {
                    if (i == vertices.Count - 1) { break; }

                    LineVertex s = new(
                        new Vector3((float)vertices[i].Position.X, (float)vertices[i].Position.Y, 0),
                        Color);
                    LineVertex e = new(
                        new Vector3((float)vertices[i + 1].Position.X, (float)vertices[i + 1].Position.Y, 0),
                        Color);

                    Vertices.Add(s);
                    Vertices.Add(e);
                }

                StartVertex = Vertices.First();
                EndVertex = Vertices.Last();
            }
            else
            {
                throw new ArgumentException("entity must be of type Arc");
            }
        }

        public override bool HitTest(System.Windows.Point point, float tolerance)
        {
            return MathHelpers.IsPointOnArc(point.X, point.Y, RadiusPoint.X, RadiusPoint.Y, Radius, StartAngle, EndAngle, tolerance);
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

        public override void DrawToD2dDeviceContext(DeviceContext1 deviceContext, Factory2 factory, Brush brush, float thickness, StrokeStyle1 strokeStyle)
        {
            PathGeometry pathGeometry = new(factory);
            using (var geometrySink = pathGeometry.Open())
            {
                geometrySink.BeginFigure(new RawVector2(Vertices[0].Position.X, Vertices[0].Position.Y), FigureBegin.Hollow);
                for (int i = 0; i < Vertices.Count / 2; i++)
                {
                    int index = 2 * i + 1;
                    geometrySink.AddLine(new RawVector2(Vertices[index].Position.X, Vertices[index].Position.Y));
                }
                geometrySink.EndFigure(FigureEnd.Open);
                geometrySink.Close();
            }
            deviceContext.DrawGeometry(pathGeometry, brush, thickness, strokeStyle);
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
            float startZ = StartVertex.Position.Z;
            float endZ = EndVertex.Position.Z;
            float midZ = startZ + ((endZ - startZ) * (midAngle - StartAngle) / Sweep);

            MidPoint = new(midX, midY, midZ);
        }

        public override void UpdateBounds()
        {
            Bounds = Rect.Empty;

            if (_dxfArc is not null)
            {
                var samplePoints = _dxfArc.ToPolyline2D(5).Vertexes;
                foreach (var vertex in samplePoints)
                {
                    Bounds = Rect.Union(Bounds, new System.Windows.Point(vertex.Position.X, vertex.Position.Y));
                }
            }
        }
        #endregion
    }
}
