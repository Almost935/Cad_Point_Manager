using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Helpers;
using Cad_Point_Manager.Extensions;
using netDxf.Entities;
using PdfSharpCore.Drawing;
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
        private const float _flatteningTolerance = 0.0001f;

        public static ConcurrentDictionary<(string fontName, FontWeight fontWeight, FontStyle fontstyle), double> FontSizeFactorDict
        { get; } = new ConcurrentDictionary<(string fontName, FontWeight fontWeight, FontStyle fontstyle), double>();

        public static (List<Vector2> vertices, RawRectangleF bounds) GetLineRepresentationOfTextLayout(
            ResCache resCache,
            TextLayout textLayout,
            string text,
            FontFace fontFace)
        {
            var fontSizeFactor = GetFontSizeFactor(resCache, textLayout, fontFace);
            var geometry = TextLayoutToGeometry(resCache, textLayout, text, fontFace, fontSizeFactor);
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

        public static (List<Vector2d> vertices, RawRectangleF bounds) TesselateTextLayout(
            ResCache resCache, TextLayout textLayout, string text, FontFace fontFace)
        {
            double fontSizeFactor = GetFontSizeFactor(resCache, textLayout, fontFace); // Multiply by a factor to smoothen curves
            var geometry = TextLayoutToGeometry(resCache, textLayout, text, fontFace, fontSizeFactor);
            var bounds = geometry.GetBounds();
            var tesselationFactor = GetTessellationFactor(textLayout.FontSize);

            var matrix = Matrix3x2.Scaling(tesselationFactor, tesselationFactor);
            TransformedGeometry transformedGeometry = new(resCache.D2dFactory, geometry, matrix);

            var vertices = TessellateGeometry(transformedGeometry, _flatteningTolerance, 1 / tesselationFactor);
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
        public static double GetFontSizeFactor(ResCache resCache, TextLayout textLayout, FontFace fontFace)
        {
            bool fontToTextHeightFactorExists = FontSizeFactorDict.TryGetValue(
                   (textLayout.FontFamilyName, textLayout.FontWeight, textLayout.FontStyle), out double fontToTextHeightFactor);

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

        private static PathGeometry TextLayoutToGeometry(
            ResCache resCache, TextLayout textLayout, string text, FontFace fontFace, double scaleFactor)
        {
            PathGeometry pathGeometry = new(resCache.D2dFactory)
            {
                FlatteningTolerance = _flatteningTolerance
            };

            using var sink = pathGeometry.Open();
            var clusterMetrics = textLayout.GetClusterMetrics();
            double charOffset = 0;

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

                var combinedMatrix = new System.Windows.Media.Matrix(
                    scaleFactor, 0, 0, -scaleFactor, charOffset, 0);

                using TransformedGeometry transformedGlyph = new(
                    resCache.D2dFactory, glyphGeometry, combinedMatrix.ToSharpDxMatrix3x2());

                transformedGlyph.FlatteningTolerance = _flatteningTolerance;
                transformedGlyph.Simplify(GeometrySimplificationOption.Lines, Matrix3x2.Identity, _flatteningTolerance, sink);

                charOffset += (clusterMetrics[i].Width * scaleFactor);
            }
            sink.Close();

            return pathGeometry;
        }

        public static List<Vector2d> TessellateGeometry(
            Geometry geometry, float flatteningTolerance = 0.001f, float tesselationFactor = 1)
        {
            using var sink = new CustomTessellationSink();

            geometry.Tessellate(flatteningTolerance, sink);

            var source = sink.Vertices;
            var vertices = new List<Vector2d>(source.Count);

            double scale = tesselationFactor;

            for (int i = 0; i < source.Count; i++)
            {
                var v = source[i];

                vertices.Add(new Vector2d(
                    v.X * scale,
                    v.Y * scale));
            }

            return vertices;
        }

        public static MTextAttachment GetMtextAttachmentPoint(MTextAttachmentPoint mTextAttachment)
        {
            return mTextAttachment switch
            {
                netDxf.Entities.MTextAttachmentPoint.TopLeft => Common.MTextAttachment.TopLeft,
                netDxf.Entities.MTextAttachmentPoint.TopCenter => Common.MTextAttachment.TopCenter,
                netDxf.Entities.MTextAttachmentPoint.TopRight => Common.MTextAttachment.TopRight,
                netDxf.Entities.MTextAttachmentPoint.MiddleLeft => Common.MTextAttachment.MiddleLeft,
                netDxf.Entities.MTextAttachmentPoint.MiddleCenter => Common.MTextAttachment.MiddleCenter,
                netDxf.Entities.MTextAttachmentPoint.MiddleRight => Common.MTextAttachment.MiddleRight,
                netDxf.Entities.MTextAttachmentPoint.BottomLeft => Common.MTextAttachment.BottomLeft,
                netDxf.Entities.MTextAttachmentPoint.BottomCenter => Common.MTextAttachment.BottomCenter,
                netDxf.Entities.MTextAttachmentPoint.BottomRight => Common.MTextAttachment.BottomRight,
                _ => Common.MTextAttachment.MiddleCenter,
            };
        }
        public static Common.TextAlignment GetTextAlignment(netDxf.Entities.TextAlignment mTextAttachment)
        {
            return mTextAttachment switch
            {
                netDxf.Entities.TextAlignment.TopLeft => Common.TextAlignment.TopLeft,
                netDxf.Entities.TextAlignment.TopCenter => Common.TextAlignment.TopCenter,
                netDxf.Entities.TextAlignment.TopRight => Common.TextAlignment.TopRight,
                netDxf.Entities.TextAlignment.MiddleLeft => Common.TextAlignment.MiddleLeft,
                netDxf.Entities.TextAlignment.MiddleCenter => Common.TextAlignment.MiddleCenter,
                netDxf.Entities.TextAlignment.MiddleRight => Common.TextAlignment.MiddleRight,
                netDxf.Entities.TextAlignment.BottomLeft => Common.TextAlignment.BottomLeft,
                netDxf.Entities.TextAlignment.BottomCenter => Common.TextAlignment.BottomCenter,
                netDxf.Entities.TextAlignment.BottomRight => Common.TextAlignment.BottomRight,
                netDxf.Entities.TextAlignment.BaselineLeft => Common.TextAlignment.BaselineLeft,
                netDxf.Entities.TextAlignment.BaselineCenter => Common.TextAlignment.BaselineCenter,
                netDxf.Entities.TextAlignment.BaselineRight => Common.TextAlignment.BaselineRight,
                netDxf.Entities.TextAlignment.Aligned => Common.TextAlignment.Aligned,
                netDxf.Entities.TextAlignment.Middle => Common.TextAlignment.Middle,
                netDxf.Entities.TextAlignment.Fit => Common.TextAlignment.Fit,
                _ => Common.TextAlignment.BottomLeft,
            };
        }
        public static XStringFormat MtextAttachmentToStringFormat(Common.MTextAttachment mTextAttachment)
        {
            return mTextAttachment switch
            {
                Common.MTextAttachment.TopLeft => XStringFormats.TopLeft,
                Common.MTextAttachment.TopCenter => XStringFormats.TopCenter,
                Common.MTextAttachment.TopRight => XStringFormats.TopRight,
                Common.MTextAttachment.MiddleLeft => XStringFormats.CenterLeft,
                Common.MTextAttachment.MiddleCenter => XStringFormats.Center,
                Common.MTextAttachment.MiddleRight => XStringFormats.CenterRight,
                Common.MTextAttachment.BottomLeft => XStringFormats.BottomLeft,
                Common.MTextAttachment.BottomCenter => XStringFormats.BottomCenter,
                Common.MTextAttachment.BottomRight => XStringFormats.BottomRight,
                _ => XStringFormats.Default
            };
        }
        public static XStringFormat TextAlignmentToStringFormat(Common.TextAlignment textAlignment)
        {
            return textAlignment switch
            {
                Common.TextAlignment.TopLeft => XStringFormats.TopLeft,
                Common.TextAlignment.TopCenter => XStringFormats.TopCenter,
                Common.TextAlignment.TopRight => XStringFormats.TopRight,
                Common.TextAlignment.MiddleLeft => XStringFormats.CenterLeft,
                Common.TextAlignment.MiddleCenter => XStringFormats.Center,
                Common.TextAlignment.MiddleRight => XStringFormats.CenterRight,
                Common.TextAlignment.BottomLeft => XStringFormats.BottomLeft,
                Common.TextAlignment.BottomCenter => XStringFormats.BottomCenter,
                Common.TextAlignment.BottomRight => XStringFormats.BottomRight,
                Common.TextAlignment.BaselineLeft => XStringFormats.BaseLineLeft,
                Common.TextAlignment.BaselineCenter => XStringFormats.BaseLineCenter,
                Common.TextAlignment.BaselineRight => XStringFormats.BaseLineRight,
                Common.TextAlignment.Aligned => XStringFormats.BottomLeft,
                Common.TextAlignment.Middle => XStringFormats.Center,
                Common.TextAlignment.Fit => XStringFormats.BottomLeft,
                _ => XStringFormats.Default
            };
        }

        public static Vector2 GetAttachmentOffset(RectangleF bounds, MTextAttachment attachmentPoint)
        {
            var xOffset = bounds.Width;
            var yOffset = bounds.Height;

            return attachmentPoint switch
            {
                Common.MTextAttachment.TopLeft =>
                    new Vector2(0, -yOffset),

                Common.MTextAttachment.TopCenter =>
                    new Vector2(-(xOffset / 2), -yOffset),

                Common.MTextAttachment.TopRight =>
                    new Vector2(-xOffset, -yOffset),

                Common.MTextAttachment.MiddleLeft =>
                    new Vector2(0, -yOffset / 2),

                Common.MTextAttachment.MiddleCenter =>
                    new Vector2(-(xOffset / 2), -yOffset / 2),

                Common.MTextAttachment.MiddleRight =>
                    new Vector2(-xOffset, -yOffset / 2),

                Common.MTextAttachment.BottomLeft =>
                    new Vector2(0, 0),

                Common.MTextAttachment.BottomCenter =>
                    new Vector2(-(xOffset / 2), 0),

                Common.MTextAttachment.BottomRight =>
                    new Vector2(-xOffset, 0),

                _ => Vector2.Zero
            };
        }

        private static float GetTessellationFactor(float textHeight)
        {
            const float minFactor = 1.0f;

            const float referenceHeight = 0.05f;
            const float referenceFactor = 60.0f;

            // Controls how aggressively tessellation falls as text gets larger.
            const float exponent = 0.75f;

            float factor = referenceFactor *
                           MathF.Pow(referenceHeight / textHeight, exponent);

            return Math.Max(minFactor, factor);
        }
    }
}
