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
        public Vector4 Color { get; set; }
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
            if (ColorType == ColorType.ByLayer)
            {
                Color = Layer.Color;
            }
            else if (ColorType == ColorType.ByBlock)
            {
                if (DrawingBlock is not null) { Color = DrawingBlock.Color; }
                else { Color = new(0, 0, 0, 1); }
            }
            //else
            //{
            //    Color = new(EntityObject.Color.R / 255.0f, EntityObject.Color.G / 255.0f, EntityObject.Color.B / 255.0f, 1);
            //}

            if (Color.X == 1 && Color.Y == 1 && Color.Z == 1)
            {
                Color = new(0, 0, 0, 1);
            }
        }
        #endregion
    }
}
