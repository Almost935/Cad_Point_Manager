using netDxf.Entities;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Cad_Point_Manager.Controls.D2DControl;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.DrawingObjectsData;

namespace Cad_Point_Manager.DrawingObjects
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
        public ObservableCollection<DrawingObject> DrawingObjects { get; set; } = new();


        public float CurrentScale { get; set; } = 1;
        #endregion

        #region Constructor
        public DrawingBlock(Insert dxfBlock, ObjectLayer layer)
        {
            DxfBlock = dxfBlock;
            Entity = dxfBlock;
            Layer = layer;

            LoadFromDxfEntity();
        }
        #endregion

        #region Methods
        public void GetDrawingObjects()
        {
            foreach (var e in DxfBlock.Explode())
            {
                var obj = DxfHelpers.GetDrawingObject(e, Layer);
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
                
            }
            else
            {
                throw new ArgumentException("DrawingObjectData must be of type DrawingArcData");
            }
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
                if (obj.Bounds.Contains((double)p.X, (double)p.Y))
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
        List<DrawingObjectData> drawingObjectDatas { get; set; }
    }
}
