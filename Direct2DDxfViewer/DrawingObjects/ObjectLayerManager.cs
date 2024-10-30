using Direct2DDxfViewer.Direct2DControl;
using netDxf;
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
        private DeviceContext1 _deviceContext;
        private Factory1 _factory;
        private OldResourceCache _resourceCache;

        private DxfDocument _dxfDocument;
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
            foreach (var dxfLayer in _dxfDocument.Layers)
            {
                ObjectLayer objectLayer = new(dxfLayer);
                Layers.Add(dxfLayer.Name, objectLayer);
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
        public void UpdateDeviceDependentResources(DeviceContext1 newDeviceContext)
        {
            foreach (var layer in Layers.Values)
            {
                layer?.UpdateDeviceDependentResources(newDeviceContext);
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
