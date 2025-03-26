using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Cad_Point_Manager.Helpers
{
    public static class TextRenderingHelpers
    {
        private const float _dictBaseTextSize = 10.00f;

        public const double TextHeightToFontSizeFactor = 1.5;
        public static ConcurrentDictionary<(string fontName, FontWeight fontWeight, FontStyle fontstyle), float> FontSizeFactorDict 
        { get; } = new ConcurrentDictionary<(string fontName, FontWeight fontWeight, FontStyle fontstyle), float>();

        public static int TextHeightToFontSize(double textHeight)
        {
            return (int)Math.Ceiling(textHeight * TextHeightToFontSizeFactor);
        }

        public static float GetSpaceWidth(SharpDX.DirectWrite.Factory directWriteFactory, string fontFamily, float fontSize)
        {
            using (var textFormat = new TextFormat(directWriteFactory, fontFamily, fontSize))
            {
                // Measure width of "A A" and subtract width of "AA" to get accurate space width.
                using (var layoutWithSpace = new TextLayout(directWriteFactory, "A A", textFormat, float.MaxValue, float.MaxValue))
                using (var layoutWithoutSpace = new TextLayout(directWriteFactory, "AA", textFormat, float.MaxValue, float.MaxValue))
                {
                    var widthWithSpace = layoutWithSpace.Metrics.Width;
                    var widthWithoutSpace = layoutWithoutSpace.Metrics.Width;

                    return widthWithSpace - widthWithoutSpace;
                }
            }
        }

        public static (TransformedGeometry geometry, RawRectangleF bounds) CreateTextGeometry(SharpDX.Direct2D1.Factory d2dFactory, string text,
            TextLayout textLayout, float fontSizeScaleFactor, float textHeight, float flatteningTolerance = 0.001f)
        {
            using (var dwriteFactory = new SharpDX.DirectWrite.Factory())
            {
                FontCollection fontCollection = dwriteFactory.GetSystemFontCollection(false);
                bool exists = fontCollection.FindFamilyName(textLayout.FontFamilyName, out int fontIndex);
                if (!exists) fontIndex = 0; // Fallback to the first font if not found
                FontFamily fontFamily = fontCollection.GetFontFamily(fontIndex);
                
                Font font = fontFamily.GetFont(0);
                FontFace fontFace = new(font);

                var pathGeometry = GetTextLayoutGeometry(d2dFactory, textLayout, text, fontFace, flatteningTolerance);

                bool fontToTextHeightFactorExists = FontSizeFactorDict.TryGetValue(
                    (textLayout.FontFamilyName, textLayout.FontWeight, textLayout.FontStyle), out float fontToTextHeightFactor);
                if (!fontToTextHeightFactorExists)
                {
                    TextFormat textFormatForBounds = new(dwriteFactory, textLayout.FontFamilyName, textLayout.FontWeight, textLayout.FontStyle, _dictBaseTextSize);
                    TextLayout textLayoutForBounds = new(dwriteFactory, "I", textFormatForBounds, float.MaxValue, float.MaxValue);
                    var boundsPathGeometry = GetTextLayoutGeometry(d2dFactory, textLayoutForBounds, "I", fontFace, flatteningTolerance);
                    var maxHeightBounds = boundsPathGeometry.GetBounds();
                    var actualTextHeight = Math.Abs(maxHeightBounds.Top - maxHeightBounds.Bottom);

                    fontToTextHeightFactor = _dictBaseTextSize / actualTextHeight;
                    FontSizeFactorDict.TryAdd((textLayout.FontFamilyName, textLayout.FontWeight, textLayout.FontStyle), fontToTextHeightFactor);

                    Debug.WriteLine($"textLayout.FontFamilyName: {textLayout.FontFamilyName} text: {text} textLayout.FontSize: {textLayout.FontSize} boundsFactor: {fontToTextHeightFactor}");

                    boundsPathGeometry.Dispose();
                    textFormatForBounds.Dispose();
                    textLayout.Dispose();
                }
                
                var boundsScaledGeometry = new TransformedGeometry(d2dFactory, pathGeometry,
                    Matrix3x2.Scaling(fontToTextHeightFactor, fontToTextHeightFactor));
                var updatedBounds = boundsScaledGeometry.GetBounds();

                var scaledGeometry = new TransformedGeometry(d2dFactory, boundsScaledGeometry,
                    Matrix3x2.Scaling(fontSizeScaleFactor, -fontSizeScaleFactor));

                pathGeometry.Dispose();
                fontFace.Dispose();
                fontCollection.Dispose();
                font.Dispose();

                return (scaledGeometry, updatedBounds);
            }
        }

        public static PathGeometry GetTextLayoutGeometry(SharpDX.Direct2D1.Factory d2dFactory, TextLayout textLayout, string text, FontFace fontFace, float flatteningTolerance)
        {
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

                    var transformedGlyph = new TransformedGeometry(d2dFactory, glyphGeometry, Matrix3x2.Translation(charOffset, 0));
                    transformedGlyph.FlatteningTolerance = flatteningTolerance;
                    transformedGlyph.Outline(flatteningTolerance, finalSink);

                    charOffset += clusterMetrics[i].Width;
                }
                finalSink.Close();
            }

            return pathGeometry;
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
