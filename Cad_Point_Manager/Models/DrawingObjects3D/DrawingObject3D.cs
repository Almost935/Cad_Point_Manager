using Cad_Point_Manager.Controls.D3DControl;
using netDxf.Entities;
using SharpDX;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Point = System.Windows.Point;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public abstract class DrawingObject3D : INotifyPropertyChanged
    {
        #region Fields
        private List<Vertex> _vertices = [];
        #endregion

        #region Properties
        public List<Vertex> Vertices
        {
            get => _vertices;
            set
            {
                _vertices = value;
                OnPropertyChanged(nameof(Vertices));
            }
        }

        public DrawingObject3dType Type { get; set; }
        public ObjectLayer3D Layer { get; set; }
        public EntityObject EntityObject { get; set; }
        public Vector4 Color { get; set; }
        public Rect Bounds { get; set; } = Rect.Empty;
        public DrawingObject3dColorType DrawingObject3DColorType { get; set; }
        public bool IsPartOfBlock { get; set; } = false;
        public DrawingBlock3D DrawingBlock3D { get; set; }
        public bool IsSelected { get; set; } = false;
        public bool IsMouseOver { get; set; } = false;
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Methods
        public abstract void UpdateData(EntityObject entity);
        public abstract void UpdateBounds();
        public abstract bool HitTest(Vector2 point, float tolerance);

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
