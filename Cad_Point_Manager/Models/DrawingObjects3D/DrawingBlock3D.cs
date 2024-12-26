using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.DrawingObjects;
using netDxf;
using netDxf.Entities;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Vector2 = SharpDX.Vector2;
using Vector3 = SharpDX.Vector3;
using Vector4 = SharpDX.Vector4;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class DrawingBlock3D : DrawingObject3D
    {
        #region Fields
        private List<DrawingObject3D> _drawingObjects = [];
        #endregion

        #region Properties
        public Insert Insert { get; set; }
        public List<DrawingObject3D> DrawingObjects
        {
            get => _drawingObjects;
            set
            {
                _drawingObjects = value;
                OnPropertyChanged(nameof(DrawingObjects));
            }
        }

        public Vector3 InsertionPoint { get; set; }
        public int NumberOfDrawingObjects => DrawingObjects.Count;
        #endregion

        #region Constructors
        private DrawingBlock3D() { Type = DrawingObject3dType.DrawingLine3D; }

        public DrawingBlock3D(Insert insert, ObjectLayer3D layer, bool isPartOfBlock = false, DrawingBlock3D block = null)
        {
            Type = DrawingObject3dType.DrawingBlock3D;
            Insert = insert;

            EntityObject = insert;
            Layer = layer;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock3D = block;
           
            UpdateColor();
            UpdateData(insert);
        }
        #endregion

        #region Methods
        public override void UpdateData(EntityObject entity)
        {
            if (entity is Insert insert)
            {
                InsertionPoint = new((float)insert.Position.X, (float)insert.Position.Y, (float)insert.Position.Z);

                UpdateDrawingObjects(insert);
                UpdateBounds();
            }
            else
            {
               throw new ArgumentException("entity must be of type Insert");
            }
        }

        public override void UpdateBounds()
        {
            Bounds = Rect.Empty;
            
            foreach (var drawingObj in DrawingObjects)
            {
                Bounds = Rect.Union(Bounds, drawingObj.Bounds);
            }
        }

        public override bool HitTest(System.Windows.Point point, float tolerance)
        {
            foreach (var obj in DrawingObjects)
            {
                if (obj.HitTest(point, tolerance))
                {
                    return true;
                }
            }
            return false;
        }


        private void UpdateDrawingObjects(EntityObject entity)
        {
            if (entity is Insert insert)
            {
                var objs = insert.Explode();

                foreach (var e in objs)
                {
                    var obj = DxfHelpers.GetDrawingObject3D(e, Layer);
                    if (obj is not null) { DrawingObjects.Add(obj); }
                }
                UpdateVertices();
            }
            else
            {
                throw new ArgumentException("entity must be of type Insert");
            }
        }

        private void UpdateVertices()
        {
            Vertices.Clear();

            foreach (var obj in DrawingObjects)
            {
                Vertices.AddRange(obj.Vertices);    
            }
        }
        #endregion
    }
}
