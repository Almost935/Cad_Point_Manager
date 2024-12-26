using Cad_Point_Manager.Controls.D3DControl;
using netDxf;
using netDxf.Entities;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


using Vector2 = SharpDX.Vector2;
using Vector3 = SharpDX.Vector3;
using Vector4 = SharpDX.Vector4;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class DrawingArc3D : DrawingCurve3D
    {
        #region Fields
        private Arc _arc => EntityObject as Arc;
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

                    Vertex s = new(
                        new Vector3((float)vertices[i].Position.X, (float)vertices[i].Position.Y, 0),
                        Color);
                    Vertex e = new(
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

        public override bool HitTest(Vector2 point, float tolerance)
        {
            // Step 1: Check if point is on the circle
            double distance = Math.Sqrt(Math.Pow(point.X - RadiusPoint.X, 2) + Math.Pow(point.X - RadiusPoint.Y, 2));
            if (Math.Abs(distance - Radius) > tolerance)
                return false;

            // Step 2: Check if the point is within the arc's angular bounds
            double angle = Math.Atan2(point.Y - RadiusPoint.Y, px - cx) * (180.0 / Math.PI); // Convert to degrees
            angle = (angle + 360) % 360; // Normalize to [0, 360]

            // Normalize the start and end angles
            startAngle = (startAngle + 360) % 360;
            endAngle = (endAngle + 360) % 360;

            if (startAngle <= endAngle)
            {
                return angle >= startAngle && angle <= endAngle;
            }
            else
            {
                // Arc crosses 360 boundary
                return angle >= startAngle || angle <= endAngle;
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
            float startZ = StartVertex.Position.Z;
            float endZ = EndVertex.Position.Z;
            float midZ = startZ + ((endZ - startZ) * (midAngle - StartAngle) / Sweep);

            MidPoint = new(midX, midY, midZ);
        }

        public override void UpdateBounds()
        {
            Bounds = Rect.Empty;

            if (_arc is not null)
            {
                var samplePoints = _arc.ToPolyline2D(5).Vertexes;
                foreach (var vertex in samplePoints)
                {
                    Bounds = Rect.Union(Bounds, new System.Windows.Point(vertex.Position.X, vertex.Position.Y));
                }
            }
        }
        #endregion
    }
}
