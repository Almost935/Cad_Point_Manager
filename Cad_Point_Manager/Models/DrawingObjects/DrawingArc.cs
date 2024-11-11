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
        public double Radius { get; set; }
        #endregion

        #region Constructor
        public DrawingArc(Arc dxfArc, ObjectLayer layer)
        {
            DxfArc = dxfArc;
            Entity = dxfArc;
            Layer = layer;
            EntityCount = 1;

            LoadFromDxfEntity(DxfArc);
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
        public override void LoadFromDxfEntity(EntityObject e)
        {
            if (e is Arc arc)
            {
                var verteces = arc.ToPolyline2D(2).Vertexes;
                StartPoint = new(
                    (float)verteces.First().Position.X,
                    (float)verteces.First().Position.Y);
                EndPoint = new(
                    (float)verteces.Last().Position.X,
                    (float)verteces.Last().Position.Y);

                // Get sweep and find out if large arc 
                if (arc.EndAngle < arc.StartAngle)
                {
                    Sweep = (360 + arc.EndAngle) - arc.StartAngle;
                }
                else
                {
                    Sweep = Math.Abs(arc.EndAngle - arc.StartAngle);
                }
                IsLargeArc = Sweep >= 180;
            }
            else
            {
                throw new ArgumentException("EntityObject must be of type Arc");
            }
        }
        public override void LoadFromData(DrawingObjectData drawingObjectData)
        {
            if (drawingObjectData is DrawingArcData data)
            {
                StartPoint = new((float)data.StartPoint.X, (float)data.EndPoint.Y);
                EndPoint = new((float)data.EndPoint.X, (float)data.EndPoint.Y);
                Sweep = data.Sweep;
                IsLargeArc = data.IsLargeArc;
                Radius = data.Radius;
                IsPartOfBlock = data.IsPartOfBlock;
                Bounds = data.Bounds;
            }
            else
            {
                throw new ArgumentException("DrawingObjectData must be of type DrawingArcData");
            }
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
                    Size = new((float)Radius, (float)Radius),
                    SweepDirection = SweepDirection.Clockwise,
                    RotationAngle = (float)Sweep,
                    ArcSize = IsLargeArc ? ArcSize.Large : ArcSize.Small
                };

                sink.AddArc(arcSegment);
                sink.EndFigure(FigureEnd.Open);
                sink.Close();

                //var simplifiedGeometry = new PathGeometry(Factory);

                //// Open a GeometrySink to store the simplified version of the original geometry
                //using (var geometrySink = simplifiedGeometry.Open())
                //{
                //    // Simplify the geometry, reducing it to line segments
                //    pathGeometry.Simplify(GeometrySimplificationOption.CubicsAndLines, 0.25f, geometrySink);
                //    geometrySink.Close();
                //}
                //Geometry = simplifiedGeometry;

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

    public class DrawingArcData : DrawingSegmentData
    {
        public double Sweep { get; set; }
        public bool IsLargeArc { get; set; }
        public double Radius { get; set; }
    }
}
