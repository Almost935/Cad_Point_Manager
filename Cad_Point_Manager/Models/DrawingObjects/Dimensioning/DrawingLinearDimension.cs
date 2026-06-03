using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;
using Cad_Point_Manager.Helpers;
using netDxf.Entities;
using PdfSharpCore.Drawing;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cad_Point_Manager.Models.DrawingObjects.Dimensioning
{
    public class DrawingLinearDimension : DrawingDimension
    {
        #region Properties
        public LinearDimension LinearDimension { get; set; }
        #endregion

        #region Constructors
        public DrawingLinearDimension(LinearDimension linearDimension, ObjectLayer layer, ColorType colorType, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            EntityObject = linearDimension;
            LinearDimension = linearDimension;
            Type = DrawingObjectType.DrawingDimension;
            DimensionType = DrawingDimensionType.Linear;
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
            _drawingObjects.Clear();
            foreach (var e in LinearDimension.Block.Entities)
            {
                var obj = DxfHelpers.GetDrawingObject(e, Layer);
                if (obj is not null)
                {
                    _drawingObjects.Add(obj);
                }
            }
            UpdateBounds();
        }
        public override void UpdateBounds()
        {
            Bounds = Rect.Empty;

            foreach (var drawingObj in DrawingObjects)
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
            throw new NotImplementedException();
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

            foreach (var obj in DrawingObjects)
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
        public override void UpdateTextVertices(ResCache resCache, uint layerId, SceneIdMap sceneIdMap, D3dStateBuffers stateBuffers)
        {
            _textVertices.Clear();

            foreach (var obj in DrawingObjects)
            {
                if (obj is DrawingBlock block)
                {
                    block.UpdateTextVertices(resCache, layerId, sceneIdMap, stateBuffers);
                    _textVertices.AddRange(block.TextVertices);
                }
                if (obj is DrawingSText text)
                {
                    text.UpdateTextVertices(resCache, layerId, sceneIdMap, stateBuffers);
                    _textVertices.AddRange(text.TextVertices);
                }
                if (obj is DrawingMtext mtext)
                {
                    mtext.UpdateTextVertices(resCache, layerId, sceneIdMap, stateBuffers);

                    foreach (var row in mtext.MtextBlock.Rows)
                    {
                        foreach (var segment in row.Segments)
                        {
                            _textVertices.AddRange(segment.TextVertices);
                        }
                    }
                }
            }
        }
        #endregion
    }
}
