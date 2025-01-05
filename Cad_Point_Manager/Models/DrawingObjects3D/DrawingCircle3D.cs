using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.DrawingObjects;
using Cad_Point_Manager.Models.DrawingObjects3D;
using Cad_Point_Manager.Models.SerializableObjects;
using netDxf.Entities;
using netDxf.Tables;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System.Windows;
using static netDxf.Entities.HatchBoundaryPath;
using Ellipse = SharpDX.Direct2D1.Ellipse;

namespace Cad_Point_Manager.DrawingObjects
{
    public class DrawingCircle3D : DrawingCurve3D
    {
        #region Fields
        private Circle _circle => EntityObject as Circle;
        #endregion

        #region Properties
        public RawVector2 Center { get; set; }
        public float Circumference { get; set; }
        #endregion

        #region Constructor
        public DrawingCircle3D(Circle circle, ObjectLayer3D layer, bool isPartOfBlock = false, DrawingBlock3D block = null)
        {
            Type = DrawingObject3dType.DrawingArc3D;
            Layer = layer;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock3D = block;
            EntityObject = circle;

            UpdateColor();
            UpdateData(circle);
        }
        #endregion

        #region Methods
        public override void UpdateData(EntityObject entity)
        {
            if (entity is Circle circle)
            {
                Radius = (float)circle.Radius;
                StartAngle = 0;
                EndAngle = 360;
                Sweep = EndAngle - StartAngle;
                RadiusPoint = new Vector3((float)circle.Center.X, (float)circle.Center.Y, (float)circle.Center.Z);
                Length = (float)((Sweep / 360) * (2 * Math.PI * Radius));

                UpdateBounds();
                UpdateVertices(circle);
            }
            else
            {
                throw new ArgumentException("entity must be of type Circle");
            }
        }

        public override void UpdateVertices(EntityObject entity)
        {
            if (entity is Circle circle)
            {
                Vertices.Clear();

                //NumberOfSegments = CalculateArcSegments(Radius, Sweep);
                NumberOfSegments = CalculateSegments(Radius, Sweep);

                var vertices = circle.ToPolyline2D(NumberOfSegments).Vertexes;

                for (int i = 0; i < vertices.Count; i++)
                {
                    if (i == vertices.Count - 1)
                    {
                        Vertex start = new(
                            new Vector3((float)vertices[i].Position.X, (float)vertices[i].Position.Y, 0),
                            Color);
                        Vertex end = new(
                            new Vector3((float)vertices[0].Position.X, (float)vertices[0].Position.Y, 0),
                            Color);

                        Vertices.Add(start);
                        Vertices.Add(end);

                        break;
                    }

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

        public override void UpdateBounds()
        {
            Bounds = Rect.Empty;

            if (_circle is not null)
            {
                var samplePoints = _circle.ToPolyline2D(4).Vertexes;
                foreach (var vertex in samplePoints)
                {
                    Bounds = Rect.Union(Bounds, new System.Windows.Point(vertex.Position.X, vertex.Position.Y));
                }
            }
        }

        public override bool HitTest(System.Windows.Point point, float tolerance)
        {
            return MathHelpers.IsPointOnCircle(point.X, point.Y, RadiusPoint.X, RadiusPoint.Y, Radius, tolerance);
        }

        public override double DistanceToPoint(System.Windows.Point point)
        {
            // Calculate the distance from the point to the center of the circle
            double dx = point.X - RadiusPoint.X;
            double dy = point.Y - RadiusPoint.Y;
            double distanceToCenter = Math.Sqrt(dx * dx + dy * dy);

            // Calculate the distance to the circle
            double distanceToCircle = distanceToCenter - Radius;

            return distanceToCircle;
        }
        #endregion
    }
}
