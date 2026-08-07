using Cad_Point_Manager.Models.DrawingObjects.HelperClasses;
using Cad_Point_Manager.Models.HitTesting;
using netDxf.Entities;
using PdfSharpCore.Drawing;
using SharpDX;
using SharpDX.Direct2D1;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public abstract class DrawingObject : HitTestableObject
    {
        #region Properties
        public DrawingObjectType Type { get; set; }
        public ObjectLayer Layer { get; set; }
        public EntityObject EntityObject { get; set; }
        public Vector4 ObjectColor { get; set; } = new Vector4(0, 0, 0, 1);
        public Vector4 BlockColor { get; set; } = new Vector4(0, 0, 0, 1);
        public ColorType ColorType { get; set; }
        public bool IsPartOfBlock { get; set; } = false;
        public DrawingBlock DrawingBlock { get; set; }
        public LineType LineType { get; set; }
        #endregion

        #region Methods
        public override string ToString()
        {
            return Type.ToString();
        }

        public abstract void UpdateData();
        public abstract void DrawToD2dDeviceContext(DeviceContext1 deviceContext, Factory2 factory, Brush brush, float thickness, StrokeStyle1 strokeStyle);
        public abstract void DrawToPdf(
            XGraphics gfx,
            System.Windows.Media.Matrix worldToPdf,
            XPen pen);

        public void UpdateColor()
        {
            if (DrawingBlock is not null)
            {
                if (DrawingBlock.ColorType == ColorType.ByLayer)
                {
                    BlockColor = DrawingBlock.Layer.Color;
                }
                else if (DrawingBlock.ColorType == ColorType.ByBlock)
                {
                    BlockColor = DrawingBlock.BlockColor;
                }
                else
                {
                    BlockColor = DrawingBlock.ObjectColor;
                }
            }
            else { BlockColor = new(0, 0, 0, 1); }
        }

        public Vector4 GetColor()
        {
            return ColorType switch
            {
                ColorType.ByLayer => Layer.Color,
                ColorType.ByBlock => BlockColor,
                _ => ObjectColor
            };
        }
        #endregion
    }
}
