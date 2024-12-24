using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using netDxf;
using netDxf.Entities;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Vector3 = SharpDX.Vector3;
using Vector4 = SharpDX.Vector4;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class DrawingPolyline3D : DrawingObject3D
    {
        public int StartVertexIndex { get; set; }
        public int EndVertexIndex { get; set; }
        public Vertex StartVertex { get; set; }
        public Vertex EndVertex { get; set; }
        public List<DrawingSegment3D> DrawingSegment3Ds { get; set; } = [];

        #region Constructors
        private DrawingPolyline3D() { Type = DrawingObject3dType.DrawingLine3D; }

        public DrawingPolyline3D(Polyline2D polyline2D, ObjectLayer3D layer, bool isPartOfBlock = false, DrawingBlock3D block = null)
        {
            Type = DrawingObject3dType.DrawingPolyline3D;
            Layer = layer;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock3D = block;
            EntityObject = polyline2D;

            UpdateColor();
            UpdateData(polyline2D);
        }

        public DrawingPolyline3D(Polyline3D polyline3D, ObjectLayer3D layer, bool isPartOfBlock = false, DrawingBlock3D block = null)
        {
            Type = DrawingObject3dType.DrawingPolyline3D;
            Layer = layer;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock3D = block;
            EntityObject = polyline3D;

            UpdateColor();
            UpdateData(polyline3D);
        }
        #endregion

        #region Methods
        public override void UpdateData(EntityObject entity)
        {
            if (entity is Polyline2D polyline2d)
            {
                var start = polyline2d.Vertexes.First();
                var end = polyline2d.Vertexes.Last();
                StartVertex = new(new Vector3((float)start.Position.X, (float)start.Position.Y, 0), Color);
                EndVertex = new(new Vector3((float)end.Position.X, (float)end.Position.Y, 0), Color);

                var entities = polyline2d.Explode();
                foreach (var e in entities)
                {
                    var obj = DxfHelpers.GetDrawingSegment3D(e, Layer);
                    if (obj is not null) { DrawingSegment3Ds.Add(obj); }
                }
            }
            else if (entity is Polyline3D polyline3d)
            {
                var start = polyline3d.Vertexes.First();
                var end = polyline3d.Vertexes.Last();
                StartVertex = new(new Vector3((float)start.X, (float)start.Y, 0), Color);
                EndVertex = new(new Vector3((float)end.X, (float)end.Y, 0), Color);

                var entities = polyline3d.Explode();
                foreach (var e in entities)
                {
                    var obj = DxfHelpers.GetDrawingSegment3D(e, Layer);
                    if (obj is not null) { DrawingSegment3Ds.Add(obj); }
                }
            }
            else
            {
                throw new ArgumentException("entity must be of type Line");
            }
        }
        #endregion
    }
}
