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
        //public bool ColorByLayer { get; set; }
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
            if (DrawingBlock is not null) { BlockColor = DrawingBlock.ObjectColor; }
            else { BlockColor = new(0, 0, 0, 1); }

            //if (ColorType == ColorType.ByLayer)
            //{
            //    ObjectColor = Layer.Color;
            //}
            //else if (ColorType == ColorType.ByBlock)
            //{
            //    if (DrawingBlock is not null) { ObjectColor = DrawingBlock.ObjectColor; }
            //    else { ObjectColor = new(0, 0, 0, 1); }
            //}
            //else
            //{
            //    Color = new(EntityObject.Color.R / 255.0f, EntityObject.Color.G / 255.0f, EntityObject.Color.B / 255.0f, 1);
            //}

            if (ObjectColor.X == 1 && ObjectColor.Y == 1 && ObjectColor.Z == 1)
            {
                ObjectColor = new(0, 0, 0, 1);
            }
        }
        #endregion
    }
}
