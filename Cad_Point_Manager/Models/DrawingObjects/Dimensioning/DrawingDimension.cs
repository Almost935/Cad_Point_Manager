using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;
using Cad_Point_Manager.Helpers;
using netDxf.Entities;
using PdfSharpCore.Drawing;
using SharpDX;
using SharpDX.Direct2D1;
using System.Diagnostics;
using System.Windows;

namespace Cad_Point_Manager.Models.DrawingObjects.Dimensioning
{
    public class DrawingDimension : DrawingObject
    {
        #region Fields
        protected DrawingBlock _dimensionBlock;
        protected readonly List<LineVertex> _lineVertices = [];
        protected readonly List<TextVertex> _textVertices = [];
        #endregion

        #region Properties
        public Dimension Dimension { get; set; }
        public DrawingDimensionType DimensionType { get; set; }
        public int StartLineVertexIndex { get; set; }
        public int EndLineVertexIndex { get; set; }
        public int StartTextVertexIndex { get; set; }
        public int EndTextVertexIndex { get; set; }

        public DrawingBlock DimensionBlock => _dimensionBlock;
        public IReadOnlyList<LineVertex> LineVertices => _lineVertices;
        public IReadOnlyList<TextVertex> TextVertices => _textVertices;
        #endregion

        #region Constructors
        public DrawingDimension(Dimension dimension, ObjectLayer layer, Vector4 objectcolor, ColorType colorType, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            EntityObject = dimension;
            Dimension = dimension;
            Type = DrawingObjectType.DrawingDimension;
            DimensionType = DrawingDimensionType.Linear;
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
            _dimensionBlock = DxfHelpers.GetDrawingObject(new Insert(Dimension.Block), Layer, ObjectColor, ColorType, isPartOfDimension: true) as DrawingBlock;
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

        public override double DistanceToPoint(System.Windows.Point p)
        {
            throw new NotImplementedException();
        }

        public override void DrawToD2dDeviceContext(DeviceContext1 deviceContext, Factory2 factory, Brush brush, float thickness, StrokeStyle1 strokeStyle)
        {
            throw new NotImplementedException();
        }

        public override void DrawToPdf(XGraphics gfx, System.Windows.Media.Matrix worldToPdf, XPen pen)
        {
            DimensionBlock.DrawToPdf(gfx, worldToPdf, pen);
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

        public void UpdateGeometryVertices(uint layerId, uint objectId)
        {
            _lineVertices.Clear();

            foreach (var obj in _dimensionBlock.DrawingObjects)
            {
                if (obj is DrawingBlock block)
                {
                    block.UpdateGeometryVertices(layerId, objectId);
                    _lineVertices.AddRange(block.LineVertices);
                }
                if (obj is DrawingGeometry geometry)
                {
                    geometry.UpdateVertices(layerId, objectId);
                    _lineVertices.AddRange(geometry.Vertices);
                }
            }
        }
        public void UpdateTextVertices(ResCache resCache, uint layerId, SceneIdMap sceneIdMap, D3dStateBuffers stateBuffers)
        {
            _textVertices.Clear();

            foreach (var obj in _dimensionBlock.DrawingObjects)
            {
                if (obj is DrawingBlock block)
                {
                    block.UpdateTextVertices(resCache, layerId, sceneIdMap, stateBuffers);
                    _textVertices.AddRange(block.TextVertices);
                }
                if (obj is DrawingSText text)
                {
                    text.UpdateVertices(resCache, layerId, sceneIdMap, stateBuffers);
                    _textVertices.AddRange(text.TextVertices);
                }
                if (obj is DrawingMtext mtext)
                {
                    mtext.UpdateVertices(resCache, layerId, sceneIdMap, stateBuffers);

                    foreach (var segment in mtext.Segments)
                    {
                        _textVertices.AddRange(segment.TextVertices);
                    }
                }
            }
        }
        #endregion
    }
}
