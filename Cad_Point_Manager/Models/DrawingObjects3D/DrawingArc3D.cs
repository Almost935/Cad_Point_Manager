using Cad_Point_Manager.Controls.D3DControl;
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
    public class DrawingArc3D : DrawingObject3D
    {
        public Vector3 Start { get; set; }
        public Vector3 End { get; set; }
        public Vector4 Color { get; set; }
        public int StartVertexIndex { get; set; }
        public int EndVertexIndex { get; set; }
        public Vertex StartVertex { get; set; }
        public Vertex EndVertex { get; set; }

        private DrawingArc3D() { Type = DrawingObject3dType.DrawingLine3D; }

        public DrawingArc3D(Line line)
        {
            Type = DrawingObject3dType.DrawingLine3D;

            Start = new((float)line.StartPoint.X, (float)line.StartPoint.Y, 0);
            End = new((float)line.EndPoint.X, (float)line.EndPoint.Y, 0);
            Color = new(0, 0, 0, 1);

            StartVertex = new(Start, Color);
            EndVertex = new(End, Color);
        }
    }
}
