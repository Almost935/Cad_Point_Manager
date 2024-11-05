
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
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Controls.D2DControl;

namespace Cad_Point_Manager.DrawingObjects
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
        public bool DxfLoaded { get; set; } = false;
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Methods
        public void LoadDxfDocument(DxfDocument dxfDocument)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            DxfLoaded = false;

            _dxfDocument = dxfDocument;
            Layers.Clear();
            Extents = new();

            Extents = DxfHelpers.GetExtentsFromHeader(DxfDocument);

            foreach (var e in _dxfDocument.Entities.All)
            {
                var layer = GetLayer(e.Layer);
                var obj = DxfHelpers.GetDrawingObject(e, layer);
                if (obj is not null)
                {
                    layer.DrawingObjects.Add(obj);
                }
            }
            DxfLoaded = true;

            stopwatch.Stop();
            Debug.WriteLine($"LoadDxfDocument: {stopwatch.ElapsedMilliseconds} ms");
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

        public void InitializeDeviceResources(ResourceCache resCache)
        {
            Stopwatch stopwatch = new();

            foreach (var layer in Layers.Values)
            {
                layer.InitializeResources(resCache);
            }

            //foreach (var layer in Layers.Values)
            //{
            //    //stopwatch.Restart();

            //    layer.InitializeGeometries();

            //    //stopwatch.Stop();
            //    //Debug.WriteLine($"InitializeGeometries: {layer.Name} - {stopwatch.ElapsedMilliseconds} ms");
            //}

            //Parallel.ForEach(Layers.Values, layer =>
            //{
            //    //stopwatch.Restart();

            //    layer.InitializeGeometries();

            //    //stopwatch.Stop();
            //    //Debug.WriteLine($"InitializeGeometries: {layer.Name} - {stopwatch.ElapsedMilliseconds} ms");
            //});


            var tasks = Layers.Values.Select(layer => Task.Run(() => layer.InitializeGeometries())).ToArray();
            Task.WhenAll(tasks).Wait();


            stopwatch.Stop();
            Debug.WriteLine($"InitializeDeviceResources: {stopwatch.ElapsedMilliseconds} ms");
        }
        public void UpdateDeviceDependentResources(ResourceCache resCache)
        {
            foreach (var layer in Layers.Values)
            {
                layer?.UpdateDeviceDependentResources(resCache);
            }
        }
        public void UpdateDeviceIndependentResources(ResourceCache resCache)
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
