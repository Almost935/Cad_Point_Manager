using netDxf;
using netDxf.Entities;
using netDxf.Header;
using SharpDX.Direct2D1;
using System.Windows.Controls;
using System.Windows;
using Ellipse = netDxf.Entities.Ellipse;
using System.Diagnostics;
using Cad_Point_Manager.Models.DrawingObjects;
using Direct2DDxfViewer.Direct2DControl;
using Cad_Point_Manager.Helpers;

namespace Cad_Point_Manager.Services
{
    public class DxfService
    {
        public bool TryGetDxfDoc(string filepath, out DxfDocument dxfDoc)
        {
            dxfDoc = DxfDocument.Load(filepath);
            return true;
        }
        public bool TryGetExtentsFromDxfDoc(DxfDocument dxfDoc, out Rect extents)
        {
            if (dxfDoc is null)
            {
                extents = new();
                return false;
            }

            if (dxfDoc.DrawingVariables.TryGetCustomVariable("$EXTMIN", out HeaderVariable extMinHeaderVariable) &&
                dxfDoc.DrawingVariables.TryGetCustomVariable("$EXTMAX", out HeaderVariable extMaxHeaderVariable))
            {
                Vector3 extMin = (Vector3)extMinHeaderVariable.Value;
                Vector3 extMax = (Vector3)extMaxHeaderVariable.Value;

                extents = new(extMin.X, extMin.Y, extMax.X - extMin.X, extMax.Y - extMin.Y);
                return true;
            }
            else 
            {                 
                extents = new();
                return false;
            }
        }

        public Rect GetExtents(Dictionary<string, ObjectLayer> layers)
        {
            Rect extents = new();

            foreach (var layer in layers.Values)
            {
                foreach (var drawingObject in layer.DrawingObjects)
                {
                    extents = Rect.Union(extents, drawingObject.);
                }
            }

            return extents;
        }

        public Dictionary<string, ObjectLayer> LoadLayers(DxfDocument dxfDoc, DeviceContext1 deviceContext, Factory1 factory, ResourceCache resCache)
        {
            Dictionary<string, ObjectLayer> layers = [];

            foreach (var e in dxfDoc.Entities.All)
            {
                if (layers.TryGetValue(e.Layer.Name, out ObjectLayer objLayer))
                {
                    var drawingObject = DxfHelper.LoadObject(e, objLayer, factory, deviceContext, resCache);

                    if (drawingObject is not null)
                    {
                        drawingObject.InitializeGeometries();
                        objLayer.DrawingObjects.Add(drawingObject);
                    }
                }
                else
                {
                    objLayer = new(deviceContext, factory, resCache, e.Layer);
                    layers.Add(e.Layer.Name, objLayer);

                    var drawingObject = DxfHelper.LoadObject(e, objLayer, factory, deviceContext, resCache);
                    if (drawingObject is not null)
                    {
                        drawingObject.InitializeGeometries();
                        objLayer.DrawingObjects.Add(drawingObject);
                    }
                }
            }

            foreach (var layer in layers.Values)
            {
                layer.LoadGeometryGroup();
            }

            return layers;
        }

        public static (byte r, byte g, byte b, byte a) GetRGBAColor(netDxf.Tables.Layer layer)
        {
            byte r, g, b, a;

            if (layer.Color.R == 255 && layer.Color.G == 255 && layer.Color.B == 255)
            {
                r = g = b = 0; a = 255;
            }
            else
            {
                r = layer.Color.R; g = layer.Color.G; b = layer.Color.B; a = 255;
            }


            return (r, g, b, a);
        }
    }
}
