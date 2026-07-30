using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;
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
        private const float _tesselationFactor = 1.0f;

        public static ConcurrentDictionary<(string fontName, FontWeight fontWeight, FontStyle fontstyle), float> FontSizeFactorDict
        { get; } = new ConcurrentDictionary<(string fontName, FontWeight fontWeight, FontStyle fontstyle), float>();

        public static (List<Vector2> vertices, RawRectangleF bounds) GetLineRepresentationOfTextLayout(
            ResCache resCache,
            TextLayout textLayout,
            string text,
            FontFace fontFace)
        {
            var fontSizeFactor =
                GetFontSizeFactor(resCache, textLayout, fontFace);

            var geometry =
                TextLayoutToGeometry(resCache, textLayout, text, fontFace, fontSizeFactor);

            var bounds = geometry.GetBounds();

            List<Vector2> vertices;

            using (var sink = new CustomLineSink())
            {
                geometry.Simplify(GeometrySimplificationOption.Lines, Matrix3x2.Identity, _flatteningTolerance, sink);

                vertices = sink.Vertices;
            }

            geometry.Dispose();

            return (vertices, bounds);
        }

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

        public static float GetSpaceWidth(SharpDX.DirectWrite.Factory1 writeFactory, string fontFamily, float fontSize)
        {
            using (var textFormat = new TextFormat(writeFactory, fontFamily, fontSize))
            {
                // Measure width of "A A" and subtract width of "AA" to get accurate space width.
                using var layoutWithSpace = new TextLayout(writeFactory, "A A", textFormat, float.MaxValue, float.MaxValue);
                using var layoutWithoutSpace = new TextLayout(writeFactory, "AA", textFormat, float.MaxValue, float.MaxValue);
                var widthWithSpace = layoutWithSpace.Metrics.Width;
                var widthWithoutSpace = layoutWithoutSpace.Metrics.Width;

                return widthWithSpace - widthWithoutSpace;
            }
        }
        public static float GetFontSizeFactor(ResCache resCache, TextLayout textLayout, FontFace fontFace)
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

        public static TextAttachmentPoint GetAttachmentPoint(MTextAttachmentPoint mTextAttachment)
        {
            return mTextAttachment switch
            {
                MTextAttachmentPoint.TopLeft => TextAttachmentPoint.TopLeft,
                MTextAttachmentPoint.TopCenter => TextAttachmentPoint.TopCenter,
                MTextAttachmentPoint.TopRight => TextAttachmentPoint.TopRight,
                MTextAttachmentPoint.MiddleLeft => TextAttachmentPoint.MiddleLeft,
                MTextAttachmentPoint.MiddleCenter => TextAttachmentPoint.MiddleCenter,
                MTextAttachmentPoint.MiddleRight => TextAttachmentPoint.MiddleRight,
                MTextAttachmentPoint.BottomLeft => TextAttachmentPoint.BottomLeft,
                MTextAttachmentPoint.BottomCenter => TextAttachmentPoint.BottomCenter,
                MTextAttachmentPoint.BottomRight => TextAttachmentPoint.BottomRight,
                _ => TextAttachmentPoint.MiddleCenter,
            };
        }
        public static TextAttachmentPoint GetAttachmentPoint(netDxf.Entities.TextAlignment mTextAttachment)
        {
            return mTextAttachment switch
            {
                netDxf.Entities.TextAlignment.TopLeft => TextAttachmentPoint.TopLeft,
                netDxf.Entities.TextAlignment.TopCenter => TextAttachmentPoint.TopCenter,
                netDxf.Entities.TextAlignment.TopRight => TextAttachmentPoint.TopRight,
                netDxf.Entities.TextAlignment.MiddleLeft => TextAttachmentPoint.MiddleLeft,
                netDxf.Entities.TextAlignment.MiddleCenter => TextAttachmentPoint.MiddleCenter,
                netDxf.Entities.TextAlignment.MiddleRight => TextAttachmentPoint.MiddleRight,
                netDxf.Entities.TextAlignment.BottomLeft => TextAttachmentPoint.BottomLeft,
                netDxf.Entities.TextAlignment.BottomCenter => TextAttachmentPoint.BottomCenter,
                netDxf.Entities.TextAlignment.BottomRight => TextAttachmentPoint.BottomRight,
                _ => TextAttachmentPoint.MiddleCenter,
            };
        }

        public static Vector2 GetAttachmentOffset(RectangleF bounds, TextAttachmentPoint attachmentPoint)
        {
            var xOffset = bounds.Width;
            var yOffset = bounds.Height;

            return attachmentPoint switch
            {
                TextAttachmentPoint.TopLeft =>
                    new Vector2(0, -yOffset),

                TextAttachmentPoint.TopCenter =>
                    new Vector2(-(xOffset / 2), -yOffset),

                TextAttachmentPoint.TopRight =>
                    new Vector2(-xOffset, -yOffset),

                TextAttachmentPoint.MiddleLeft =>
                    new Vector2(0, -yOffset / 2),

                TextAttachmentPoint.MiddleCenter =>
                    new Vector2(-(xOffset / 2), -yOffset / 2),

                TextAttachmentPoint.MiddleRight =>
                    new Vector2(-xOffset, -yOffset / 2),

                TextAttachmentPoint.BottomLeft =>
                    new Vector2(0, 0),

                TextAttachmentPoint.BottomCenter =>
                    new Vector2(-(xOffset / 2), 0),

                TextAttachmentPoint.BottomRight =>
                    new Vector2(-xOffset, 0),

                _ => Vector2.Zero
            };
        }
    }
}
