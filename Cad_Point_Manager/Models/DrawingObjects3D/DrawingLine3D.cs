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
using System.Windows;
using Vector2 = SharpDX.Vector2;
using Vector3 = SharpDX.Vector3;
using Vector4 = SharpDX.Vector4;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class DrawingLine3D : DrawingSegment3D
    {
        #region Properties
        public Vector3 MidPoint { get; set; }
        #endregion

        #region Constructors
        private DrawingLine3D() { Type = DrawingObject3dType.DrawingLine3D; }

        public DrawingLine3D(Line line, ObjectLayer3D layer, bool isPartOfBlock = false, DrawingBlock3D block = null)
        {
            Type = DrawingObject3dType.DrawingLine3D;
            Layer = layer;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock3D = block;
            EntityObject = line;

            UpdateColor();
            UpdateData(line);
        }
        #endregion

        #region Methods
        public override void UpdateData(EntityObject entity)
        {
            if (entity is Line line)
            {
                StartVertex = new(new Vector3((float)line.StartPoint.X, (float)line.StartPoint.Y, 0), Color);
                EndVertex = new(new Vector3((float)line.EndPoint.X, (float)line.EndPoint.Y, 0), Color);
                Vertices.AddRange(new List<Vertex> { StartVertex, EndVertex });
                Length = Vector3.Distance(StartVertex.Position, EndVertex.Position); 
                MidPoint = (StartVertex.Position + EndVertex.Position) / 2;

                UpdateBounds();
            }
            else
            {
                throw new ArgumentException("entity must be of type Line");
            }
        }

        public override void UpdateBounds()
        {
            Bounds = Rect.Empty;

            Bounds = Rect.Union(Bounds, new System.Windows.Point(StartVertex.Position.X, StartVertex.Position.Y));
            Bounds = Rect.Union(Bounds, new System.Windows.Point(EndVertex.Position.X, EndVertex.Position.Y));
        }

        public override bool HitTest(System.Windows.Point point, float tolerance)
        {
            //bool isPointOnLine = MathHelpers.IsPointOnLine(point, new System.Windows.Point(StartVertex.Position.X, StartVertex.Position.Y),
            //    new System.Windows.Point(EndVertex.Position.X, EndVertex.Position.Y), tolerance);

            //bool isPointOnLine = MathHelpers.IsPointOnLine(point.X, point.Y, StartVertex.Position.X, StartVertex.Position.Y, EndVertex.Position.X, EndVertex.Position.Y, tolerance);
            bool isPointOnLine = MathHelpers.IsPointOnLine(point, new System.Windows.Point(StartVertex.Position.X, StartVertex.Position.Y),
                new System.Windows.Point(EndVertex.Position.X, EndVertex.Position.Y), tolerance);

            return isPointOnLine;
        }
        #endregion
    }
}
