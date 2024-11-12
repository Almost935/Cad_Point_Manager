using Cad_Point_Manager.Models.DrawingObjects;
using Cad_Point_Manager.Models.SerializableObjects;
using netDxf.Entities;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Ellipse = SharpDX.Direct2D1.Ellipse;

namespace Cad_Point_Manager.DrawingObjects
{
    public class DrawingCircle : DrawingSegment
    {
        #region Fields
        private Circle _dxfCircle;
        #endregion

        #region Properties
        public Circle DxfCircle
        {
            get { return _dxfCircle; }
            set
            {
                _dxfCircle = value;
                OnPropertyChanged(nameof(DxfCircle));
            }
        }

        public float Radius { get; set; }
        public RawVector2 Center { get; set; }
        #endregion

        #region Constructor
        public DrawingCircle(Circle dxfCircle, ObjectLayer layer, DrawingBlock drawingBlock = null, DrawingPolyline pline = null)
        {
            DxfCircle = dxfCircle;
            Entity = dxfCircle;
            Layer = layer;
            Block = drawingBlock;
            DrawingPolyline = pline;
            EntityCount = 1;
            LoadFromDxfEntity(dxfCircle);
        }

        public DrawingCircle(DrawingCircleData circleData, ObjectLayer layer, DrawingBlock drawingBlock = null, DrawingPolyline pline = null)
        {
            Layer = layer;
            Block = drawingBlock;
            DrawingPolyline = pline;
            EntityCount = 1;
            LoadFromData(circleData);
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
            if (e is Circle circle)
            {
                Radius = (float)circle.Radius;
                Center = new RawVector2((float)DxfCircle.Center.X, (float)DxfCircle.Center.Y);
                var verteces = circle.ToPolyline2D(2).Vertexes;
                StartPoint = new(
                    (float)verteces.First().Position.X,
                    (float)verteces.First().Position.Y);
                EndPoint = new(
                    (float)verteces.Last().Position.X,
                    (float)verteces.Last().Position.Y);
            }
            else
            {
                throw new ArgumentException("EntityObject must be of type Circle");
            }
        }
        public override void LoadFromData(DrawingObjectData drawingObjectData)
        {
            if (drawingObjectData is DrawingCircleData data)
            {
                StartPoint = new((float)data.StartPoint.X, (float)data.EndPoint.Y);
                EndPoint = new((float)data.EndPoint.X, (float)data.EndPoint.Y);
                Center = new((float)data.Center.X, (float)data.Center.Y);
                Radius = data.Radius;
                IsPartOfBlock = data.IsPartOfBlock;
                IsPartOfPolyline = data.IsPartOfPolyline;
                Bounds = data.Bounds;
            }
            else
            {
                throw new ArgumentException("DrawingObjectData must be of type DrawingCircleData");
            }
        }
        public override DrawingObjectData GetData()
        {
            return new DrawingCircleData(this);
        }

        public override void UpdateGeometry()
        {
            Ellipse ellipse = new(new RawVector2((float)Center.X, (float)Center.Y), (float)Radius, (float)Radius);
            EllipseGeometry ellipseGeometry = new(Factory, ellipse);

            Geometry = ellipseGeometry;

            var bounds = Geometry.GetWidenedBounds(_hitTestStrokeThickness);
            Bounds = new(bounds.Left, bounds.Top, Math.Abs(bounds.Right - bounds.Left), Math.Abs(bounds.Bottom - bounds.Top));
        }
    
        public override bool Hittest(RawVector2 p, float thickness)
        {
            return Geometry.StrokeContainsPoint(p, thickness);
        }
        #endregion
    }
    public class DrawingCircleData : DrawingSegmentData
    {
        public float Radius { get; set; }
        public SerializablePoint Center { get; set; }

        public DrawingCircleData(DrawingCircle drawingCircle, DrawingBlockData drawingBlockData = null, DrawingPolylineData drawingPolylineData = null)
        {
            Radius = drawingCircle.Radius;
            Center = new(drawingCircle.Center.X, drawingCircle.Center.Y);
            StartPoint = new(drawingCircle.StartPoint.X, drawingCircle.StartPoint.Y);
            EndPoint = new(drawingCircle.EndPoint.X, drawingCircle.EndPoint.Y);
            Bounds = drawingCircle.Bounds;
            LayerName = drawingCircle.Layer.Name;
            IsPartOfBlock = drawingCircle.IsPartOfBlock;
            DrawingBlockData = drawingBlockData;
            IsPartOfPolyline = drawingCircle.IsPartOfPolyline;
            DrawingPolylineData = drawingPolylineData;
        }

        public override DrawingObject CreateDrawingObject(ObjectLayer layer, DrawingBlock block = null)
        {
            ArgumentNullException.ThrowIfNull(layer);
            DrawingCircle drawingCircle = new(this, layer, block);

            return drawingCircle;
        }

        public override DrawingSegment CreateDrawingSegment(ObjectLayer layer, DrawingBlock block = null, DrawingPolyline pline = null)
        {
            ArgumentNullException.ThrowIfNull(layer);
            DrawingCircle drawingCircle = new(this, layer, block, pline);

            return drawingCircle;
        }
    }
}
