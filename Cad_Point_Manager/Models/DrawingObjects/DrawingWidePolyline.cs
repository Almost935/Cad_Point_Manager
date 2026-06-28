using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Models.DrawingObjects.Dimensioning;
using netDxf.Entities;
using PdfSharpCore.Drawing;
using SharpDX;
using SharpDX.Direct2D1;
using System.Windows;
using Cad_Point_Manager.Extensions;
using System.Diagnostics;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingWidePolyline : DrawingGeometry
    {
        #region Properties
        public List<SolidVertex> SolidVertices { get; } = [];
        public float Thickness { get; }
        public bool IsClosed { get; set; }
        public float Length { get; set; }
        public List<DrawingSolid> DrawingSolids { get; } = [];
        #endregion

        #region Constructors
        public DrawingWidePolyline(Polyline2D polyline2D, ObjectLayer layer, float thickness, Vector4 objectColor, 
            ColorType colorType, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingPolyline;

            EntityObject = polyline2D;
            Layer = layer;
            Thickness = thickness;
            ObjectColor = objectColor;
            ColorType = colorType;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock = block;

            UpdateColor();
            UpdateData();
        }
        public DrawingWidePolyline(Polyline3D polyline3D, ObjectLayer layer, float thickness, Vector4 objectColor, 
            ColorType colorType, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingPolyline;

            EntityObject = polyline3D;
            Layer = layer;
            Thickness = thickness;
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
            if (EntityObject is Polyline2D polyline2d)
            {
                IsClosed = polyline2d.IsClosed;
                Length = 0;
                for (int i = 1; i < polyline2d.Vertexes.Count; i++) // i = 1 to skip the first vertex
                {
                    var preVertex = polyline2d.Vertexes[i - 1];
                    var vertex = polyline2d.Vertexes[i];
                    Length += preVertex.DistanceTo(vertex).ToFloat(); 

                    Vector3 direction = (vertex.Position - preVertex.Position).ToSharpDXVector3();
                    direction.Normalize();

                    Debug.WriteLine($"\npreVertex: {preVertex} vertex: {vertex}" +
                        $"\npreVertex.StartWidth: {preVertex.StartWidth} preVertex.EndWidth: {preVertex.EndWidth}" +
                        $"\nvertex.StartWidth: {vertex.StartWidth} vertex.EndWidth: {vertex.EndWidth}");
                }
            }
            else if (EntityObject is Polyline3D polyline3d)
            {
                IsClosed = polyline3d.IsClosed;
                Length = 0;
                for (int i = 1; i < polyline3d.Vertexes.Count; i++) // i = 1 to skip the first vertex
                {
                    var preVertex = polyline3d.Vertexes[i - 1];
                    var vertex = polyline3d.Vertexes[i];
                    Length += netDxf.Vector3.Distance(preVertex, vertex).ToFloat();
                   
                    Vector3 direction = (vertex - preVertex).ToSharpDXVector3();
                    direction.Normalize();

                    //Debug.WriteLine($"\npreVertex: {preVertex} vertex: {vertex}" +
                    //    $"\npreVertex.StartWidth: {preVertex.StartWidth} preVertex.EndWidth: {preVertex.EndWidth}" +
                    //    $"\nvertex.StartWidth: {vertex.StartWidth} vertex.EndWidth: {vertex.EndWidth}");
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

        }
        #endregion
    }
}
