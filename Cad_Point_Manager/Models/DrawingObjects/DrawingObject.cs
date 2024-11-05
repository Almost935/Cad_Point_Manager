using Cad_Point_Manager.Controls.D2DControl;
using Cad_Point_Manager.Helpers;
using netDxf.Entities;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Cad_Point_Manager.DrawingObjects
{
    public abstract class DrawingObject : INotifyPropertyChanged, IDisposable
    {
        #region Fields
        private ObjectLayer _layer;
        private bool _isSnapped = false;
        private bool _isHighlighted = false;
        private float _outerEdgeOpacity = 0.25f;
        private bool _disposed = false;

        protected float _hitTestStrokeThickness = 10;
        #endregion

        #region Properties
        public bool IsSnapped
        {
            get { return _isSnapped; }
            set
            {
                _isSnapped = value;
                OnPropertyChanged(nameof(IsSnapped));
            }
        }
        public bool IsHighlighted
        {
            get { return _isHighlighted; }
            set
            {
                _isHighlighted = value;
                OnPropertyChanged(nameof(IsHighlighted));
            }
        }
        public ObjectLayer Layer
        {
            get { return _layer; }
            set
            {
                _layer = value;
                OnPropertyChanged(nameof(Layer));
            }
        }

        public EntityObject Entity { get; set; }
        public Geometry Geometry { get; set; }
        public Rect Bounds { get; set; } = Rect.Empty;
        public DeviceContext1 DeviceContext { get; set; }
        public Factory1 Factory { get; set; }
        public Brush Brush { get; set; }
        public Brush OuterEdgeBrush { get; set; }
        public StrokeStyle1 HairlineStrokeStyle { get; set; }
        public StrokeStyle1 FixedStrokeStyle { get; set; }
        public float Thickness { get; set; } = 0.25f;

        public ResourceCache ResCache { get; set; }
        public bool IsInView { get; set; } = true;
        public bool IsPartOfBlock { get; set; } = false;
        public DrawingBlock Block { get; set; }
        public int EntityCount { get; set; }
        #endregion

        #region Constructor
        public DrawingObject() { }
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Methods
        public abstract void UpdateDxfProperties();
        public abstract void UpdateGeometry();
        public abstract void DrawToDeviceContext(float thickness, Brush brush);
        public abstract void DrawToDeviceContext(float thickness, Brush brush, StrokeStyle1 strokeStyle);
        public abstract bool DrawingObjectIsInRect(Rect rect);
        public abstract bool Hittest(RawVector2 p, float thickness);


        public virtual void InitializeResources(ResourceCache resCache)
        {
            ResCache = resCache;
            DeviceContext = resCache.DeviceContext;
            Factory = resCache.Factory;

            UpdateBrush();
            GetStrokeStyle();
        }
        public virtual void UpdateDeviceDependentResources(ResourceCache resCache)
        {
            ResCache = resCache;
            DeviceContext = resCache.DeviceContext;

            UpdateBrush();
        }

        public virtual void UpdateDeviceIndependentResources(ResourceCache resCache)
        {
            ResCache = resCache;
            Factory = resCache.Factory;

            GetStrokeStyle();
        }
        public void UpdateBrush()
        {
            if (Entity is null || DeviceContext is null)
            {
                return;
            }

            Brush?.Dispose();
            Brush = null;

            OuterEdgeBrush?.Dispose();
            OuterEdgeBrush = null;

            (byte r, byte g, byte b, byte a) = DxfHelpers.GetRGBAColor(Entity);
            (byte r2, byte g2, byte b2, byte a2) = (r, g, b, (byte)(0.4 * 255));

            Brush = ResCache.GetBrush(r, g, b, a);
            OuterEdgeBrush = ResCache.GetBrush(r2, g2, b2, a2);
        }

        public void GetStrokeStyle()
        {
            HairlineStrokeStyle = ResCache.GetStrokeStyle(ResourceCache.LineType.Solid, StrokeTransformType.Hairline);
            FixedStrokeStyle = ResCache.GetStrokeStyle(ResourceCache.LineType.Solid, StrokeTransformType.Fixed);
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
                Brush?.Dispose();
                OuterEdgeBrush?.Dispose();
                HairlineStrokeStyle?.Dispose();
                Geometry?.Dispose();
            }

            // Free unmanaged resources if any

            _disposed = true;
        }

        ~DrawingObject()
        {
            Dispose(false);
        }
        #endregion
    }
}
