using netDxf.Tables;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class ObjectLayer3D
    {
        public string Name { get; set; }
        public Vector4 Color { get; set; }
        public Layer DxfLayer { get; set; }
        public List<DrawingObject3D> DrawingObject3Ds { get; set; } = [];

        public ObjectLayer3D() { }

        public ObjectLayer3D(Layer layer)
        {
            Name = layer.Name;
            Color = new(layer.Color.R / 255.0f, layer.Color.G / 255.0f, layer.Color.B / 255.0f, 1);
            DxfLayer = layer;
        }
    }
}
