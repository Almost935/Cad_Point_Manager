using Direct2DDxfViewer.Direct2DControl;
using Direct2DDXFViewer.Helpers;
using netDxf;
using netDxf.Entities;
using netDxf.Units;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Direct2DDXFViewer.DrawingObjects
{
    public class ObjectLayerManager : IDisposable, INotifyPropertyChanged
    {
        #region Fields
        private bool _disposed = false;

        private DxfDocument _dxfDocument;
        private Rect _extents;
        #endregion

        #region Properties
        public DxfDocument DxfDocument
        {
            get { return _dxfDocument; }
            set
            {
                _dxfDocument = value;
                OnPropertyChanged(nameof(DxfDocument));
            }
        }
        public Rect Extents
        {
            get { return _extents; }
            set
            {
                _extents = value;
                OnPropertyChanged(nameof(Extents));
            }
        }

        public Dictionary<string, ObjectLayer> Layers { get; set; } = new();
        public List<DrawingObject> DrawingObjects => Layers.Values.SelectMany(layer => layer.DrawingObjects).ToList();
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Methods
        public void LoadDxfDocument(DxfDocument dxfDocument)
        {
            _dxfDocument = dxfDocument;
            Layers.Clear();
            Extents = new();

            Extents = DxfHelpers.GetExtentsFromHeader(DxfDocument);

            foreach (var e in _dxfDocument.Entities.All)
            {
                var layer = GetLayer(e.Layer);
                var obj = DxfHelpers.GetDrawingObject(e, layer);
                layer.DrawingObjects.Add(obj);
            }
        }
        public ObjectLayer GetLayer(netDxf.Tables.Layer dxfLayer)
        {
            if (Layers.TryGetValue(dxfLayer.Name, out ObjectLayer layer)) { return layer; }
            else
            {
                ObjectLayer objectLayer = new(dxfLayer);
                Layers.Add(dxfLayer.Name, objectLayer);
                return objectLayer;
            }
        }

        public void InitializeDeviceResources(DeviceContext1 deviceContext, Factory1 factory, ResourceCache resCache)
        {
            foreach (var layer in Layers.Values)
            {
                layer.InitializeResources(resCache);
            }
        }
        public void UpdateDeviceDependentResources(DeviceContext1 newDeviceContext, ResourceCache resCache)
        {
            foreach (var layer in Layers.Values)
            {
                layer?.UpdateDeviceDependentResources(resCache);
            }
        }
        public void UpdateDeviceIndependentResources(Factory1 factory, ResourceCache resCache)
        {
            foreach (var layer in Layers.Values)
            {
                layer?.UpdateDeviceIndependentResources(resCache);
            }
        }

        public List<DrawingObject> GetDrawingObjectsinRect(Rect rect)
        {
            List<DrawingObject> drawingObjects = [];
            foreach (var layer in Layers.Values)
            {
                foreach (var obj in layer.DrawingObjects)
                {
                    if (obj.DrawingObjectIsInRect(rect))
                    {
                        drawingObjects.Add(obj);
                    }
                }
            }
            return drawingObjects;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // Dispose managed resources
                if (Layers != null)
                {
                    foreach (var layer in Layers.Values)
                    {
                        layer?.Dispose();
                    }
                    Layers.Clear();
                }
            }

            // Free unmanaged resources if any

            _disposed = true;
        }

        ~ObjectLayerManager()
        {
            Dispose(false);
        }
        #endregion
    }
}
