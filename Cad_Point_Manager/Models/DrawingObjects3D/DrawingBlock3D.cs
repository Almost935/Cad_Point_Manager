using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using netDxf;
using netDxf.Entities;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Vector3 = SharpDX.Vector3;
using Vector4 = SharpDX.Vector4;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class DrawingBlock3D : DrawingObject3D
    {
        #region Properties
        private List<DrawingObject3D> _drawingObjects = new();
        public List<DrawingObject3D> DrawingObjects
        {
            get => _drawingObjects;
            set
            {
                _drawingObjects = value;
                OnPropertyChanged(nameof(DrawingObjects));
            }
        }

        public Insert Insert { get; set; }
        #endregion

        #region Constructors
        private DrawingBlock3D() { Type = DrawingObject3dType.DrawingLine3D; }

        public DrawingBlock3D(Insert insert, ObjectLayer3D layer, bool isPartOfBlock = false, DrawingBlock3D block = null)
        {
            Type = DrawingObject3dType.DrawingBlock3D;
            EntityObject = insert;
            Insert = insert;
            Layer = layer;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock3D = block;

            UpdateColor();
            UpdateDrawingObjects();
        }
        #endregion

        #region Methods
        private List<DrawingObject3D> UpdateDrawingObjects()
        {
            List<DrawingObject3D> drawingObjects = [];
            
            foreach (var e in Insert.Explode())
            {
                var obj = DxfHelpers.GetDrawingObject3D(e, Layer);
                if (obj is not null) { drawingObjects.Add(obj); }
            }

            return drawingObjects;
        }

        public override void UpdateData(EntityObject entity)
        {
            if (entity is Insert insert)
            {

            }
        }
        #endregion
    }
}
