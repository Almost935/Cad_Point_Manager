using netDxf.Entities;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cad_Point_Manager.DrawingObjects
{
    public class DrawingArc : DrawingSegment
    {
        #region Fields
        private Arc _dxfArc;
        #endregion

        #region Properties
        public Arc DxfArc
        {
            get { return _dxfArc; }
            set
            {
                _dxfArc = value;
                OnPropertyChanged(nameof(DxfArc));
            }
        }

        public double Sweep { get; set; }
        public bool IsLargeArc { get; set; }
        public bool Radius { get; }
        #endregion

        #region Constructor
        public DrawingArc(Arc dxfArc, ObjectLayer layer)
        {
            DxfArc = dxfArc;
            Entity = dxfArc;
            Layer = layer;
            EntityCount = 1;

            UpdateDxfProperties();         
        }
        #endregion

        #region Methods
       

        public override void DrawToDeviceContext(float thickness, Brush brush)
        {
            if (DeviceContext is not null)
            {
                DeviceContext.DrawGeometry(Geometry, brush, thickness);
            }
        }
        public override void DrawToDeviceContext(float thickness, Brush brush, StrokeStyle1 strokeStyle)
        {
            if (DeviceContext is not null)
            {
                DeviceContext.DrawGeometry(Geometry, brush, thickness, strokeStyle);
            }
        }

        public override bool DrawingObjectIsInRect(Rect rect)
        {
            return Bounds.IntersectsWith(rect) || Bounds.Contains(rect);
        }
        public override void UpdateDxfProperties()
        {
            // Start by getting start and end points using NetDxf ToPolyline2D method
            StartPoint = new(
                (float)DxfArc.ToPolyline2D(2).Vertexes.First().Position.X,
                (float)DxfArc.ToPolyline2D(2).Vertexes.First().Position.Y);
            EndPoint = new(
                (float)DxfArc.ToPolyline2D(2).Vertexes.Last().Position.X,
                (float)DxfArc.ToPolyline2D(2).Vertexes.Last().Position.Y);

            // Get sweep and find out if large arc 
            if (DxfArc.EndAngle < DxfArc.StartAngle)
            {
                Sweep = (360 + DxfArc.EndAngle) - DxfArc.StartAngle;
            }
            else
            {
                Sweep = Math.Abs(DxfArc.EndAngle - DxfArc.StartAngle);
            }
            IsLargeArc = Sweep >= 180;
        }
        public override void UpdateGeometry()
        {
            PathGeometry pathGeometry = new(Factory);
            using (var sink = pathGeometry.Open())
            {
                sink.BeginFigure(StartPoint, FigureBegin.Filled);

                ArcSegment arcSegment = new()
                {
                    Point = EndPoint,
                    Size = new((float)DxfArc.Radius, (float)DxfArc.Radius),
                    SweepDirection = SweepDirection.Clockwise,
                    RotationAngle = (float)Sweep,
                    ArcSize = IsLargeArc ? ArcSize.Large : ArcSize.Small
                };

                sink.AddArc(arcSegment);
                sink.EndFigure(FigureEnd.Open);
                sink.Close();

                Geometry = pathGeometry;

                var bounds = Geometry.GetWidenedBounds(_hitTestStrokeThickness);
                Bounds = new(bounds.Left, bounds.Top, Math.Abs(bounds.Right - bounds.Left), Math.Abs(bounds.Bottom - bounds.Top));
            }
        }
        public override bool Hittest(RawVector2 p, float thickness)
        {
            return Geometry.StrokeContainsPoint(p, thickness);
        }
        #endregion
    }
}
