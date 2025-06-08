using Cad_Point_Manager.Models.HitTesting;
using netDxf.Entities;
using SharpDX;
using SharpDX.Direct2D1;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Point = System.Windows.Point;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public abstract class DrawingObject3D : HitTestableObject, INotifyPropertyChanged
    {
        #region Properties
        public DrawingObject3dType Type { get; set; }
        public ObjectLayer3D Layer { get; set; }
        public EntityObject EntityObject { get; set; }
        public Vector4 Color { get; set; }
        public DrawingObject3dColorType DrawingObject3DColorType { get; set; }
        public bool IsPartOfBlock { get; set; } = false;
        public DrawingBlock3D DrawingBlock3D { get; set; }
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Methods
        public override string ToString()
        {
            return Type.ToString();
        }

        public abstract void UpdateData(EntityObject entity);
        public abstract void DrawToD2dDeviceContext(DeviceContext1 deviceContext, Factory2 factory, Brush brush, float thickness, StrokeStyle1 strokeStyle);

        public void UpdateColor()
        {
            if (EntityObject.Color.IsByLayer) 
            { 
                DrawingObject3DColorType = DrawingObject3dColorType.ByLayer;
                Color = Layer.Color; 
            }
            else if (EntityObject.Color.IsByBlock) 
            {
                DrawingObject3DColorType = DrawingObject3dColorType.ByBlock;
                if (DrawingBlock3D is not null) { Color = DrawingBlock3D.Color; }
                else { Color = new(0, 0, 0, 1); } 
            }
            else 
            {
                DrawingObject3DColorType = DrawingObject3dColorType.ByObject;
                Color = new(EntityObject.Color.R / 255.0f, EntityObject.Color.G / 255.0f, EntityObject.Color.B / 255.0f, 1); 
            }

            if (Color.X == 1 && Color.Y == 1 && Color.Z == 1)
            {
                Color = new(0, 0, 0, 1);
            }
        }


        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
