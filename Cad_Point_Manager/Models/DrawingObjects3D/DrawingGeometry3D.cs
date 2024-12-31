using Cad_Point_Manager.Controls.D3DControl;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public abstract class DrawingGeometry3D : DrawingObject3D
    {
        public Vertex StartVertex { get; set; }
        public Vertex EndVertex { get; set; }
        public List<Vertex> Vertices { get; set; } = [];
        public int StartVertexIndex { get; set; }
        public int EndVertexIndex { get; set; }
        public Geometry Geometry2D { get; set; }
    }
}
