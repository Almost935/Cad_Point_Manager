using netDxf;
using Cad_Point_Manager.Common;

namespace Cad_Point_Manager.Extensions
{
    public static class NetDxfVector3Extensions
    {
        public static SharpDX.Vector3 ToSharpDXVector3(this Vector3 v)
        {
            return new SharpDX.Vector3((float)v.X, (float)v.Y, (float)v.Z);
        }

        public static Vector2d ToVector2d(this Vector3 v)
        {
            return new Vector2d((float)v.X, (float)v.Y);
        }
    }
}
