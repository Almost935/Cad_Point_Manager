using netDxf;

namespace Cad_Point_Manager.Extensions
{
    public static class NetDxfVector3Extensions
    {
        public static SharpDX.Vector3 ToSharpDXVector3(this Vector3 v)
        {
            return new SharpDX.Vector3(v.X.ToFloat(), v.Y.ToFloat(), v.Z.ToFloat());
        }
    }
}
