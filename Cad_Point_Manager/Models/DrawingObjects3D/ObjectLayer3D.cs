using netDxf.Tables;
using SharpDX;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class ObjectLayer3D : INotifyPropertyChanged
    {
        #region Fields
        private bool _isVisible = true;
        private Vector4 _color;
        #endregion
         
        #region Properties
        public string Name { get; set; }
        public Layer DxfLayer { get; set; }
        public List<DrawingObject3D> DrawingObject3Ds { get; set; } = [];
        public List<DrawingSText3D> DrawingText3Ds { get; set; } = [];

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }
        public Vector4 Color
        {
            get => _color;
            set
            {
                _color = value;
                OnPropertyChanged(nameof(Color));
            }
        }
        #endregion

        #region Constructors
        private ObjectLayer3D() { }

        public ObjectLayer3D(Layer layer)
        {
            Name = layer.Name;
            Color = new(layer.Color.R / 255.0f, layer.Color.G / 255.0f, layer.Color.B / 255.0f, 1);
            DxfLayer = layer;
        }
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Methods
        public void AddDrawingObject(DrawingObject3D drawingObject3D)
        {
            DrawingObject3Ds.Add(drawingObject3D);

            if (drawingObject3D is DrawingBlock3D block)
            {
                foreach (var obj in block.DrawingObjects)
                {
                    if (obj is DrawingSText3D drawingText)
                    {
                        DrawingText3Ds.Add(drawingText);
                    }
                }
            }
            if (drawingObject3D is DrawingSText3D text)
            {
                DrawingText3Ds.Add(text);
            }
        }


        public override string ToString()
        {
            return Name;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
