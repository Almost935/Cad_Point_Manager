using Cad_Point_Manager.Controls.D3DControl;
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
    public class DrawingLine3D : DrawingSegment3D
    {
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
                MiddlePoint = (StartVertex.Position + EndVertex.Position) / 2;
            }
            else
            {
                throw new ArgumentException("entity must be of type Line");
            }
        }
        #endregion
    }
}
