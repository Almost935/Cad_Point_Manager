using Cad_Point_Manager.Helpers;
using netDxf.Entities;
using System.Collections.ObjectModel;
using System.Windows;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingPolyline3D : DrawingPolyline
    {
        #region Fields
        private Polyline3D _dxfPolyline3D;
        #endregion

        #region Properties
        public Polyline3D DxfPolyline3D
        {
            get { return _dxfPolyline3D; }
            set
            {
                _dxfPolyline3D = value;
                OnPropertyChanged(nameof(DxfPolyline3D));
            }
        }
        #endregion

        #region Constructor
        public DrawingPolyline3D(Polyline3D dxfPolyline3D, ObjectLayer layer, DrawingBlock drawingBlock = null)
        {
            DxfPolyline3D = dxfPolyline3D;
            Entity = dxfPolyline3D;
            Layer = layer;
            Block = drawingBlock;
            LoadFromDxfEntity(dxfPolyline3D);
        }

        public DrawingPolyline3D(DrawingPolyline3DData polyline3DData, ObjectLayer layer, DrawingBlock drawingBlock = null)
        {
            Layer = layer;
            Block = drawingBlock;
            LoadFromData(polyline3DData);
        }
        #endregion

        #region Methods
        public ObservableCollection<DrawingSegment> GetDrawingSegments(Polyline3D pline)
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
            if (e is Polyline3D pline)
            {
                var verteces = pline.Vertexes;

                StartPoint = new(
                    (float)verteces.First().X,
                    (float)verteces.First().Y);
                EndPoint = new(
                    (float)verteces.Last().X,
                    (float)verteces.Last().Y);
                DrawingSegments = GetDrawingSegments(pline);
                EntityCount = DrawingSegments.Count;
            }
            else
            {
                throw new ArgumentException("EntityObject must be of type DrawingPolyline3D");
            }
        }

        public override void LoadFromData(DrawingObjectData drawingObjectData)
        {
            if (drawingObjectData is DrawingPolyline3DData data)
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
                throw new ArgumentException("DrawingObjectData must be of type DrawingPolyline3DData");
            }
        }
        public override DrawingObjectData GetData()
        {
            return new DrawingPolyline3DData(this);
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

    public class DrawingPolyline3DData : DrawingPolylineData
    {
        public DrawingPolyline3DData(DrawingPolyline3D drawingPolyline, DrawingBlockData drawingBlockData = null)
        {
            StartPoint = new(drawingPolyline.StartPoint.X, drawingPolyline.StartPoint.Y);
            EndPoint = new(drawingPolyline.EndPoint.X, drawingPolyline.EndPoint.Y);
            Bounds = drawingPolyline.Bounds;
            LayerName = drawingPolyline.Layer.Name;
            IsPartOfBlock = drawingPolyline.IsPartOfBlock;
            DrawingBlockData = drawingBlockData;

            foreach (var seg in drawingPolyline.DrawingSegments)
            {
                DrawingSegmentDatas.Add(seg.GetDrawingSegmentData());
            }
        }

        public override DrawingObject CreateDrawingObject(ObjectLayer layer, DrawingBlock block = null)
        {
            ArgumentNullException.ThrowIfNull(layer);
            DrawingPolyline3D drawingpline = new(this, layer, block);

            return drawingpline;
        }
    }
}
