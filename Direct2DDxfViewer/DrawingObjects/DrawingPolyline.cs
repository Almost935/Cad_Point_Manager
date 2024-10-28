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
        #endregion

        #region Properties
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
                    segment.DrawToRenderTarget(DeviceContext, thickness, brush);
                }
            }
        }
        public override void DrawToDeviceContext(float thickness, Brush brush, StrokeStyle1 strokeStyle)
        {
            if (DeviceContext is not null)
            {
                foreach (var segment in DrawingSegments)
                {
                    segment.DrawToRenderTarget(DeviceContext, thickness, brush, strokeStyle);
                }
            }
        }
        public override void DrawToRenderTarget(float thickness, Brush brush)
        {
            foreach (var segment in DrawingSegments)
            {
                segment.DrawToRenderTarget(target, thickness, brush);
            }
        }
        public override void DrawToRenderTarget(float thickness, Brush brush, StrokeStyle1 strokeStyle)
        {
            foreach (var segment in DrawingSegments)
            {
                segment.DrawToRenderTarget(target, thickness, brush, strokeStyle);
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
