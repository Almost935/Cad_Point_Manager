using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class LayerManager
    {
        public SortedDictionary<string, ObjectLayer3D> Layers { get; set; } = [];

        public ObjectLayer3D GetLayer(string layerName)
        {
            ObjectLayer3D layer;
            var layerExists = Layers.TryGetValue(layerName, out layer);
            if (layerExists)
            {
                return layer;
            }
            else
            {
                layer = new();
                layer.LayerName = layerName;
                Layers.Add(layerName, layer);
                return layer;
            }
        }
        public void AddObjectToLayer(string layerName, DrawingObject3D drawingObject)
        {
            var layer = GetLayer(layerName);
            layer.DrawingObject3Ds.Add(drawingObject);
        }

        public void RemoveObjectFromLayer(string layerName, DrawingObject3D drawingObject)
        {
            var layer = GetLayer(layerName);
            layer.DrawingObject3Ds.Remove(drawingObject);
        }

        public void ClearLayer(string layerName)
        {
            var layer = GetLayer(layerName);
            layer.DrawingObject3Ds.Clear();
        }

        public void ClearAllLayers()
        {
            Layers.Clear();
        }
    }
}
