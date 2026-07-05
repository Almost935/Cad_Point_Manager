using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
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
        public float Length { get; set; }
        public float Width { get; set; }
        public List<DrawingSegment> DrawingSegments { get; } = [];
        public int StartVertexIndex { get; set; }
        public int EndVertexIndex { get; set; }
        #endregion

        #region Constructors
        public DrawingWidePolyline(Polyline2D polyline2D, ObjectLayer layer, Vector4 objectColor,
            ColorType colorType, float width, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingPolyline;

            EntityObject = polyline2D;
            Layer = layer;
            ObjectColor = objectColor;
            ColorType = colorType;
            Width = width;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock = block;

            UpdateColor();
            UpdateData();
        }
        public DrawingWidePolyline(Polyline3D polyline3D, ObjectLayer layer, Vector4 objectColor,
            ColorType colorType, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingPolyline;

            EntityObject = polyline3D;
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
            if (EntityObject is Polyline2D polyline2D)
            {
                IsClosed = polyline2D.IsClosed;
                Length = 0;

                foreach (var e in polyline2D.Explode())
                {
                    var segment = DxfHelpers.GetDrawingSegment(e, Layer, ObjectColor, ColorType, DrawingBlock);
                    DrawingSegments.Add(segment);
                    Length += segment.Length;
                }
            }
            else
            {
                throw new ArgumentException("entity must be of type Polyline2D or Polyline3D");
            }
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
        public override double DistanceToPoint(System.Windows.Point p)
        {
            return 1000;
        }
        public override void DrawToD2dDeviceContext(DeviceContext1 deviceContext, Factory2 factory, Brush brush, float thickness, StrokeStyle1 strokeStyle)
        {

        }
        public override void DrawToPdf(XGraphics gfx, System.Windows.Media.Matrix worldToPdf, XPen pen)
        {
        }
        public override bool GeometryInRect(Rect rect)
        {
            return false;
        }
        public override void UpdateVertices(ResCache resCache, uint layerId, uint objectId)
        {
            var vertices = WidenedGeometryRenderingHelpers.GetWidenedPolylineVertices(resCache, this, Width, out var widenedVertices);

            for (int i = 0; i < vertices.Count; i += 3) // Every three vertices represent a triangle
            {
                var p1 = vertices[i];
                var p2 = vertices[i + 1];
                var p3 = vertices[i + 2];

                SolidVertex sv1 = new(p1.ToSharpDXVector3(), layerId, objectId);
                SolidVertex sv2 = new(p2.ToSharpDXVector3(), layerId, objectId);
                SolidVertex sv3 = new(p3.ToSharpDXVector3(), layerId, objectId);

                SolidVertices.Add(sv1);
                SolidVertices.Add(sv2);
                SolidVertices.Add(sv3);
            }
        }
        #endregion
    }
}
