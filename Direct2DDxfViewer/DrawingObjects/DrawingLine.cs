using Direct2DDxfViewer.Direct2DControl;
using Direct2DDXFViewer.Helpers;
using netDxf;
using netDxf.Entities;
using netDxf.Units;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Point = System.Windows.Point;

namespace Direct2DDXFViewer.DrawingObjects
{
    public class DrawingLine : DrawingSegment
    {
        #region Fields
        private Line _dxfLine;
        #endregion

        #region Properties
        public Line DxfLine
        {
            get { return _dxfLine; }
            set
            {
                _dxfLine = value;
                OnPropertyChanged(nameof(DxfLine));
            }
        }
        #endregion

        #region Constructor
        public DrawingLine(Line dxfLine, ObjectLayer layer)
        {
            DxfLine = dxfLine;
            Entity = dxfLine;
            Layer = layer;
            EntityCount = 1;
        }
        #endregion

        #region Methods
        public override void DrawToDeviceContext(float thickness, Brush brush)
        {
            DeviceContext?.DrawLine(StartPoint, EndPoint, brush, thickness);
        }
        public override void DrawToDeviceContext(float thickness, Brush brush, StrokeStyle1 strokeStyle)
        {
            DeviceContext?.DrawLine(StartPoint, EndPoint, brush, thickness, strokeStyle);
        }
      
        public override bool DrawingObjectIsInRect(Rect rect)
        {
            return Bounds.IntersectsWith(rect) || Bounds.Contains(rect);
        }

        public override void UpdateDxfProperties()
        {
            StartPoint = new((float)DxfLine.StartPoint.X, (float)DxfLine.StartPoint.Y);
            EndPoint = new((float)DxfLine.EndPoint.X, (float)DxfLine.EndPoint.Y);
        }
        public async override void UpdateGeometry()
        {
            PathGeometry pathGeometry = new(Factory);
            using (var sink = pathGeometry.Open())
            {
                sink.BeginFigure(StartPoint, FigureBegin.Filled);
                sink.AddLine(EndPoint);
                sink.EndFigure(FigureEnd.Open);
                sink.Close();

                Geometry = pathGeometry;

                var bounds = Geometry.GetWidenedBounds(10);
                Bounds = new(bounds.Left, bounds.Top, Math.Abs(bounds.Right - bounds.Left), Math.Abs(bounds.Bottom - bounds.Top));
            }
        }

        public override bool Hittest(RawVector2 p, float thickness)
        {
            return Geometry.StrokeContainsPoint(p, thickness); ;
        }
        #endregion
    }
}
