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

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingSolid : DrawingObject
    {
        #region Fields
        #endregion

        #region Properties
        public Vector2d Point1 { get; set; }
        public Vector2d Point2 { get; set; }
        public Vector2d Point3 { get; set; }
        public Vector2d Point4 { get; set; }
        public List<SolidVertex> Vertices { get; } = [];
        public int StartVertexIndex { get; set; }
        public int EndVertexIndex { get; set; }
        #endregion

        #region Constructors
        public DrawingSolid(Solid solid, ObjectLayer layer, Vector4 objectColor, ColorType colorType,
            LineType lineType, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingSolid;
            EntityObject = solid;
            Layer = layer;
            ObjectColor = objectColor;
            ColorType = colorType;
            LineType = lineType;
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
                Point1 = solid.FirstVertex.ToVector2d();
                Point2 = solid.SecondVertex.ToVector2d();
                Point3 = solid.FourthVertex.ToVector2d();
                Point4 = solid.ThirdVertex.ToVector2d();
            }
            else
            {
                throw new ArgumentException("entity must be of type Solid");
            }
        }
        public override void DrawToPdf(XGraphics gfx, System.Windows.Media.Matrix worldToPdf, XPen pen)
        {

        }
        public override double DistanceToPoint(Vector2d point, MsdfAtlas atlas = null)
        {
            double minDistanceSquared = double.MaxValue;

            for (int i = 0; i + 2 < Vertices.Count; i += 3)
            {
                var a = Vertices[i].Position;
                var b = Vertices[i + 1].Position;
                var c = Vertices[i + 2].Position;

                if (PointInTriangle(point, a, b, c))
                    return 0;

                minDistanceSquared = Math.Min(
                    minDistanceSquared, DistanceSquaredToSegment(point, a, b));

                minDistanceSquared = Math.Min(
                    minDistanceSquared, DistanceSquaredToSegment(point, b, c));

                minDistanceSquared = Math.Min(
                    minDistanceSquared, DistanceSquaredToSegment(point, c, a));
            }

            return minDistanceSquared == double.MaxValue
                ? double.MaxValue : Math.Sqrt(minDistanceSquared);
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

        public void UpdateVertices(uint layerId, uint objectId, Vector2d gpuRelative)
        {
            Vertices.Clear();

            // DXF SOLID can represent a triangle
            if (Point3 == Point4)
            {
                Vertices.Add(new(Point1.ToSharpDXVector2(), layerId, objectId));
                Vertices.Add(new(Point2.ToSharpDXVector2(), layerId, objectId));
                Vertices.Add(new(Point3.ToSharpDXVector2(), layerId, objectId));
                return;
            }

            // Bowtie/self-intersecting solid
            if (MathHelpers.LineSegmentsIntersect(Point1, Point4, Point2, Point3))
            {
                UseBowtieSolid(layerId, objectId, gpuRelative);
                return;
            }

            // Normal quadrilateral
            double a1 = SignedArea2D(Point1, Point2, Point3);
            double a2 = SignedArea2D(Point1, Point3, Point4);

            bool optionAValid = Math.Sign(a1) == Math.Sign(a2);

            double b1 = SignedArea2D(Point1, Point2, Point4);
            double b2 = SignedArea2D(Point2, Point3, Point4);

            bool optionBValid = Math.Sign(b1) == Math.Sign(b2);

            if (optionAValid && !optionBValid)
            {
                UseDiagonal13(layerId, objectId, gpuRelative);
            }
            else if (optionBValid && !optionAValid)
            {
                UseDiagonal24(layerId, objectId, gpuRelative);
            }
            else
            {
                // Fallback
                double diag13 = Vector2d.DistanceSquared(Point1, Point3);
                double diag24 = Vector2d.DistanceSquared(Point2, Point4);

                if (diag13 <= diag24)
                {
                    UseDiagonal13(layerId, objectId, gpuRelative);
                }
                else
                {
                    UseDiagonal24(layerId, objectId, gpuRelative);
                }
            }
        }

        private void UseDiagonal13(uint layerId, uint objectId, Vector2d gpuRelative)
        {
            Vertices.Add(new(Point1.ToGpuRelative(gpuRelative), layerId, objectId));
            Vertices.Add(new(Point2.ToGpuRelative(gpuRelative), layerId, objectId));
            Vertices.Add(new(Point3.ToGpuRelative(gpuRelative), layerId, objectId));

            Vertices.Add(new(Point1.ToGpuRelative(gpuRelative), layerId, objectId));
            Vertices.Add(new(Point3.ToGpuRelative(gpuRelative), layerId, objectId));
            Vertices.Add(new(Point4.ToGpuRelative(gpuRelative), layerId, objectId));
        }
        private void UseDiagonal24(uint layerId, uint objectId, Vector2d gpuRelative)
        {
            Vertices.Add(new(Point1.ToGpuRelative(gpuRelative), layerId, objectId));
            Vertices.Add(new(Point2.ToGpuRelative(gpuRelative), layerId, objectId));
            Vertices.Add(new(Point4.ToGpuRelative(gpuRelative), layerId, objectId));

            Vertices.Add(new(Point2.ToGpuRelative(gpuRelative), layerId, objectId));
            Vertices.Add(new(Point3.ToGpuRelative(gpuRelative), layerId, objectId));
            Vertices.Add(new(Point4.ToGpuRelative(gpuRelative), layerId, objectId));
        }
        private void UseBowtieSolid(uint layerId, uint objectId, Vector2d gpuRelative)
        {
            var intersection = MathHelpers.GetIntersection(Point1, Point4, Point2, Point3);

            // triangle 1
            Vertices.Add(new(Point1.ToGpuRelative(gpuRelative), layerId, objectId));
            Vertices.Add(new(Point2.ToGpuRelative(gpuRelative), layerId, objectId));
            Vertices.Add(new(intersection.ToGpuRelative(gpuRelative), layerId, objectId));

            // triangle 2
            Vertices.Add(new(Point3.ToGpuRelative(gpuRelative), layerId, objectId));
            Vertices.Add(new(Point4.ToGpuRelative(gpuRelative), layerId, objectId));
            Vertices.Add(new(intersection.ToGpuRelative(gpuRelative), layerId, objectId));
        }

        private static double SignedArea2D(Vector2d a, Vector2d b, Vector2d c)
        {
            return
                (b.X - a.X) * (c.Y - a.Y) -
                (b.Y - a.Y) * (c.X - a.X);
        }
        #endregion

        #region Static Methods
        private static bool PointInTriangle(
            Vector2d p, Vector2 a, Vector2 b, Vector2 c)
        {
            double d1 = Cross(p, a, b);
            double d2 = Cross(p, b, c);
            double d3 = Cross(p, c, a);

            bool hasNegative = d1 < 0 || d2 < 0 || d3 < 0;
            bool hasPositive = d1 > 0 || d2 > 0 || d3 > 0;

            return !(hasNegative && hasPositive);
        }

        private static double Cross(Vector2d p, Vector2 a, Vector2 b)
        {
            return (p.X - b.X) * (a.Y - b.Y) -
                   (a.X - b.X) * (p.Y - b.Y);
        }
        private static double DistanceSquaredToSegment(Vector2d p, Vector2 a, Vector2 b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;

            double lengthSquared = dx * dx + dy * dy;

            if (lengthSquared == 0)
            {
                double x = p.X - a.X;
                double y = p.Y - a.Y;
                return x * x + y * y;
            }

            double t = ((p.X - a.X) * dx +
                        (p.Y - a.Y) * dy) / lengthSquared;

            t = Math.Clamp(t, 0, 1);

            double closestX = a.X + t * dx;
            double closestY = a.Y + t * dy;

            double offsetX = p.X - closestX;
            double offsetY = p.Y - closestY;

            return offsetX * offsetX + offsetY * offsetY;
        }

        #endregion
    }
}
