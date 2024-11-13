using Cad_Point_Manager.Controls.D2DControl;
using Cad_Point_Manager.Models.SerializableObjects;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System.Collections.ObjectModel;
using System.Windows;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public abstract class DrawingPolyline : DrawingObject
    {
        #region Fields
        private RawVector2 _startPoint;
        private RawVector2 _endPoint;
        #endregion

        #region Properties
        public RawVector2 StartPoint
        {
            get { return _startPoint; }
            set
            {
                _startPoint = value;
                OnPropertyChanged(nameof(StartPoint));
            }
        }
        public RawVector2 EndPoint
        {
            get { return _endPoint; }
            set
            {
                _endPoint = value;
                OnPropertyChanged(nameof(EndPoint));
            }
        }

        public ObservableCollection<DrawingSegment> DrawingSegments { get; set; } = new();
        #endregion

        #region Methods
        public override void DrawToDeviceContext(float thickness, Brush brush)
        {
            if (DeviceContext is not null)
            {
                foreach (var segment in DrawingSegments)
                {
                    segment.DrawToDeviceContext(thickness, brush);
                }
            }
        }
        public override void DrawToDeviceContext(float thickness, Brush brush, StrokeStyle1 strokeStyle)
        {
            if (DeviceContext is not null)
            {
                foreach (var segment in DrawingSegments)
                {
                    segment.DrawToDeviceContext(thickness, brush, strokeStyle);
                }
            }
        }

        public override void InitializeResources(ResourceCache resCache)
        {
            ResCache = resCache;
            DeviceContext = resCache.DeviceContext;
            Factory = resCache.Factory;

            foreach (var obj in DrawingSegments)
            {
                obj.InitializeResources(resCache);
            }
        }
        public override void UpdateDeviceDependentResources(ResourceCache resCache)
        {
            ResCache = resCache;
            DeviceContext = resCache.DeviceContext;

            foreach (var obj in DrawingSegments)
            {
                obj.UpdateDeviceDependentResources(resCache);
            }
        }
        public override void UpdateDeviceIndependentResources(ResourceCache resCache)
        {
            ResCache = resCache;
            Factory = resCache.Factory;

            foreach (var obj in DrawingSegments)
            {
                obj.UpdateDeviceIndependentResources(resCache);
            }
        }

        public override bool DrawingObjectIsInRect(Rect rect)
        {
            foreach (var segment in DrawingSegments)
            {
                if (segment.DrawingObjectIsInRect(rect))
                {
                    return true;
                }
            }
            return false;
        }
        public override bool Hittest(RawVector2 p, float thickness)
        {
            foreach (var segment in DrawingSegments)
            {
                if (segment.Bounds.Contains(p.X, p.Y))
                {
                    if (segment.Hittest(p, thickness))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        #endregion
    }

    public abstract class DrawingPolylineData : DrawingObjectData
    {
        public DrawingPolylineData() { }

        public SerializablePoint StartPoint { get; set; }
        public SerializablePoint EndPoint { get; set; }
        public List<DrawingSegmentData> DrawingSegmentDatas { get; set; } = [];
    }
}
