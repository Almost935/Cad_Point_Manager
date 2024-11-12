using netDxf.Entities;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System.Collections.ObjectModel;
using System.Windows;
using Cad_Point_Manager.Controls.D2DControl;
using Cad_Point_Manager.Helpers;
using System.Net;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingBlock : DrawingObject
    {
        #region Fields
        private Insert _dxfBlock;
        #endregion

        #region Properties
        public Insert DxfBlock
        {
            get { return _dxfBlock; }
            set
            {
                _dxfBlock = value;
                OnPropertyChanged(nameof(DxfBlock));
            }
        }
        public ObservableCollection<DrawingObject> DrawingObjects { get; set; } = [];


        public float CurrentScale { get; set; } = 1;
        #endregion

        #region Constructor
        public DrawingBlock(Insert dxfBlock, ObjectLayer layer, DrawingBlock drawingBlock = null)
        {
            DxfBlock = dxfBlock;
            Entity = dxfBlock;
            Layer = layer;
            Block = drawingBlock;
            LoadFromDxfEntity(dxfBlock);
        }

        public DrawingBlock(DrawingBlockData blockData, ObjectLayer layer, DrawingBlock drawingBlock = null)
        {
            Layer = layer;
            Block = drawingBlock;
            LoadFromData(blockData);
        }
        #endregion

        #region Methods
        public void GetDrawingObjects()
        {
            foreach (var e in DxfBlock.Explode())
            {
                var obj = DxfHelpers.GetDrawingObject(e, Layer, this);
                obj.IsPartOfBlock = true;
                obj.Block = this;

                if (obj is not null)
                {
                    EntityCount += obj.EntityCount;
                    DrawingObjects.Add(obj);
                }
            }
        }

        public override void DrawToDeviceContext(float thickness, Brush brush)
        {
            if (DeviceContext is not null)
            {
                foreach (var obj in DrawingObjects)
                {
                    obj.DrawToDeviceContext(thickness, brush);
                }
            }
        }
        public override void DrawToDeviceContext(float thickness, Brush brush, StrokeStyle1 strokeStyle)
        {
            if (DeviceContext is not null)
            {
                foreach (var obj in DrawingObjects)
                {
                    obj.DrawToDeviceContext(thickness, brush, strokeStyle);
                }
            }
        }

        public override void InitializeResources(ResourceCache resCache)
        {
            ResCache = resCache;
            DeviceContext = resCache.DeviceContext;
            Factory = resCache.Factory;

            foreach (var obj in DrawingObjects)
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

            foreach (var obj in DrawingObjects)
            {
                obj.UpdateDeviceDependentResources(resCache);
            }
            UpdateBrush();
        }

        public override void UpdateDeviceIndependentResources(ResourceCache resCache)
        {
            ResCache = resCache;
            Factory = resCache.Factory;

            foreach (var obj in DrawingObjects)
            {
                obj.UpdateDeviceIndependentResources(resCache);
            }
            GetStrokeStyle();
        }

        public override bool DrawingObjectIsInRect(Rect rect)
        {
            foreach (var obj in DrawingObjects)
            {
                if (obj.DrawingObjectIsInRect(rect))
                {
                    return true;
                }
            }
            return false;
        }

        public override void LoadFromDxfEntity(EntityObject entity)
        {
            if (entity is Insert insert)
            {
                foreach (var e in insert.Explode())
                {
                    var obj = DxfHelpers.GetDrawingObject(e, Layer);

                    if (obj is not null)
                    {
                        EntityCount += obj.EntityCount;
                        DrawingObjects.Add(obj);
                    }
                }
            }
            else
            {
                throw new ArgumentException("EntityObject must be of type Insert");
            }
        }
        public override void LoadFromData(DrawingObjectData drawingObjectData)
        {
            if (drawingObjectData is DrawingBlockData data)
            {
                Bounds = data.Bounds;
                IsPartOfBlock = data.IsPartOfBlock;  
                DrawingObjects = [];

                foreach (var objDatas in data.DrawingObjectDatas)
                {
                    DrawingObjects.Add(objDatas.CreateDrawingObject(Layer));
                }
            }
            else
            {
                throw new ArgumentException("DrawingObjectData must be of type DrawingArcData");
            }
        }
        public override DrawingObjectData GetData()
        {
            return new DrawingBlockData(this);
        }

        public override void UpdateGeometry()
        {
            foreach (var obj in DrawingObjects)
            {
                obj.UpdateGeometry();

                if (Bounds.IsEmpty)
                {
                    Bounds = obj.Bounds;
                }
                else
                {
                    Bounds.Union(obj.Bounds);
                }
            }
        }

        public override bool Hittest(RawVector2 p, float thickness)
        {
            foreach (var obj in DrawingObjects)
            {
                if (obj.Bounds.Contains(p.X, p.Y))
                {
                    if (obj.Hittest(p, thickness))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        #endregion
    }
    public class DrawingBlockData : DrawingObjectData
    {
        #region Properties
        public List<DrawingObjectData> DrawingObjectDatas { get; set; } = [];
        #endregion

        #region Constructor
        public DrawingBlockData(DrawingBlock drawingBlock, DrawingBlockData drawingBlockData = null, DrawingPolylineData drawingPolylineData = null)
        {
            Bounds = drawingBlock.Bounds;
            LayerName = drawingBlock.Layer.Name;
            IsPartOfBlock = drawingBlock.IsPartOfBlock;
            DrawingBlockData = drawingBlockData;

            foreach (var obj in drawingBlock.DrawingObjects)
            {
                DrawingObjectDatas.Add(obj.GetData());
            }
        }
        #endregion

        #region Methods
        public override DrawingObject CreateDrawingObject(ObjectLayer layer, DrawingBlock block = null)
        {
            ArgumentNullException.ThrowIfNull(layer);

            DrawingBlock drawingBlock = new(this, layer);

            foreach (var data in DrawingObjectDatas)
            {
                drawingBlock.DrawingObjects.Add(data.CreateDrawingObject(layer, block));
            }

            return drawingBlock;
        }
        #endregion
    }
}
