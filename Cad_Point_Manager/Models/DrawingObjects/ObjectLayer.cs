using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D2DControl;
using Cad_Point_Manager.Models.SerializableObjects;
using netDxf.Tables;
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

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class ObjectLayer : INotifyPropertyChanged, IDisposable
    {
        #region Fields
        private CadManager _cadManager;
        private ResourceCache _resCache;
        private string _name;
        private List<DrawingObject> _drawingObjects = [];
        private bool isVisible = true;
        private bool _disposed = false;
        #endregion

        #region Properties
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
        public List<DrawingObject> DrawingObjects
        {
            get { return _drawingObjects; }
            set
            {
                _drawingObjects = value;
                OnPropertyChanged(nameof(DrawingObjects));
            }
        }
        public int DrawingObjectsCount
        {
            get { return DrawingObjects.Count; }
        }
        public bool IsVisible
        {
            get { return isVisible; }
            set
            {
                isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }

        public GeometryGroup GeometryGroup { get; set; }
        public Brush LayerBrush { get; set; }
        public StrokeStyle1 HairlineStrokeStyle { get; set; }
        public netDxf.Tables.Layer DxfLayer { get; set; }
        public SerializableColor Color { get; set; }
        #endregion

        #region Constructors
        public ObjectLayer(netDxf.Tables.Layer layer, CadManager cadManager)
        {
            DxfLayer = layer;
            _cadManager = cadManager;
            LoadFromDxfLayer(layer);
        }
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Methods
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public void InitializeResources(ResourceCache resCache)
        {
            _resCache = resCache;

            GetLayerBrush();
            GetLayerStrokeStyle();

            foreach (var obj in DrawingObjects)
            {
                obj?.InitializeResources(resCache);
            }
        }
        public void InitializeGeometries()
        {
            Parallel.ForEach(DrawingObjects, obj =>
            {
                Stopwatch stopwatch = new();
                stopwatch.Restart();

                obj?.UpdateGeometry();

                //stopwatch.Stop();
                //Debug.WriteLine($"InitializeGeometry of obj: {obj.GetType()}: {stopwatch.ElapsedMilliseconds} ms");
            });

            LoadGeometryGroup();
        }
        public void UpdateDeviceDependentResources(ResourceCache resCache)
        {
            _resCache = resCache;

            LayerBrush?.Dispose();
            LayerBrush = null;

            GetLayerBrush();

            foreach (var obj in DrawingObjects)
            {
                obj?.UpdateDeviceDependentResources(resCache);
            }
        }
        public void UpdateDeviceIndependentResources(ResourceCache resCache)
        {
            _resCache = resCache;

            GeometryGroup?.Dispose();
            GeometryGroup = null;
            HairlineStrokeStyle?.Dispose();
            HairlineStrokeStyle = null;

            GetLayerStrokeStyle();

            foreach (var obj in DrawingObjects)
            {
                obj?.UpdateDeviceIndependentResources(resCache);
            }
            LoadGeometryGroup();
        }
        private void LoadFromDxfLayer(netDxf.Tables.Layer layer)
        {
            Name = layer.Name;
            Color = new(layer.Color.R, layer.Color.G, layer.Color.B, 255);
        }
        private void LoadGeometryGroup()
        {
            List<Geometry> geometries = [];

            foreach (var obj in DrawingObjects)
            {
                if (obj is DrawingBlock block)
                {
                    foreach (var blockObj in block.DrawingObjects)
                    {
                        if (obj is DrawingPolyline polyline)
                        {
                            foreach (var segment in polyline.DrawingSegments)
                            {
                                if (segment.Geometry is not null)
                                {
                                    geometries.Add(segment.Geometry);
                                }
                            }
                        }
                        if (blockObj.Geometry is not null)
                        {
                            geometries.Add(blockObj.Geometry);
                        }
                    }
                }
                else if (obj is DrawingPolyline polyline)
                {
                    foreach (var segment in polyline.DrawingSegments)
                    {
                        if (segment.Geometry is not null)
                        {
                            geometries.Add(segment.Geometry);
                        }
                    }
                }
                else if (obj.Geometry is not null)
                {
                    geometries.Add(obj.Geometry);
                }
            }
            if (geometries.Count > 0)
            {
                var geometryArr = geometries.ToArray();
                GeometryGroup = new(_resCache.Factory, FillMode.Alternate, geometryArr);
            }
        }
        public void GetLayerStrokeStyle()
        {
            HairlineStrokeStyle = _cadManager.GetStrokeStyle(Enums.LineType.Solid, StrokeTransformType.Hairline);
        }
        public void GetLayerBrush()
        {
            LayerBrush?.Dispose();
            LayerBrush = _cadManager.GetBrush(Color.R, Color.G, Color.B, Color.A);
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
                if (_drawingObjects != null)
                {
                    foreach (var drawingObject in _drawingObjects)
                    {
                        if (drawingObject is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    }
                    _drawingObjects.Clear();
                }
            }

            // Free unmanaged resources if any

            _disposed = true;
        }

        ~ObjectLayer()
        {
            Dispose(false);
        }
        #endregion
    }

    public class ObjectLayerData
    {
        public string Name { get; set; }
        public SerializableColor Color { get; set; }
        public List<DrawingObjectData> DrawingObjects { get; set; } = [];
    }
}
