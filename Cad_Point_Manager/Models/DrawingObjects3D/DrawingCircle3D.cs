using Cad_Point_Manager.Controls.D3DControl;
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
        public float Radius { get; set; }
        public RawVector2 Center { get; set; }
        public List<Vertex> IntermediateVertices { get; set; } = [];
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
        #endregion
    }
}
