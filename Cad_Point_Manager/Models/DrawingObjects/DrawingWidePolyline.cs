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
using System.Windows;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingWidePolyline : DrawingGeometry
    {
        #region Properties
        public Polyline2D Polyline2D => EntityObject as Polyline2D;
        public List<SolidVertex> SolidVertices { get; } = [];
        public bool IsClosed { get; set; }
        public double Length { get; set; }
        public double Width { get; set; }
        public List<DrawingSegment> DrawingSegments { get; } = [];
        #endregion

        #region Constructors
        public DrawingWidePolyline(
            Polyline2D polyline2D, ObjectLayer layer, Vector4 objectColor, ColorType colorType,
            LineType lineType, double width, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingPolyline;

            EntityObject = polyline2D;
            Layer = layer;
            ObjectColor = objectColor;
            ColorType = colorType;
            LineType = lineType;
            Width = width;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock = block;

            UpdateColor();
            UpdateData();
        }
        #endregion

        #region Methods
        public override void UpdateData()
        {
            if (EntityObject is not Polyline2D polyline2D)
                throw new ArgumentException("Entity must be a Polyline2D");

            DrawingSegments.Clear();

            IsClosed = polyline2D.IsClosed;
            Length = 0.0;

            var vertices = polyline2D.Vertexes;

            if (vertices.Count > 0)
            {
                Start = vertices[0].Position.ToVector2d();
                End = vertices[^1].Position.ToVector2d();
            }

            foreach (var entity in polyline2D.Explode())
            {
                var segment = DxfHelpers.GetDrawingSegment(
                    entity, Layer, ObjectColor, ColorType, LineType, DrawingBlock);

                if (segment is null)
                    continue;

                DrawingSegments.Add(segment);
                Length += segment.Length;
            }

            UpdateBounds();
        }

        public override void UpdateBounds()
        {
            Rect bounds = Rect.Empty;

            foreach (var segment in DrawingSegments)
            {
                if (segment.Bounds.IsEmpty)
                    continue;

                bounds.Union(segment.Bounds);
            }

            if (!bounds.IsEmpty)
                bounds.Inflate(Width * 0.5, Width * 0.5);

            Bounds = bounds;
        }

        public override double DistanceToPoint(Vector2d p, MsdfAtlas atlas = null)
        {
            double minDistance = double.MaxValue;

            foreach (var segment in DrawingSegments)
            {
                double distance = segment.DistanceToPoint(p);

                if (distance < minDistance)
                {
                    minDistance = distance;

                    if (minDistance <= Width * 0.5)
                        return 0;
                }
            }

            if (minDistance == double.MaxValue)
                return double.MaxValue;

            return Math.Max(0, minDistance - Width * 0.5);
        }


        public override void DrawToPdf(XGraphics gfx, System.Windows.Media.Matrix worldToPdf, XPen pen)
        {
        }
        public override bool GeometryInRect(Rect rect)
        {
            return false;
        }
        public override void UpdateVertices(
            ResCache resCache, uint layerId, uint objectId, uint lineTypeId, Vector2d renderOrigin)
        {
            SolidVertices.Clear();

            var vertices =
                WidenedGeometryRenderingHelpers.GetWidenedPolylineVertices(
                    resCache, this, Width, renderOrigin);

            foreach (var p in vertices)
            {
                SolidVertices.Add(new(p.ToSharpDXVector2(), layerId, objectId));
            }
        }
        #endregion
    }
}
