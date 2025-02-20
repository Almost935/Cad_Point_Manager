using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Helpers
{
    public static class TextRenderingHelpers
    {
        public static PathGeometry CreateTextGeometry(SharpDX.Direct2D1.Factory d2dFactory, string text, TextFormat textFormat)
        {
            using (var dwriteFactory = new SharpDX.DirectWrite.Factory())
            {
                // Get the system font collection
                FontCollection fontCollection = dwriteFactory.GetSystemFontCollection(false);

                // Find the index of the font in the collection
                int fontIndex;
                bool exists = fontCollection.FindFamilyName(textFormat.FontFamilyName, out fontIndex);
                if (!exists) fontIndex = 0; // Fallback to the first font if not found

                // Get the font family and font
                FontFamily fontFamily = fontCollection.GetFontFamily(fontIndex);
                Font font = fontFamily.GetFont(0); // Use the first font style

                // Create a font face from the font
                FontFace fontFace = new FontFace(font);

                // Create a path geometry for storing text outlines
                var pathGeometry = new PathGeometry(d2dFactory);
                using (var sink = pathGeometry.Open())
                {
                    for (int i = 0; i < text.Length; i++)
                    {
                        short[] glyphIndices = fontFace.GetGlyphIndices(new int[] { text[i] });

                        fontFace.GetGlyphRunOutline(
                            textFormat.FontSize,
                            glyphIndices,
                            null,
                            null,
                            1,
                            false,
                            false,
                            sink
                        );
                    }

                    sink.Close();
                }

                return pathGeometry;
            }
        }

        public static List<Vector2> TessellateGeometry(RenderTarget renderTarget, Geometry geometry)
        {
            var vertices = new List<Vector2>();

            using (var sink = new CustomTessellationSink())
            {
                geometry.Tessellate(sink);
                vertices.AddRange(sink.Vertices); 
            }

            return vertices;
        }

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
}
