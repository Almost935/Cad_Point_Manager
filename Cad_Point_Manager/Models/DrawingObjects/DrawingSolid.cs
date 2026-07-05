using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using netDxf.Entities;
using PdfSharpCore.Drawing;
using SharpDX;
using SharpDX.Direct2D1;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingSolid : DrawingObject
    {
        #region Fields
        #endregion

        #region Properties
        public Vector3 Point1 { get; set; }
        public Vector3 Point2 { get; set; }
        public Vector3 Point3 { get; set; }
        public Vector3 Point4 { get; set; }
        public List<SolidVertex> Vertices { get; } = [];
        public int StartVertexIndex { get; set; }
        public int EndVertexIndex { get; set; }
        #endregion

        #region Constructors
        public DrawingSolid(Solid solid, ObjectLayer layer, Vector4 objectColor, ColorType colorType, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingSolid;
            EntityObject = solid;
            Layer = layer;
            ObjectColor = objectColor;
            ColorType = colorType;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock = block;

            UpdateColor();
            UpdateData();
        }
        #endregion

        #region Methods
        public override void UpdateData()
        {
            if (EntityObject is Solid solid)
            {
                Point1 = solid.FirstVertex.ToSharpDXVector3();
                Point2 = solid.SecondVertex.ToSharpDXVector3();
                Point3 = solid.FourthVertex.ToSharpDXVector3(); // Note: AutoCAD's SOLID entity defines the vertices in a specific order, where the third vertex is actually the fourth point of the solid.
                Point4 = solid.ThirdVertex.ToSharpDXVector3();
            }
            else
            {
                throw new ArgumentException("entity must be of type Solid");
            }
        }
        public override void DrawToD2dDeviceContext(DeviceContext1 deviceContext, Factory2 factory, Brush brush, float thickness, StrokeStyle1 strokeStyle)
        {

        }
        public override void DrawToPdf(XGraphics gfx, System.Windows.Media.Matrix worldToPdf, XPen pen)
        {

        }
        public override double DistanceToPoint(System.Windows.Point point)
        {
            return 0;
        }
        public override void UpdateBounds()
        {

        }

        public override void MouseEnter()
        {

        }
        public override void MouseLeave()
        {

        }
        public override void Select()
        {

        }
        public override void Deselect()
        {

        }

        public void UpdateVertices(uint layerId, uint objectId)
        {
            Vertices.Clear();

            // DXF SOLID can represent a triangle
            if (Point3 == Point4)
            {
                Vertices.Add(new(Point1, layerId, objectId));
                Vertices.Add(new(Point2, layerId, objectId));
                Vertices.Add(new(Point3, layerId, objectId));
                return;
            }

            // Bowtie/self-intersecting solid
            if (MathHelpers.LineSegmentsIntersect(Point1, Point4, Point2, Point3))
            {
                UseBowtieSolid(layerId, objectId);
                return;
            }

            // Normal quadrilateral
            float a1 = SignedArea2D(Point1, Point2, Point3);
            float a2 = SignedArea2D(Point1, Point3, Point4);

            bool optionAValid = MathF.Sign(a1) == MathF.Sign(a2);

            float b1 = SignedArea2D(Point1, Point2, Point4);
            float b2 = SignedArea2D(Point2, Point3, Point4);

            bool optionBValid = MathF.Sign(b1) == MathF.Sign(b2);

            if (optionAValid && !optionBValid)
            {
                UseDiagonal13(layerId, objectId);
            }
            else if (optionBValid && !optionAValid)
            {
                UseDiagonal24(layerId, objectId);
            }
            else
            {
                // Fallback
                float diag13 = Vector3.DistanceSquared(Point1, Point3);

                float diag24 = Vector3.DistanceSquared(Point2, Point4);

                if (diag13 <= diag24)
                {
                    UseDiagonal13(layerId, objectId);
                }
                else
                {
                    UseDiagonal24(layerId, objectId);
                }
            }
        }
        private void UseDiagonal13(uint layerId, uint objectId)
        {
            Vertices.Add(new(Point1, layerId, objectId));
            Vertices.Add(new(Point2, layerId, objectId));
            Vertices.Add(new(Point3, layerId, objectId));

            Vertices.Add(new(Point1, layerId, objectId));
            Vertices.Add(new(Point3, layerId, objectId));
            Vertices.Add(new(Point4, layerId, objectId));
        }

        private void UseDiagonal24(uint layerId, uint objectId)
        {
            Vertices.Add(new(Point1, layerId, objectId));
            Vertices.Add(new(Point2, layerId, objectId));
            Vertices.Add(new(Point4, layerId, objectId));

            Vertices.Add(new(Point2, layerId, objectId));
            Vertices.Add(new(Point3, layerId, objectId));
            Vertices.Add(new(Point4, layerId, objectId));
        }
        private void UseBowtieSolid(uint layerId, uint objectId)
        {
            var i = MathHelpers.GetIntersection(Point1, Point4, Point2, Point3);

            var intersection = new Vector3(i.X, i.Y, 0);

            // triangle 1
            Vertices.Add(new(Point1, layerId, objectId));
            Vertices.Add(new(Point2, layerId, objectId));
            Vertices.Add(new(intersection, layerId, objectId));

            // triangle 2
            Vertices.Add(new(Point3, layerId, objectId));
            Vertices.Add(new(Point4, layerId, objectId));
            Vertices.Add(new(intersection, layerId, objectId));
        }

        private static float SignedArea2D(Vector3 a, Vector3 b, Vector3 c)
        {
            return
                (b.X - a.X) * (c.Y - a.Y) -
                (b.Y - a.Y) * (c.X - a.X);
        }
        #endregion
    }
}
