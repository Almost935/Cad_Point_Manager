using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using netDxf;
using netDxf.Entities;
using SharpDX;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Vector2 = SharpDX.Vector2;
using Vector3 = SharpDX.Vector3;
using Vector4 = SharpDX.Vector4;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class DrawingPolyline3D : DrawingObject3D
    {
        #region Properties
        public int StartVertexIndex { get; set; }
        public int EndVertexIndex { get; set; }
        public Vertex StartVertex { get; set; }
        public Vertex EndVertex { get; set; }
        public float Length { get; set; }
        public bool IsClosed { get; set; }
        public List<DrawingSegment3D> DrawingSegments { get; set; } = [];
        public int NumberOfSegments => DrawingSegments.Count;
        #endregion

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
                IsClosed = polyline2d.IsClosed;

                UpdateVertices(polyline2d);
                UpdateBounds();

                Length = 0;
                foreach (var segment in DrawingSegments)
                {
                    Length += segment.Length;
                }
            }
            else if (entity is Polyline3D polyline3d)
            {
                IsClosed = polyline3d.IsClosed;

                UpdateVertices(polyline3d);

                Length = 0;
                foreach (var segment in DrawingSegments)
                {
                    Length += segment.Length;
                }
            }
            else
            {
                throw new ArgumentException("entity must be of type Polyline2D or Polyline3D");
            }
        }

        public override void UpdateBounds()
        {
            Bounds = Rect.Empty;

            foreach (var segment in DrawingSegments)
            {
                Bounds = Rect.Union(Bounds, segment.Bounds);
            }
        }

        public override bool HitTest(System.Windows.Point point, float tolerance)
        {
            foreach (var segment in DrawingSegments)
            {
                if (segment.HitTest(point, tolerance))
                {
                    return true;
                }
            }

            return false;
        }


        public void UpdateVertices(EntityObject entity)
        {
            if (entity is Polyline2D polyline2D)
            {
                var start = polyline2D.Vertexes.First();
                var end = polyline2D.Vertexes.Last();
                StartVertex = new(new Vector3((float)start.Position.X, (float)start.Position.Y, 0), Color);
                EndVertex = new(new Vector3((float)end.Position.X, (float)end.Position.Y, 0), Color);

                var entities = polyline2D.Explode();
                foreach (var e in entities)
                {
                    var obj = DxfHelpers.GetDrawingSegment3D(e, Layer);
                    if (obj is not null) 
                    { 
                        DrawingSegments.Add(obj); 
                        Vertices.AddRange(obj.Vertices);
                    }
                }
            }
            else if (entity is Polyline3D polyline3D)
            {
                var start = polyline3D.Vertexes.First();
                var end = polyline3D.Vertexes.Last();
                StartVertex = new(new Vector3((float)start.X, (float)start.Y, (float)start.Z), Color);
                EndVertex = new(new Vector3((float)end.X, (float)end.Y, (float)end.Z), Color);

                var entities = polyline3D.Explode();
                foreach (var e in entities)
                {
                    var obj = DxfHelpers.GetDrawingSegment3D(e, Layer);
                    if (obj is not null)
                    {
                        DrawingSegments.Add(obj);
                        Vertices.AddRange(obj.Vertices);
                    }
                }
            }
            else
            {
                throw new ArgumentException("entity must be of type Polyline2D or Polyline3D");
            }
            #endregion
        }
    }
}
