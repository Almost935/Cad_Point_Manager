using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System.Windows;

using Ellipse = SharpDX.Direct2D1.Ellipse;
using EllipseGeometry = SharpDX.Direct2D1.EllipseGeometry;
using Geometry = SharpDX.Direct2D1.Geometry;
using Brush = SharpDX.Direct2D1.Brush;
using PathGeometry = SharpDX.Direct2D1.PathGeometry;
using ArcSegment = SharpDX.Direct2D1.ArcSegment;
using SweepDirection = SharpDX.Direct2D1.SweepDirection;
using Matrix = System.Windows.Media.Matrix;
using Cad_Point_Manager.Models.SerializableObjects;
using netDxf.Entities;
using static netDxf.Entities.HatchBoundaryPath;
using Cad_Point_Manager.Common;

namespace Cad_Point_Manager.DrawingObjects
{
    public class DrawingEllipse : DrawingSegment
    {
        #region Fields
        private netDxf.Entities.Ellipse _dxfEllipse;
        #endregion

        #region Properties
        public netDxf.Entities.Ellipse DxfEllipse
        {
            get { return _dxfEllipse; }
            set
            {
                _dxfEllipse = value;
                OnPropertyChanged(nameof(_dxfEllipse));
            }
        }

        public Enums.EllipseType Type { get; set; }
        public double StartAngle { get; set; }
        public double EndAngle { get; set; }
        public double Sweep { get; set; }
        public double MajorAxis { get; set; }
        public double MinorAxis { get; set; }
        public double Rotation { get; set; }
        public bool IsLargeArc { get; set; }
        public SerializablePoint Center { get; set; }
        public double Radius { get; set; }
        #endregion

        #region Constructor
        public DrawingEllipse(netDxf.Entities.Ellipse dxfEllipse, ObjectLayer layer)
        {
            DxfEllipse = dxfEllipse;
            Entity = dxfEllipse;
            Layer = layer;
            EntityCount = 1;

            LoadFromDxfEntity(dxfEllipse);
        }
        #endregion

        #region Methods
        public override void DrawToDeviceContext(float thickness, Brush brush)
        {
            DeviceContext?.DrawGeometry(Geometry, brush, thickness);
        }
        public override void DrawToDeviceContext(float thickness, Brush brush, StrokeStyle1 strokeStyle)
        {
            DeviceContext?.DrawGeometry(Geometry, brush, thickness, strokeStyle);
        }
       
        public override bool DrawingObjectIsInRect(Rect rect)
        {
            return Bounds.IntersectsWith(rect) || Bounds.Contains(rect);
        }

        public override void LoadFromDxfEntity(EntityObject e)
        {
            if (e is netDxf.Entities.Ellipse ellipse)
            {
                var verteces = ellipse.ToPolyline2D(2).Vertexes;
                StartPoint = new(
                    (float)verteces.First().Position.X,
                    (float)verteces.First().Position.Y);
                EndPoint = new(
                    (float)verteces.Last().Position.X,
                    (float)verteces.Last().Position.Y);

                StartAngle = ellipse.StartAngle;
                EndAngle = ellipse.EndAngle;
                MajorAxis = ellipse.MajorAxis;
                MinorAxis = ellipse.MinorAxis;
                Rotation = ellipse.Rotation;
                Center = new(ellipse.Center.X, ellipse.Center.Y);

                if (EndAngle < StartAngle)
                {
                    Sweep = (360 + EndAngle) - StartAngle;
                }
                else
                {
                    Sweep = Math.Abs(EndAngle - StartAngle);
                }
                IsLargeArc = Sweep >= 180;

                if (DxfEllipse.IsFullEllipse)
                {
                    Type = Enums.EllipseType.FullEllipse;
                }
                else
                {
                    Type = Enums.EllipseType.Arc;
                }
            }
            else
            {
                throw new ArgumentException("EntityObject must be of type Ellipse");
            }
        }
        public override void LoadFromData(DrawingObjectData drawingObjectData)
        {
            if (drawingObjectData is DrawingEllipseData data)
            {
                StartPoint = new((float)data.StartPoint.X, (float)data.EndPoint.Y);
                EndPoint = new((float)data.EndPoint.X, (float)data.EndPoint.Y);
                StartAngle = data.StartAngle;
                EndAngle = data.EndAngle;
                MajorAxis = data.MajorAxis;
                MinorAxis = data.MinorAxis;
                Center = data.Center;
                Radius = data.Radius;
                IsPartOfBlock = data.IsPartOfBlock;
                Bounds = data.Bounds;
                Type = data.Type;
                Sweep = data.Sweep;
                IsLargeArc = data.IsLargeArc;
            }
            else
            {
                throw new ArgumentException("DrawingObjectData must be of type DrawingEllipseData");
            }
        }

