using Direct2DDxfViewer.Direct2DControl;
using netDxf.Entities;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Direct2DDXFViewer.DrawingObjects
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
        public abstract void GetDrawingSegments();

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

            UpdateBrush();
            GetStrokeStyle();
        }
        public override void UpdateDeviceDependentResources(ResourceCache resCache)
        {
            ResCache = resCache;
            DeviceContext = resCache.DeviceContext;

            foreach (var obj in DrawingSegments)
            {
                obj.UpdateDeviceDependentResources(resCache);
            }

            UpdateBrush();
        }
        public override void UpdateDeviceIndependentResources(ResourceCache resCache)
        {
            ResCache = resCache;
            Factory = resCache.Factory;

            foreach (var obj in DrawingSegments)
            {
                obj.UpdateDeviceIndependentResources(resCache);
            }

            GetStrokeStyle();
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
                if (segment.Bounds.Contains((double)p.X, (double)p.Y))
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
}
