using netDxf.Entities;
using PdfSharpCore.Drawing;
using SharpDX.Direct2D1;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingSpline : DrawingGeometry
    {
        #region Fields
        private int _polylineApproximationPrecision = 1000;

        private Polyline2D _polyline;
        #endregion

        #region Properties
        public DrawingPolyline PolylineApproximation { get; set; }
        #endregion

        #region Constructors
        public DrawingSpline(Spline spline, ObjectLayer layer, ColorType colorType, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingSpline;
            EntityObject = spline;
            Layer = layer;
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
            if (EntityObject is Spline spline)
            {
                _polyline = spline.ToPolyline2D(_polylineApproximationPrecision);
                PolylineApproximation = new(_polyline, Layer, ColorType, isPartOfBlock: IsPartOfBlock, block: DrawingBlock);
            }
            else
            {
                throw new ArgumentException("entity must be of type Spline");
            }
        }
        public override void DrawToD2dDeviceContext(DeviceContext1 deviceContext, Factory2 factory, Brush brush, float thickness, StrokeStyle1 strokeStyle)
        {
            PolylineApproximation.DrawToD2dDeviceContext(deviceContext, factory, brush, thickness, strokeStyle);
        }
        public override void DrawToPdf(
           XGraphics gfx,
           System.Windows.Media.Matrix worldToPdf,
           XPen pen)
        {
            foreach (var segment in PolylineApproximation.DrawingSegments)
            {
                segment.DrawToPdf(gfx, worldToPdf, pen);
            }
        }

        public override void UpdateVertices(uint layerId, uint objectId)
        {
            PolylineApproximation.UpdateVertices(layerId, objectId);
            Vertices = PolylineApproximation.Vertices;
        }

        public override void UpdateBounds()
        {
            PolylineApproximation.UpdateBounds();
            Bounds = PolylineApproximation.Bounds;
        }

        public override double DistanceToPoint(System.Windows.Point p)
        {
            return PolylineApproximation.DistanceToPoint(p);
        }

        public override bool GeometryInRect(System.Windows.Rect rect)
        {
            return PolylineApproximation.GeometryInRect(rect);
        }
        #endregion
    }
}
