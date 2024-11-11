using Cad_Point_Manager.Helpers;
using netDxf.Entities;
using SharpDX.Direct2D1;
using System.Collections.ObjectModel;
using System.Windows;
using static netDxf.Entities.HatchBoundaryPath;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingPolyline2D : DrawingPolyline
    {
        #region Fields
        private Polyline2D _dxfPolyline2D;
        #endregion

        #region Properties
        public Polyline2D DxfPolyline2D
        {
            get { return _dxfPolyline2D; }
            set
            {
                _dxfPolyline2D = value;
                OnPropertyChanged(nameof(DxfPolyline2D));
            }
        }
        #endregion

        #region Constructor
        public DrawingPolyline2D(Polyline2D dxfPolyline2D, ObjectLayer layer, DrawingBlock drawingBlock = null)
        {
            DxfPolyline2D = dxfPolyline2D;
            Entity = dxfPolyline2D;
            Layer = layer;
            Block = drawingBlock;
            LoadFromDxfEntity(DxfPolyline2D);
        }

        public DrawingPolyline2D(DrawingPolyline2DData polylineData, ObjectLayer layer, DrawingBlock drawingBlock = null)
        {
            Layer = layer;
            Block = drawingBlock;
            LoadFromData(polylineData);
        }
        #endregion

        #region Methods

        public ObservableCollection<DrawingSegment> GetDrawingSegments(Polyline2D pline)
        {
            ObservableCollection<DrawingSegment> drawingObjects = [];

            foreach (var e in pline.Explode())
            {
                var obj = DxfHelpers.GetDrawingSegment(e, Layer);
                obj.IsPartOfPolyline = true;
                obj.DrawingPolyline = this;
                obj.LoadFromDxfEntity(e);

                if (obj is not null)
                {
                    drawingObjects.Add(obj);
                }
            }
            return drawingObjects;
        }

        public override void LoadFromDxfEntity(EntityObject e)
        {
            if (e is Polyline2D pline)
            {
                var verteces = pline.Vertexes;
                StartPoint = new(
                    (float)verteces.First().Position.X,
                    (float)verteces.First().Position.Y);
                EndPoint = new(
                    (float)verteces.Last().Position.X,
                    (float)verteces.Last().Position.Y);
                DrawingSegments = GetDrawingSegments(pline);
                EntityCount = DrawingSegments.Count;
            }
            else
            {
                throw new ArgumentException("EntityObject must be of type DrawingPolyline2D");
            }
        }
        public override void LoadFromData(DrawingObjectData drawingObjectData)
        {
            if (drawingObjectData is DrawingPolyline2DData data)
            {
                StartPoint = new((float)data.StartPoint.X, (float)data.EndPoint.Y);
                EndPoint = new((float)data.EndPoint.X, (float)data.EndPoint.Y);
                IsPartOfBlock = data.IsPartOfBlock;
                Bounds = data.Bounds;

                foreach (var drawingSegmentData in data.DrawingSegmentDatas)
                {
                    var drawingSegment = drawingSegmentData.CreateDrawingSegment(Layer, Block, this);
                    DrawingSegments.Add(drawingSegment);
                }
            }
            else
            {
                throw new ArgumentException("DrawingObjectData must be of type DrawingPolyline2DData");
            }
        }

        public override void UpdateGeometry()
        {
            Parallel.ForEach(DrawingSegments, segment =>
            {
                segment.UpdateGeometry();

                if (Bounds.IsEmpty)
                {
                    Bounds = segment.Bounds;
                }
                else
                {
                    Bounds = Rect.Union(Bounds, segment.Bounds);
                }
            });
        }
        #endregion
    }

    public class DrawingPolyline2DData : DrawingPolylineData
    {
        public override DrawingObject CreateDrawingObject(ObjectLayer layer, DrawingBlock block = null)
        {
            ArgumentNullException.ThrowIfNull(layer);
            DrawingPolyline2D drawingpline = new(this, layer, block);

            return drawingpline;
        }
    }
}