        public override void UpdateGeometry()
        {
            if (Type is Enums.EllipseType.FullEllipse)
            {
                Geometry = GetEllipseGeometry();
            }
            else
            {
                Geometry = GetArcGeometry();
            }

            var bounds = Geometry.GetWidenedBounds(10);
            Bounds = new(bounds.Left, bounds.Top, Math.Abs(bounds.Right - bounds.Left), Math.Abs(bounds.Bottom - bounds.Top));
        }
        public Geometry GetArcGeometry()
        {            
            PathGeometry pathGeometry = new(Factory);
            using (var sink = pathGeometry.Open())
            {
                sink.BeginFigure(StartPoint, FigureBegin.Filled);

                ArcSegment arcSegment = new()
                {
                    Point = EndPoint,
                    Size = new((float)(MajorAxis / 2), (float)(MinorAxis / 2)),
                    SweepDirection = SweepDirection.Clockwise,
                    RotationAngle = (float)Rotation,
                    ArcSize = IsLargeArc ? ArcSize.Large : ArcSize.Small
                };

                sink.AddArc(arcSegment);
                sink.EndFigure(FigureEnd.Open);
                sink.Close();

                // Apply rotation if needed
                if (Rotation != 0)
                {
                    Matrix matrix = new();
                    //matrix.RotateAt((float)rotation, centerPoint.X, centerPoint.Y);

                    // Apply rotation transformation if required
                    RawMatrix3x2 transform = new((float)matrix.M11, (float)matrix.M12, (float)matrix.M21, (float)matrix.M22, (float)matrix.OffsetX, (float)matrix.OffsetY);

                    return new TransformedGeometry(Factory, pathGeometry, transform);
                }
                else
                {
                    return pathGeometry;
                }
            }
        }
        public Geometry GetEllipseGeometry()
        {
            // Convert center coordinates and axes to SharpDX format
            var centerPoint = new RawVector2((float)Center.X, (float)Center.Y);

            Matrix matrix = new();
            matrix.RotateAt((float)Rotation, centerPoint.X, centerPoint.Y);

            // Create the SharpDX Ellipse
            Ellipse ellipse = new(centerPoint, (float)(MajorAxis / 2), (float)(MinorAxis / 2));

            // Create EllipseGeometry
            var ellipseGeometry = new EllipseGeometry(Factory, ellipse);

            // Apply rotation if needed
            if (Rotation != 0)
            {
                // Apply rotation transformation if required
                RawMatrix3x2 transform = new((float)matrix.M11, (float)matrix.M12, (float)matrix.M21, (float)matrix.M22, (float)matrix.OffsetX, (float)matrix.OffsetY);
                return new TransformedGeometry(Factory, ellipseGeometry, transform);
            }
            else
            {
                return ellipseGeometry;
            }
        }
    
        public override bool Hittest(RawVector2 p, float thickness)
        {
            return Geometry.StrokeContainsPoint(p, thickness);
        }
        #endregion
    }
    public class DrawingEllipseData : DrawingSegmentData
    {
        public Enums.EllipseType Type { get; set; }
        public double StartAngle { get; set; }
        public double EndAngle { get; set; }
        public double Sweep { get; set; }
        public double MajorAxis { get; set; }
        public double MinorAxis { get; set; }
        public double Rotation { get; set; }
        public bool IsLargeArc { get; set; }
        public double Radius { get; set; }
        public SerializablePoint Center { get; set; }
    }
}
