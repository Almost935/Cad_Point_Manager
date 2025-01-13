 using Cad_Point_Manager.Controls.D3DControl;
using netDxf.Tables;
using SharpDX;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class ObjectLayer3D : INotifyPropertyChanged
    {
        #region Fields
        public bool _isVisible = true;
        #endregion

        #region Properties
        public string Name { get; set; }
        public Vector4 Color { get; set; }
        public Layer DxfLayer { get; set; }
        public List<DrawingObject3D> DrawingObject3Ds { get; set; } = [];
        public List<Vertex> Vertices { get; set; } = [];
        public List<DrawingText3D> DrawingText3Ds { get; set; } = [];

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                OnPropertyChanged();
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
                Vertices.AddRange(block.DrawingGeometryVerteces);
            }
            if (drawingObject3D is DrawingGeometry3D geometry)
            {
                Vertices.AddRange(geometry.Vertices);
            }
            if (drawingObject3D is DrawingText3D text)
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
