using Cad_Point_Manager.Common.Collections;
using netDxf.Tables;
using SharpDX;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class ObjectLayer : INotifyPropertyChanged
    {
        #region Fields
        private bool _isVisible = true;
        private Vector4 _color;
        #endregion

        #region Properties
        public string Name { get; set; }
        public Layer DxfLayer { get; set; }
        public BatchableObservableCollection<DrawingObject> DrawingObjects { get; set; } = [];
        public List<DrawingSText> DrawingText3Ds { get; set; } = [];
        public uint Id { get; set; }

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
        private ObjectLayer() { }

        public ObjectLayer(Layer layer)
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
        public void AddDrawingObject(DrawingObject drawingObject3D)
        {
            DrawingObjects.Add(drawingObject3D);

            if (drawingObject3D is DrawingBlock block)
            {
                foreach (var obj in block.DrawingObjects)
                {
                    if (obj is DrawingSText drawingText)
                    {
                        DrawingText3Ds.Add(drawingText);
                    }
                }
            }
            if (drawingObject3D is DrawingSText text)
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
