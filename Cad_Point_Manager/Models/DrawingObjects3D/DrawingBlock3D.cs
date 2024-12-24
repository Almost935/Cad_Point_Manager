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
        #region Fields
        private List<DrawingObject3D> _drawingObjects = [];
        private List<Vertex> _vertices = [];
        #endregion

        #region Properties
        public List<DrawingObject3D> DrawingObjects
        {
            get => _drawingObjects;
            set
            {
                _drawingObjects = value;
                OnPropertyChanged(nameof(DrawingObjects));
            }
        }
        public List<Vertex> Vertices
        {
            get => _vertices;
            set
            {
                _vertices = value;
                OnPropertyChanged(nameof(Vertices));
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
                UpdateVertices();
            }
            else
            {
               throw new ArgumentException("entity must be of type Insert");
            }
        }

        private void UpdateDrawingObjects(EntityObject entity)
        {
            if (entity is Insert insert)
            {
                foreach (var e in insert.Explode())
                {
                    var obj = DxfHelpers.GetDrawingObject3D(e, Layer);
                    if (obj is not null) { DrawingObjects.Add(obj); }
                }
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
                if (obj is DrawingSegment3D segment)
                {
                    Vertices.AddRange(segment.Vertices);
                }
            }
        }
        #endregion
    }
}
