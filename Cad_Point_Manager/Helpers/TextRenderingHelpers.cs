using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using netDxf.Entities;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;

namespace Cad_Point_Manager.Helpers
{
    public static class TextRenderingHelpers
    {
        private const float _dictBaseTextSize = 10.00f;
        public const float _textRenderingScaleFactor = 5.0f;

        public const double TextHeightToFontSizeFactor = 1.5;
        public static ConcurrentDictionary<(string fontName, FontWeight fontWeight, FontStyle fontstyle), float> FontSizeFactorDict 
        { get; } = new ConcurrentDictionary<(string fontName, FontWeight fontWeight, FontStyle fontstyle), float>();

        public static (bool verticesCreated, List<Vector2> vertices, RawRectangleF bounds) TesselateTextLayout(D3dResCache resCache, TextLayout textLayout, string text, float textHeight, FontFace fontFace)
        {
            float renderingSize = _textRenderingScaleFactor * textHeight;
            (var geometry, var bounds) = CreateTextGeometry(resCache, text, textLayout, _textRenderingScaleFactor, textHeight, fontFace);
        }

        public static int TextHeightToFontSize(double textHeight)
        {
            return (int)Math.Ceiling(textHeight * TextHeightToFontSizeFactor);
        }

        public static float GetSpaceWidth(D3dResCache resCache, string fontFamily, float fontSize)
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

        public static (TransformedGeometry geometry, RawRectangleF bounds) CreateTextGeometry(D3dResCache resCache, string text,
            TextLayout textLayout, float fontSizeScaleFactor, float textHeight, FontFace fontFace, float flatteningTolerance = 0.001f)
        {
            using (var dwriteFactory = new SharpDX.DirectWrite.Factory())
            {
                var pathGeometry = GetTextLayoutGeometry(resCache.D2dFactory, textLayout, text, fontFace, flatteningTolerance);

                bool fontToTextHeightFactorExists = FontSizeFactorDict.TryGetValue(
                    (textLayout.FontFamilyName, textLayout.FontWeight, textLayout.FontStyle), out float fontToTextHeightFactor);
                if (!fontToTextHeightFactorExists)
                {
                    TextFormat textFormatForBounds = new(dwriteFactory, textLayout.FontFamilyName, textLayout.FontWeight, textLayout.FontStyle, _dictBaseTextSize);
                    TextLayout textLayoutForBounds = new(dwriteFactory, "I", textFormatForBounds, float.MaxValue, float.MaxValue);
                    var boundsPathGeometry = GetTextLayoutGeometry(resCache.D2dFactory, textLayoutForBounds, "I", fontFace, flatteningTolerance);
                    var maxHeightBounds = boundsPathGeometry.GetBounds();
                    var actualTextHeight = Math.Abs(maxHeightBounds.Top - maxHeightBounds.Bottom);

                    fontToTextHeightFactor = _dictBaseTextSize / actualTextHeight;
                    FontSizeFactorDict.TryAdd((textLayout.FontFamilyName, textLayout.FontWeight, textLayout.FontStyle), fontToTextHeightFactor);

                    boundsPathGeometry.Dispose();
                    textFormatForBounds.Dispose();
                    textLayoutForBounds.Dispose();
                }
                
                var boundsScaledGeometry = new TransformedGeometry(resCache.D2dFactory, pathGeometry,
                    Matrix3x2.Scaling(fontToTextHeightFactor, fontToTextHeightFactor));
                
                var updatedBounds = boundsScaledGeometry.GetBounds();

                var scaledGeometry = new TransformedGeometry(resCache.D2dFactory, boundsScaledGeometry,
                    Matrix3x2.Scaling(fontSizeScaleFactor, -fontSizeScaleFactor));

                pathGeometry.Dispose();

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
                    var character = text[i];
                    var glyphGeometry = new PathGeometry(d2dFactory);
                    glyphGeometry.FlatteningTolerance = flatteningTolerance;

                    using (var glyphSink = glyphGeometry.Open())
                    {
                        short[] glyphIndices = fontFace.GetGlyphIndices(new int[] { character });

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

        public static List<Vector2> TessellateGeometry(Geometry geometry, float flatteningTolerance = 0.001f, float rotation = 0)
        {
            var vertices = new List<Vector2>();
            
            using (var sink = new CustomTessellationSink())
            {
                geometry.Tessellate(flatteningTolerance, sink);
                vertices.AddRange(sink.Vertices);
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
