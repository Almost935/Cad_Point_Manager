using SharpDX;
using SharpDX.Direct2D1;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Helpers
{
    public class CustomTessellationSink : CallbackBase, TessellationSink
    {
        public List<Vector2> Vertices = [];

        public void AddTriangles(Triangle[] triangles)
        {
            foreach (var triangle in triangles)
            {
                Vertices.Add(new Vector2(triangle.Point1.X, triangle.Point1.Y));
                Vertices.Add(new Vector2(triangle.Point2.X, triangle.Point2.Y));
                Vertices.Add(new Vector2(triangle.Point3.X, triangle.Point3.Y));
            }
        }

        public void Close() { }

        public new void QueryInterface(ref Guid guid, out IntPtr comObject)
        {
            comObject = IntPtr.Zero;
        }

        public new int AddRef()
        {
            return 1;
        }

        public new int Release()
        {
            return 1;
        }
    }
}
