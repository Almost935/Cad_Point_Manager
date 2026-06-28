using netDxf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Extensions
{
    public static class NetDxfVector2Extensions
    {
        public static SharpDX.Vector3 ToSharpDXVector3(this Vector2 v, float elevation = 0.0f)
        {
            return new SharpDX.Vector3(v.X.ToFloat(), v.Y.ToFloat(), elevation);
        }
    }
}
