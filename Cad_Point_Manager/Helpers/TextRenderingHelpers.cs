using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using netDxf.Entities;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using System.Collections.Concurrent;

namespace Cad_Point_Manager.Helpers
{
    public static class TextRenderingHelpers
    {
        private const float _dictBaseTextSize = 10.00f;
        private const float _flatteningTolerance = 0.001f;
        private const float _textHeightToFontSizeFactor = 1.5f;
        private const float _tesselationFactor = 5.0f;

        public static ConcurrentDictionary<(string fontName, FontWeight fontWeight, FontStyle fontstyle), float> FontSizeFactorDict
        { get; } = new ConcurrentDictionary<(string fontName, FontWeight fontWeight, FontStyle fontstyle), float>();

        public static (List<Vector2> vertices, RawRectangleF bounds) TesselateTextLayout(ResCache resCache, TextLayout textLayout,
            string text, FontFace fontFace)
        {
            var fontSizeFactor = GetFontSizeFactor(resCache, textLayout, fontFace); // Multiply by a factor to smoothen curves
            var geometry = TextLayoutToGeometry(resCache, textLayout, text, fontFace, fontSizeFactor);
            var bounds = geometry.GetBounds();

            var matrix = Matrix3x2.Scaling(_tesselationFactor, _tesselationFactor);
            TransformedGeometry transformedGeometry = new(resCache.D2dFactory, geometry, matrix);

            var vertices = TessellateGeometry(transformedGeometry, _flatteningTolerance, 1 / _tesselationFactor);
            geometry.Dispose();

            return (vertices, bounds);
        }

        public static float GetSpaceWidth(ResCache resCache, string fontFamily, float fontSize)
        {
            using (var textFormat = new TextFormat(resCache.WriteFactory, fontFamily, fontSize))
            {
                // Measure width of "A A" and subtract width of "AA" to get accurate space width.
                using (var layoutWithSpace = new TextLayout(resCache.WriteFactory, "A A", textFormat, float.MaxValue, float.MaxValue))
                using (var layoutWithoutSpace = new TextLayout(resCache.WriteFactory, "AA", textFormat, float.MaxValue, float.MaxValue))
                {
                    var widthWithSpace = layoutWithSpace.Metrics.Width;
                    var widthWithoutSpace = layoutWithoutSpace.Metrics.Width;

                    return widthWithSpace - widthWithoutSpace;
                }
            }
        }

        private static PathGeometry TextLayoutToGeometry(ResCache resCache, TextLayout textLayout, string text, FontFace fontFace,
            float scaleFactor)
        {
            PathGeometry pathGeometry = new(resCache.D2dFactory)
            {
                FlatteningTolerance = _flatteningTolerance
            };

            using var sink = pathGeometry.Open();
            var clusterMetrics = textLayout.GetClusterMetrics();
            float charOffset = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i];
                using PathGeometry glyphGeometry = new(resCache.D2dFactory);

                using var glyphSink = glyphGeometry.Open();
                short[] glyphIndices = fontFace.GetGlyphIndices(new int[] { character });
                fontFace.GetGlyphRunOutline(
                    textLayout.FontSize,
                    glyphIndices,
                    null,
                    null,
                    1,
                    false,
                    false,
                    glyphSink);
                glyphSink.Close();

                var combinedMatrix = Matrix3x2.Scaling(scaleFactor, -scaleFactor) * Matrix3x2.Translation(charOffset, 0);

                using TransformedGeometry transformedGlyph = new(resCache.D2dFactory, glyphGeometry, combinedMatrix);
                transformedGlyph.FlatteningTolerance = _flatteningTolerance;
                transformedGlyph.Simplify(GeometrySimplificationOption.Lines, Matrix3x2.Identity, _flatteningTolerance, sink);

                charOffset += (clusterMetrics[i].Width * scaleFactor);
            }
            sink.Close();

            return pathGeometry;
        }

        private static float GetFontSizeFactor(ResCache resCache, TextLayout textLayout, FontFace fontFace)
        {
            bool fontToTextHeightFactorExists = FontSizeFactorDict.TryGetValue(
                   (textLayout.FontFamilyName, textLayout.FontWeight, textLayout.FontStyle), out float fontToTextHeightFactor);

            if (!fontToTextHeightFactorExists)
            {
                TextFormat textFormatForBounds = new(resCache.WriteFactory, textLayout.FontFamilyName, textLayout.FontWeight, textLayout.FontStyle, _dictBaseTextSize);
                TextLayout textLayoutForBounds = new(resCache.WriteFactory, "I", textFormatForBounds, float.MaxValue, float.MaxValue);
                var boundsPathGeometry = TextLayoutToGeometry(resCache, textLayoutForBounds, "I", fontFace, 1);
                var maxHeightBounds = boundsPathGeometry.GetBounds();
                var actualTextHeight = Math.Abs(maxHeightBounds.Top - maxHeightBounds.Bottom);

                fontToTextHeightFactor = _dictBaseTextSize / actualTextHeight;
                FontSizeFactorDict.TryAdd((textLayout.FontFamilyName, textLayout.FontWeight, textLayout.FontStyle), fontToTextHeightFactor);

                boundsPathGeometry.Dispose();
                textFormatForBounds.Dispose();
                textLayoutForBounds.Dispose();
            }

            return fontToTextHeightFactor;
        }

        public static List<Vector2> TessellateGeometry(Geometry geometry, float flatteningTolerance = 0.001f, float tesselationFactor = 1)
        {
            var vertices = new List<Vector2>();

            using (var sink = new CustomTessellationSink())
            {
                geometry.Tessellate(flatteningTolerance, sink);
                vertices.AddRange(sink.Vertices);
            }

            var scale = Matrix.Scaling(tesselationFactor, tesselationFactor, 1);
            for (int i = 0; i < vertices.Count; i++)
            {
                var vertex = vertices[i];
                vertices[i] = Vector2.TransformCoordinate(vertex, scale);
            }

            return vertices;
        }

        public static Enums.TextAttachmentPoint GetAttachmentPoint(MTextAttachmentPoint mTextAttachment)
        {
            return mTextAttachment switch
            {
                MTextAttachmentPoint.TopLeft => Enums.TextAttachmentPoint.TopLeft,
                MTextAttachmentPoint.TopCenter => Enums.TextAttachmentPoint.TopCenter,
                MTextAttachmentPoint.TopRight => Enums.TextAttachmentPoint.TopRight,
                MTextAttachmentPoint.MiddleLeft => Enums.TextAttachmentPoint.MiddleLeft,
                MTextAttachmentPoint.MiddleCenter => Enums.TextAttachmentPoint.MiddleCenter,
                MTextAttachmentPoint.MiddleRight => Enums.TextAttachmentPoint.MiddleRight,
                MTextAttachmentPoint.BottomLeft => Enums.TextAttachmentPoint.BottomLeft,
                MTextAttachmentPoint.BottomCenter => Enums.TextAttachmentPoint.BottomCenter,
                MTextAttachmentPoint.BottomRight => Enums.TextAttachmentPoint.BottomRight,
                _ => Enums.TextAttachmentPoint.MiddleCenter,
            };
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
