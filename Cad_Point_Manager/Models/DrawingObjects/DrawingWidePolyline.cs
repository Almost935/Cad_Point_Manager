using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Models.DrawingObjects.Dimensioning;
using DocumentFormat.OpenXml.Drawing;
using netDxf.Entities;
using PdfSharpCore.Drawing;
using SharpDX;
using SharpDX.Direct2D1;
using System.Diagnostics;
using System.Windows;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingWidePolyline : DrawingGeometry
    {
        #region Properties
        public List<SolidVertex> SolidVertices { get; } = [];
        public bool IsClosed { get; set; }
        public float Length { get; set; }
        public List<DrawingSolid> DrawingSolids { get; } = [];
        public int StartVertexIndex { get; set; }
        public int EndVertexIndex { get; set; }
        #endregion

        #region Constructors
        public DrawingWidePolyline(Polyline2D polyline2D, ObjectLayer layer, Vector4 objectColor,
            ColorType colorType, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingPolyline;

            EntityObject = polyline2D;
            Layer = layer;
            ObjectColor = objectColor;
            ColorType = colorType;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock = block;

            UpdateColor();
            UpdateData();

            Debug.WriteLine($"\nLayer.Name: {Layer.Name}");
            for (int i = 1; i < polyline2D.Vertexes.Count; i++) // i = 1 to skip the first vertex
            {
                var preVertex = polyline2D.Vertexes[i - 1];
                var vertex = polyline2D.Vertexes[i];

                Debug.WriteLine($"preVertex: {preVertex} vertex: {vertex}" +
                    $"\npreVertex.StartWidth: {preVertex.StartWidth} preVertex.EndWidth: {preVertex.EndWidth}" +
                    $"\nvertex.StartWidth: {vertex.StartWidth} vertex.EndWidth: {vertex.EndWidth}");
            }
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
                for (int i = 1; i < polyline2D.Vertexes.Count; i++) // i = 1 to skip the first vertex
                {
                    var startVertex = polyline2D.Vertexes[i - 1];
                    var endVertex = polyline2D.Vertexes[i];
                    var startPosition = startVertex.Position.ToSharpDXVector3();
                    var endPosition = endVertex.Position.ToSharpDXVector3();
                    Length += startPosition.GetDistance2D(endPosition);

                    Vector3 direction = endPosition - startPosition;
                    direction.Normalize();
                    Vector3 normal = new(-direction.Y, direction.X, 0);

                    float startWidth = startVertex.StartWidth.ToFloat();
                    float endWidth = endVertex.EndWidth.ToFloat();

                    Vector3 startOffset = normal * (startWidth * 0.5f);
                    Vector3 endOffset = normal * (endWidth * 0.5f);

                    Vector3 topLeft = startPosition + startOffset;
                    Vector3 bottomLeft = startPosition - startOffset;

                    Vector3 topRight = endPosition + endOffset;
                    Vector3 bottomRight = endPosition - endOffset;

                    Solid solid = new(topLeft.ToNetDxfVector2(), topRight.ToNetDxfVector2(), bottomLeft.ToNetDxfVector2(), bottomRight.ToNetDxfVector2());
                    DrawingSolid drawingSolid = new(solid, Layer, ObjectColor, ColorType); 
                    DrawingSolids.Add(drawingSolid);
                }

                foreach (var e in polyline2D.Explode())
                {

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
            foreach (var solid in DrawingSolids)
            {
                solid.DrawToD2dDeviceContext(deviceContext, factory, brush, thickness, strokeStyle);
            }
        }
        public override void DrawToPdf(XGraphics gfx, System.Windows.Media.Matrix worldToPdf, XPen pen)
        {
        }
        public override bool GeometryInRect(Rect rect)
        {
            return false;
        }
        public override void UpdateVertices(uint layerId, uint objectId)
        {
            foreach (var solid in DrawingSolids)
            {
                solid.UpdateVertices(layerId, objectId);
                SolidVertices.AddRange(solid.Vertices);
            }
        }
        #endregion
    }
}
