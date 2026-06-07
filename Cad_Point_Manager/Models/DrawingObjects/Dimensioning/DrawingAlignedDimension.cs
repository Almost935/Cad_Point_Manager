using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;
using Cad_Point_Manager.Helpers;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using netDxf.Entities;
using PdfSharpCore.Drawing;
using SharpDX;
using SharpDX.Direct2D1;
using System.Diagnostics;
using System.Windows;
using Point = System.Windows.Point;

namespace Cad_Point_Manager.Models.DrawingObjects.Dimensioning
{
    public class DrawingAlignedDimension : DrawingDimension
    {
        #region Properties
        public AlignedDimension AlignedDimension { get; set; }
        #endregion

        #region Constructors
        public DrawingAlignedDimension(AlignedDimension alignedDimension, ObjectLayer layer, Vector4 objectcolor, ColorType colorType, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            EntityObject = alignedDimension;
            AlignedDimension = alignedDimension;
            Type = DrawingObjectType.DrawingDimension;
            DimensionType = DrawingDimensionType.Aligned;
            Layer = layer;
            ObjectColor = objectcolor;
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
            var insert = new Insert(AlignedDimension.Block);

            Debug.WriteLineIf(Layer.Name == "1: Dim_Testing", $"\n\n\nDim ColorType: {ColorType} ObjectColor: {ObjectColor} BlockColor: {BlockColor} Layer.Color: {Layer.Color}");

            _dimensionBlock = DxfHelpers.GetDrawingObject(insert, Layer, DxfHelpers.GetDrawingObjectColor(this), ColorType) as DrawingBlock;

            Debug.WriteLineIf(Layer.Name == "Dim_Testing", $"\n\n\n");

            UpdateBounds();
        }
        public override void UpdateBounds()
        {
            Bounds = Rect.Empty;

            foreach (var drawingObj in _dimensionBlock.DrawingObjects)
            {
                Bounds = Rect.Union(Bounds, drawingObj.Bounds);
            }
        }

        public override double DistanceToPoint(Point p)
        {
            throw new NotImplementedException();
        }

        public override void DrawToD2dDeviceContext(DeviceContext1 deviceContext, Factory2 factory, Brush brush, float thickness, StrokeStyle1 strokeStyle)
        {
            throw new NotImplementedException();
        }

        public override void DrawToPdf(XGraphics gfx, System.Windows.Media.Matrix worldToPdf, XPen pen)
        {

        }

        public override void MouseEnter()
        {
            throw new NotImplementedException();
        }
        public override void MouseLeave()
        {
            throw new NotImplementedException();
        }

        public override void Select()
        {
            throw new NotImplementedException();
        }
        public override void Deselect()
        {
            throw new NotImplementedException();
        }

        public override void UpdateGeometryVertices(uint layerId, uint objectId)
        {
            _lineVertices.Clear();

            _dimensionBlock.UpdateGeometryVertices(layerId, objectId);
            _lineVertices.AddRange(_dimensionBlock.LineVertices);
        }
        public override void UpdateTextVertices(ResCache resCache, uint layerId, SceneIdMap sceneIdMap, D3dStateBuffers stateBuffers)
        {
            _textVertices.Clear();

            _dimensionBlock.UpdateTextVertices(resCache, layerId, sceneIdMap, stateBuffers);
            _textVertices.AddRange(_dimensionBlock.TextVertices);
        }
        #endregion 
    }
}
