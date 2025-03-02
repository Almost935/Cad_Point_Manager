using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace Cad_Point_Manager.Helpers
{
    public static class TextRenderingHelpers
    {
        public static PathGeometry CreateTextGeometry(SharpDX.Direct2D1.Factory d2dFactory, string text, TextLayout textLayout, float flatteningTolerance = 0.001f)
        {
            using (var dwriteFactory = new SharpDX.DirectWrite.Factory())
            {
                // Get the system font collection
                FontCollection fontCollection = dwriteFactory.GetSystemFontCollection(false);

                // Find the index of the font in the collection
                bool exists = fontCollection.FindFamilyName(textLayout.FontFamilyName, out int fontIndex);
                if (!exists) fontIndex = 0; // Fallback to the first font if not found

                // Get the font family and font
                FontFamily fontFamily = fontCollection.GetFontFamily(fontIndex);

                //Debug.WriteLine($"\n");

                for (int j = 0; j < fontFamily.FontCount; j++)
                {
                    var candidateFont = fontFamily.GetFont(j);

                    //Debug.WriteLine($"candidateFont: {candidateFont}");
                    //Debug.WriteLine($"Style: {candidateFont.Style}");
                    //Debug.WriteLine($"Weight: {candidateFont.Weight}");
                }

                Font font = fontFamily.GetFont(0); // Use the first font style

                // Create a font face from the font
                FontFace fontFace = new(font);

                // Create a path geometry for storing text outlines
                var pathGeometry = new PathGeometry(d2dFactory);
                pathGeometry.FlatteningTolerance = flatteningTolerance;

                using (var finalSink = pathGeometry.Open())
                {
                    var clusterMetrics = textLayout.GetClusterMetrics();
                    float charOffset = 0;

                    for (int i = 0; i < text.Length; i++)
                    {
                        var glyphGeometry = new PathGeometry(d2dFactory);
                        glyphGeometry.FlatteningTolerance = flatteningTolerance;

                        using (var glyphSink = glyphGeometry.Open())
                        {
                            short[] glyphIndices = fontFace.GetGlyphIndices(new int[] { text[i] });

                            fontFace.GetGlyphRunOutline(
                                textLayout.FontSize,
                                glyphIndices,
                                null,
                                null,
                                1,
                                false,
                                false,
                                glyphSink
                            );
                            glyphSink.Close();
                        }
                        var transformedGlyph = new TransformedGeometry(d2dFactory, glyphGeometry, Matrix3x2.Translation(charOffset, 0) * Matrix3x2.Scaling(1, -1));
                        transformedGlyph.FlatteningTolerance = flatteningTolerance;
                        transformedGlyph.Outline(flatteningTolerance, finalSink);

                        charOffset += clusterMetrics[i].Width;
                    }
                    finalSink.Close();
                }

                return pathGeometry;
            }
        }

        public static List<Vector2> TessellateGeometry(Geometry geometry, float flatteningTolerance = 0.001f)
        {
            var vertices = new List<Vector2>();

            using (var sink = new CustomTessellationSink())
            {
                geometry.Tessellate(flatteningTolerance, sink);
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
