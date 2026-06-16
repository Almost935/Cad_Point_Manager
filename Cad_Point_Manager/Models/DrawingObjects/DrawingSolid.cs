using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using netDxf.Entities;
using PdfSharpCore.Drawing;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System.Windows;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingSolid : DrawingObject
    {
        #region Fields
        #endregion

        #region Properties
        #endregion

        #region Constructors
        public DrawingSolid(Solid solid, ObjectLayer layer, Vector4 objectColor, ColorType colorType, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingSolid;
            EntityObject = solid;
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
            if (EntityObject is Solid arc)
            {

            }
            else
            {
                throw new ArgumentException("entity must be of type Arc");
            }
        }
        public override void DrawToD2dDeviceContext(DeviceContext1 deviceContext, Factory2 factory, Brush brush, float thickness, StrokeStyle1 strokeStyle)
        {

        }
        public override void DrawToPdf(
            XGraphics gfx,
            System.Windows.Media.Matrix worldToPdf,
            XPen pen)
        {

        }
        public override double DistanceToPoint(System.Windows.Point point)
        {
            return 0;
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
        public override void Select()
        {
            
        }
        public override void Deselect()
        {

        }
        #endregion
    }
}
