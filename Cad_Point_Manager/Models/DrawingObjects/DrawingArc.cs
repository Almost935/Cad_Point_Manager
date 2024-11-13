using Cad_Point_Manager.Models.SerializableObjects;
using netDxf.Entities;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System.Windows;

namespace Cad_Point_Manager.Models.DrawingObjects
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

        public float Sweep { get; set; }
        public bool IsLargeArc { get; set; }
        public float Radius { get; set; }
        public SerializablePoint Center { get; set; }
        public float StartAngle { get; set; }
        public float EndAngle { get; set; }
        #endregion

        #region Constructor
        public DrawingArc(Arc dxfArc, ObjectLayer layer, DrawingBlock drawingBlock = null, DrawingPolyline drawingPline = null)
        {
            DxfArc = dxfArc;
            Entity = dxfArc;
            Layer = layer;
            Block = drawingBlock;
            DrawingPolyline = drawingPline;
            EntityCount = 1;
            LoadFromDxfEntity(DxfArc);
        }

        public DrawingArc(DrawingArcData arcData, ObjectLayer layer, DrawingBlock drawingBlock = null, DrawingPolyline drawingPline = null)
        {
            Layer = layer;
            Block = drawingBlock;
            DrawingPolyline = drawingPline;
            EntityCount = 1;
            LoadFromData(arcData);
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
                Radius = (float)arc.Radius;
                Center = new((float)arc.Center.X, (float)arc.Center.Y);
                
                StartAngle = (float)arc.StartAngle;
                EndAngle = (float)arc.EndAngle;
                Sweep = EndAngle - StartAngle;
                if (Sweep < 0) { Sweep += 360; }
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
                StartAngle = data.StartAngle;
                EndAngle = data.EndAngle;
                Center = new((float)data.Center.X, (float)data.Center.Y);
                Sweep = data.Sweep;
                IsLargeArc = data.IsLargeArc;
                Radius = data.Radius;
                IsPartOfBlock = data.IsPartOfBlock;
                IsPartOfPolyline = data.IsPartOfPolyline;
                Bounds = data.Bounds;
            }
            else
            {
                throw new ArgumentException("DrawingObjectData must be of type DrawingArcData");
            }
        }
        public override DrawingObjectData GetData()
        {
            return new DrawingArcData(this);
        }
        public override DrawingSegmentData GetDrawingSegmentData()
        {
            return new DrawingArcData(this);
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
            if (Geometry is null || Geometry.IsDisposed) { return false; }

            return Geometry.StrokeContainsPoint(p, thickness);
        }
        #endregion
    }

    public class DrawingArcData : DrawingSegmentData
    {
        #region Properties
        public float Sweep { get; set; }
        public bool IsLargeArc { get; set; }
        public float Radius { get; set; }
        public float StartAngle { get; set; }
        public float EndAngle { get; set; }
        public SerializablePoint Center { get; set; }
        #endregion

        #region Constructor
        public DrawingArcData(DrawingArc drawingArc, DrawingBlockData drawingBlockData = null, DrawingPolylineData drawingPolylineData = null)
        {
            Sweep = drawingArc.Sweep;
            IsLargeArc = drawingArc.IsLargeArc;
            Radius = drawingArc.Radius;
            StartAngle = drawingArc.StartAngle;
            EndAngle = drawingArc.EndAngle;
            Center = drawingArc.Center;
            StartPoint = new(drawingArc.StartPoint.X, drawingArc.StartPoint.Y);
            EndPoint = new(drawingArc.EndPoint.X, drawingArc.EndPoint.Y);
            Bounds = drawingArc.Bounds;
            LayerName = drawingArc.Layer.Name;
            IsPartOfBlock = drawingArc.IsPartOfBlock;
            DrawingBlockData = drawingBlockData;
            IsPartOfPolyline = drawingArc.IsPartOfPolyline;
            DrawingPolylineData = drawingPolylineData;
        }
        #endregion

        #region Methods
        public override DrawingObject CreateDrawingObject(ObjectLayer layer, DrawingBlock block = null)
        {
            ArgumentNullException.ThrowIfNull(layer);
            DrawingArc drawingArc = new(this, layer, block);

            return drawingArc;
        }

        public override DrawingSegment CreateDrawingSegment(ObjectLayer layer, DrawingBlock block = null, DrawingPolyline pline = null)
        {
            ArgumentNullException.ThrowIfNull(layer);
            DrawingArc drawingArc = new(this, layer, block, pline);

            return drawingArc;
        }
        #endregion
    }
}
