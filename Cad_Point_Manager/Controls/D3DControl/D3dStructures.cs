using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Controls.D3DControl
{
    [StructLayout(LayoutKind.Sequential)]
    struct Vertex
    {
        public Vector3 Position;
        public Vector4 Color; 

        public Vertex(Vector3 position, Vector4 color)
        {
            Position = position;
            Color = color;
        }
    }
}
